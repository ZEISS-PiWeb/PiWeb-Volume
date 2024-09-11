#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
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
	using System.Threading;
	using System.Threading.Tasks;

	#endregion

	internal class BlockVolumeDecoder
	{
		#region methods

		internal void Decode( Stream input,
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
			var encodedBlockLengths = new ushort[ blockCount ];
			var encodedBlocks = new short[ blockCount * BlockVolume.N3 ];

			using var reader = new BinaryReader( input );

			ReadMetadata( reader, out var quantization );

			for( ushort biz = 0; biz < bcz; biz++ )
			{
				ct.ThrowIfCancellationRequested();
				if( layerPredicate?.Invoke( biz ) is false )
				{
					SkipLayer( reader, blockCount );
				}
				else
				{
					ReadLayer( reader, blockCount, encodedBlocks, encodedBlockLengths );
					DecodeLayer( encodedBlocks, encodedBlockLengths, bcx, bcy, biz, quantization, zigzag, blockPredicate, blockAction );
				}

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

		private static void SkipLayer( BinaryReader reader, int blockCount )
		{
			for( var i = 0; i < blockCount; i++ )
			{
				var resultLength = reader.ReadUInt16();

				var length = resultLength & 0x0FFF;
				var firstLength = ( resultLength & 0b0011000000000000 ) >> 12;
				var otherLength = ( resultLength & 0b1100000000000000 ) >> 14;

				var count = 0;
				if( length > 0 )
					count += firstLength;
				if( length > 1 )
					count += ( length - 1 ) * otherLength;

				reader.BaseStream.Seek( count, SeekOrigin.Current );
			}
		}

		private static void ReadLayer( BinaryReader reader, int blockCount, short[] encodedBlocks, ushort[] encodedBlockLengths )
		{
			var blockBuffer = ArrayPool<short>.Shared.Rent( BlockVolume.N3 );

			for( var i = 0; i < blockCount; i++ )
			{
				var resultLength = reader.ReadUInt16();

				var dataIndex = 0;
				var length = resultLength & 0x0FFF;
				var firstLength = ( resultLength & 0b0011000000000000 ) >> 12;
				var otherLength = ( resultLength & 0b1100000000000000 ) >> 14;

				encodedBlockLengths[ i ] = (ushort)length;
				if( length > 0 )
					blockBuffer[ dataIndex++ ] = firstLength == 2 ? reader.ReadInt16() : reader.ReadSByte();

				for( var j = 1; j < length; j++ )
					blockBuffer[ dataIndex++ ] = otherLength == 2 ? reader.ReadInt16() : reader.ReadSByte();

				Buffer.BlockCopy( blockBuffer, 0, encodedBlocks, i * BlockVolume.N3 * sizeof( short ), BlockVolume.N3 * sizeof( short ) );
			}

			ArrayPool<short>.Shared.Return( blockBuffer );
		}

		private static void DecodeLayer(
			short[] encodedBlocks,
			ushort[] encodedBlockLengths,
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

					var encodedLength = encodedBlockLengths[ index ];

					var doubleBuffer1 = buffers.Item1;
					var doubleBuffer2 = buffers.Item2;
					var byteBuffer = buffers.Item3;

					//1. Dediscretization
					for( int i = 0, di = index * BlockVolume.N3; i < BlockVolume.N3; i++, di++ )
						doubleBuffer1[ i ] = i >= encodedLength ? 0 : encodedBlocks[ di ];

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
						byteBuffer[ i ] = (byte)Math.Max( byte.MinValue, Math.Min( byte.MaxValue, Math.Round( doubleBuffer1[ i ] ) ) );

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

		#endregion

		internal delegate void BlockAction( byte[] data, BlockIndex index );

		internal delegate bool BlockPredicate( BlockIndex index );

		internal delegate bool LayerPredicate( ushort index );
	}
}