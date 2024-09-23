#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

// ReSharper disable SuggestBaseTypeForParameter
namespace Zeiss.PiWeb.Volume.Block;

#region usings

using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

#endregion

/// <summary>
/// Decodes an encoded block volume.
/// </summary>
internal static class BlockVolumeDecoder
{
	#region methods

	/// <summary>
	/// Decodes a block volume from the given <paramref name="data"/>. The decoded blocks can be handled with the
	/// <paramref name="blockAction"/>. Use the <paramref name="layerPredicate"/> and
	/// <paramref name="blockPredicate"/> to decode only specific blocks or layers.
	/// </summary>
	internal static void Decode( byte[] data,
		BlockAction blockAction,
		LayerPredicate? layerPredicate = null,
		BlockPredicate? blockPredicate = null,
		IProgress<VolumeSliceDefinition>? progress = null,
		CancellationToken ct = default )
	{
		var reader = new BinaryReader( new MemoryStream( data ) );
		var (_, sizeX, sizeY, sizeZ, quantization) = BlockVolumeMetaData.Read( reader );
		var (bcx, bcy, bcz) = BlockVolume.GetBlockCount( sizeX, sizeY, sizeZ );
		var blockCount = bcx * bcy;
		var encodedBlockInfos = new EncodedBlockInfo[ blockCount ];

		for( ushort biz = 0; biz < bcz; biz++ )
		{
			ct.ThrowIfCancellationRequested();

			var layerLength = reader.ReadInt32();
			if( layerPredicate?.Invoke( biz ) is false )
			{
				reader.BaseStream.Seek( layerLength, SeekOrigin.Current );
				continue;
			}

			ReadBlockInfos( reader, blockCount, encodedBlockInfos );
			DecodeLayer( data, encodedBlockInfos, bcx, bcy, biz, quantization, blockPredicate, blockAction );

			progress?.Report( new VolumeSliceDefinition( Direction.Z, (ushort)( biz * BlockVolume.N ) ) );
		}
	}

	private static void ReadBlockInfos( BinaryReader reader, int blockCount, EncodedBlockInfo[] encodedBlockInfos )
	{
		for( var i = 0; i < blockCount; i++ )
		{
			var encodedBlockInfo = EncodedBlockInfo.Read( reader );
			reader.BaseStream.Seek( encodedBlockInfo.Info.Length, SeekOrigin.Current );
			encodedBlockInfos[ i ] = encodedBlockInfo;
		}
	}

	private static void DecodeLayer(
		byte[] encodedBlocks,
		EncodedBlockInfo[] encodedBlockInfos,
		ushort blockCountX,
		ushort blockCountY,
		ushort blockIndexZ,
		double[] quantization,
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
				ZigZag.Reverse( doubleBuffer1, doubleBuffer2 );

				//3. Quantization
				Quantization.Apply( quantization.AsSpan(), doubleBuffer2, doubleBuffer1 );

				//4. Cosine transform
				DiscreteCosineTransform.Transform( doubleBuffer1, doubleBuffer2, true );

				//5. Discretization
				for( var i = 0; i < BlockVolume.N3; i++ )
					byteBuffer[ i ] = (byte)Math.Clamp( doubleBuffer2[ i ], byte.MinValue, byte.MaxValue );

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

	private static void ReadBlock( ReadOnlySpan<byte> data, EncodedBlockInfo blockInfo, Span<double> result )
	{
		var encodedBlockData = data[ blockInfo.StartIndex.. ];

		result.Clear();

		if( blockInfo.Info.ValueCount == 0 )
			return;

		result[ 0 ] = blockInfo.Info.IsFirstValueShort
			? MemoryMarshal.Read<short>( encodedBlockData )
			: MemoryMarshal.Read<sbyte>( encodedBlockData );

		if( blockInfo.Info.ValueCount == 1 )
			return;

		if( blockInfo.Info.AreOtherValuesShort )
		{
			var values = MemoryMarshal.Cast<byte, short>( encodedBlockData[ blockInfo.Info.FirstValueSize.. ] );
			for( ushort i = 1, vi = 0; i < blockInfo.Info.ValueCount; i++, vi++ )
				result[ i ] = values[ vi ];
		}
		else
		{
			var values = MemoryMarshal.Cast<byte, sbyte>( encodedBlockData[ blockInfo.Info.FirstValueSize.. ] );
			for( ushort i = 1, vi = 0; i < blockInfo.Info.ValueCount; i++, vi++ )
				result[ i ] = values[ vi ];
		}
	}

	#endregion

	private readonly record struct EncodedBlockInfo( int StartIndex, BlockInfo Info )
	{
		#region methods

		/// <summary>
		/// Reads the encoded block info from the specified <paramref name="reader"/>
		/// </summary>
		public static EncodedBlockInfo Read( BinaryReader reader )
		{
			var info = BlockInfo.Read( reader );
			var startIndex = (int)reader.BaseStream.Position;

			return new EncodedBlockInfo( startIndex, info );
		}

		#endregion
	}

	internal delegate void BlockAction( ReadOnlySpan<byte> data, BlockIndex index );

	internal delegate bool BlockPredicate( BlockIndex index );

	internal delegate bool LayerPredicate( ushort index );
}