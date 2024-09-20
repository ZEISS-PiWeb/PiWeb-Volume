#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

// ReSharper disable SuggestBaseTypeForParameter
namespace Zeiss.PiWeb.Volume.Block
{
	#region usings

	using System;
	using System.Buffers;
	using System.IO;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;

	#endregion

	internal class BlockVolumeDecoder
	{
		#region methods

		internal static void Decode( byte[] data,
			VolumeMetadata metadata,
			BlockAction blockAction,
			LayerPredicate? layerPredicate = null,
			BlockPredicate? blockPredicate = null,
			IProgress<VolumeSliceDefinition>? progress = null,
			CancellationToken ct = default )
		{
			var zigzag = ZigZag.Calculate();
			var (bcx, bcy, bcz) = BlockVolume.GetBlockCount( metadata );
			var blockCount = bcx * bcy;
			var encodedBlockInfos = new EncodedBlockInfo[ blockCount ];
			var reader = new BinaryReader( new MemoryStream( data ) );

			ReadMetadata( reader, out var quantization );

			for( ushort biz = 0; biz < bcz; biz++ )
			{
				ct.ThrowIfCancellationRequested();

				ReadLayerInfos( reader, blockCount, encodedBlockInfos );

				if( layerPredicate?.Invoke( biz ) is false )
					continue;

				DecodeLayer( data, encodedBlockInfos, bcx, bcy, biz, quantization, zigzag, blockPredicate, blockAction );


				progress?.Report( new VolumeSliceDefinition( Direction.Z, (ushort)( biz * BlockVolume.N ) ) );
			}
		}

		private static void ReadMetadata( BinaryReader reader, out double[] quantization )
		{
			var header = reader.ReadUInt32();
			if( header != BlockVolume.FileHeader )
				throw new FormatException( $"Encountered unexpected file header 0x{header:x8}, expected 0x{BlockVolume.FileHeader:x8}" );

			var version = reader.ReadUInt32();
			if( version != BlockVolume.Version )
				throw new FormatException( $"Encountered unexpected file header '{version}', expected {BlockVolume.Version}" );

			quantization = Quantization.Read( reader, true );
		}

		private static void ReadLayerInfos( BinaryReader reader, int blockCount, EncodedBlockInfo[] encodedBlockInfos )
		{
			for( var i = 0; i < blockCount; i++ )
			{
				var encodedBlockInfo = EncodedBlockInfo.Read( reader );
				reader.BaseStream.Seek( encodedBlockInfo.Length, SeekOrigin.Current );
				encodedBlockInfos[ i ] = encodedBlockInfo;
			}
		}

		private readonly record struct EncodedBlockInfo( int StartIndex, ushort ValueCount, byte FirstValueSize, byte OtherValuesSize )
		{
			public ushort Length => (ushort)( FirstValueSize + ( ValueCount - 1 ) * OtherValuesSize );

			public static EncodedBlockInfo Read( BinaryReader reader )
			{
				var resultLength = reader.ReadUInt16();

				var startIndex = (int)reader.BaseStream.Position;
				var valueCount = resultLength & 0x0FFF;
				var firstValueSize = ( resultLength & 0b0011000000000000 ) >> 12;
				var otherValuesSize = ( resultLength & 0b1100000000000000 ) >> 14;

				return new EncodedBlockInfo( startIndex, (ushort)valueCount, (byte)firstValueSize, (byte)otherValuesSize );
			}
		}

		private static void DecodeLayer(
			byte[] encodedBlocks,
			EncodedBlockInfo[] encodedBlockInfos,
			ushort blockCountX,
			ushort blockCountY,
			ushort blockIndexZ,
			double[] quantization,
			int[] zigzag,
			BlockPredicate? blockPredicate,
			BlockAction blockAction )
		{
			Parallel.For( 0, blockCountX * blockCountY, () => (
					ArrayPool<double>.Shared.Rent( BlockVolume.N3 ),
					ArrayPool<double>.Shared.Rent( BlockVolume.N3 ),
					ArrayPool<byte>.Shared.Rent( BlockVolume.N3 )
				), ( index, _, buffers ) =>
				{
					var blockIndexX = index % blockCountX;
					var blockIndexY = index / blockCountX;
					var blockIndex = new BlockIndex( (ushort)blockIndexX, (ushort)blockIndexY, blockIndexZ );

					if( blockPredicate?.Invoke( blockIndex ) == false )
						return buffers;

					var doubleBuffer1 = buffers.Item1.AsSpan();
					var doubleBuffer2 = buffers.Item2.AsSpan();
					var byteBuffer = buffers.Item3;
					var encodedBlockInfo = encodedBlockInfos[ index ];

					//1. Dediscretization
					ReadBlock( encodedBlocks.AsSpan(), encodedBlockInfo, doubleBuffer1 );

					//2. ZigZag
					for( var i = 0; i < BlockVolume.N3; i++ )
						doubleBuffer2[ zigzag[ i ] ] = doubleBuffer1[ i ];

					//3. Quantization
					for( var i = 0; i < BlockVolume.N3; i++ )
						doubleBuffer2[ i ] *= quantization[ i ];

					//4. Cosine transform
					DiscreteCosineTransform.Transform( doubleBuffer2, doubleBuffer1, true );

					//5. Discretization
					for( var i = 0; i < BlockVolume.N3; i++ )
						byteBuffer[ i ] = (byte)Math.Clamp( Math.Round( doubleBuffer1[ i ] ), byte.MinValue, byte.MaxValue );

					blockAction( byteBuffer, blockIndex );

					return buffers;
				},
				buffers =>
				{
					ArrayPool<double>.Shared.Return( buffers.Item1 );
					ArrayPool<double>.Shared.Return( buffers.Item2 );
					ArrayPool<byte>.Shared.Return( buffers.Item3 );
				} );
		}

		private static void ReadBlock( ReadOnlySpan<byte> data, EncodedBlockInfo encodedBlockInfo, Span<double> result )
		{
			var encodedBlockData = data.Slice( encodedBlockInfo.StartIndex, encodedBlockInfo.Length );

			result[ 0 ] = encodedBlockInfo.FirstValueSize == 1
				? MemoryMarshal.Read<sbyte>( encodedBlockData )
				: MemoryMarshal.Read<short>( encodedBlockData );

			var otherValueData = encodedBlockData
				.Slice( encodedBlockInfo.FirstValueSize, encodedBlockInfo.Length - encodedBlockInfo.FirstValueSize );

			if( encodedBlockInfo.OtherValuesSize == 1 )
			{
				var values = MemoryMarshal.Cast<byte, sbyte>( otherValueData );
				for( ushort i = 1, vi = 0; i < BlockVolume.N3; i++, vi++ )
					result[ i ] = i >= encodedBlockInfo.ValueCount ? 0 : values[ vi ];
			}
			else
			{
				var values = MemoryMarshal.Cast<byte, short>( otherValueData );
				for( ushort i = 1, vi = 0; i < BlockVolume.N3; i++, vi++ )
					result[ i ] = i >= encodedBlockInfo.ValueCount ? 0 : values[ vi ];
			}
		}

		#endregion

		internal delegate void BlockAction( byte[] data, BlockIndex index );

		internal delegate bool BlockPredicate( BlockIndex index );

		internal delegate bool LayerPredicate( ushort index );
	}
}