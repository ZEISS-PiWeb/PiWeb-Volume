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
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
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
		var header = BlockVolumeMetaData.Create( data );
		var (_, sizeX, sizeY, sizeZ, quantization) = header;
		var (bcx, bcy, bcz) = BlockVolume.GetBlockCount( sizeX, sizeY, sizeZ );
		var blockCount = bcx * bcy;
		var encodedBlockInfos = new EncodedBlockInfo[ blockCount ];
		var position = BlockVolumeMetaData.HeaderLength;
		var dataSpan = data.AsSpan();

		Quantization.Invert( quantization );

		for( ushort biz = 0; biz < bcz; biz++ )
		{
			ct.ThrowIfCancellationRequested();

			var layerLength = MemoryMarshal.Read<int>( dataSpan.Slice( position, sizeof( int ) ) );
			position += sizeof( int );
			if( layerPredicate?.Invoke( biz ) is false )
			{
				position += layerLength;
				continue;
			}

			ReadLayer( dataSpan, position, blockCount, encodedBlockInfos );
			DecodeLayer( data, encodedBlockInfos, bcx, bcy, biz, quantization, blockPredicate, blockAction );

			progress?.Report( new VolumeSliceDefinition( Direction.Z, (ushort)( biz * BlockVolume.N ) ) );

			position += layerLength;
		}
	}

	private static void ReadLayer( ReadOnlySpan<byte> dataSpan, int position, int blockCount, EncodedBlockInfo[] encodedBlockInfos )
	{
		for( var i = 0; i < blockCount; i++ )
		{
			var value = MemoryMarshal.Read<ushort>( dataSpan[ position.. ] );

			position += sizeof( ushort );

			var encodedBlockInfo = new EncodedBlockInfo( position, BlockInfo.Create( value ) );
			encodedBlockInfos[ i ] = encodedBlockInfo;

			position += encodedBlockInfo.Info.Length;
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
#if DEBUG
		var buffers = new DecodingBuffers();
		for( var index = 0; index < blockCountX * blockCountY; index++ )
		{
#else
		Parallel.For( 0, blockCountX * blockCountY, () => new DecodingBuffers(), ( index, _, buffers ) =>
		{
#endif
			var blockIndexX = index % blockCountX;
			var blockIndexY = index / blockCountX;
			var blockIndex = new BlockIndex( (ushort)blockIndexX, (ushort)blockIndexY, blockIndexZ );

			if( blockPredicate?.Invoke( blockIndex ) == false )
#if DEBUG
				continue;
#else
				return buffers;
#endif

			var inputSpan = buffers.InputBuffer.AsSpan( 0, BlockVolume.N3 );
			var outputSpan = buffers.OutputBuffer.AsSpan( 0, BlockVolume.N3 );
			var resultSpan = buffers.ResultBuffer.AsSpan( 0, BlockVolume.N3 );
			var encodedBlockInfo = encodedBlockInfos[ index ];

			//1. Dediscretization
			ReadBlock( encodedBlocks.AsSpan(), encodedBlockInfo, inputSpan );

			//2. ZigZag
			ZigZag.Reverse( inputSpan, outputSpan );

			var nonEmptyVectors = FindNonEmptyVectors( outputSpan );

			//3. Quantization
			Quantization.Apply( quantization.AsSpan(), outputSpan );

			//4. Cosine transform
			DiscreteCosineTransform.Transform( outputSpan, inputSpan, true, nonEmptyVectors );

			//5. Discretization
			for( var i = 0; i < BlockVolume.N3; i++ )
				resultSpan[ i ] = (byte)Math.Clamp( inputSpan[ i ], byte.MinValue, byte.MaxValue );

			blockAction( resultSpan, blockIndex );
#if DEBUG
		}

		buffers.Return();
#else
			return buffers;
		}, buffers => buffers.Return() );
#endif
	}

	private static ulong FindNonEmptyVectors( Span<double> values )
	{
		var vectors = MemoryMarshal.Cast<double, Vector512<double>>( values );
		var result = 0UL;

		for( ushort i = 0; i < BlockVolume.N2; i++ )
		{
			if( Vector512.Sum( Vector512.Abs( vectors[ i ] ) ) > 1e-12 )
				result |= 1UL << i;
		}

		return result;
	}

	private static void ReadBlock( ReadOnlySpan<byte> data, EncodedBlockInfo blockInfo, Span<double> result )
	{
		var length = blockInfo.Info.Length;
		var encodedBlockData = data.Slice( blockInfo.StartIndex, length );

		result.Clear();

		if( blockInfo.Info.ValueCount == 0 )
			return;

		result[ 0 ] = blockInfo.Info.IsFirstValueShort
			? MemoryMarshal.Read<short>( encodedBlockData )
			: MemoryMarshal.Read<sbyte>( encodedBlockData );

		if( blockInfo.Info.ValueCount == 1 )
			return;

		var firstValueSize = blockInfo.Info.FirstValueSize;
		var otherValuesData = encodedBlockData.Slice( firstValueSize, length - firstValueSize );

		if( blockInfo.Info.AreOtherValuesShort )
		{
			var values = MemoryMarshal.Cast<byte, short>( otherValuesData );
			for( ushort i = 1, vi = 0; i < blockInfo.Info.ValueCount; i++, vi++ )
				result[ i ] = values[ vi ];
		}
		else
		{
			var values = MemoryMarshal.Cast<byte, sbyte>( otherValuesData );
			for( ushort i = 1, vi = 0; i < blockInfo.Info.ValueCount; i++, vi++ )
				result[ i ] = values[ vi ];
		}
	}

	#endregion

	private readonly struct DecodingBuffers()
	{
		public double[] InputBuffer { get; } = ArrayPool<double>.Shared.Rent( BlockVolume.N3 );
		public double[] OutputBuffer { get; } = ArrayPool<double>.Shared.Rent( BlockVolume.N3 );
		public byte[] ResultBuffer { get; } = ArrayPool<byte>.Shared.Rent( BlockVolume.N3 );

		public void Return()
		{
			ArrayPool<double>.Shared.Return( InputBuffer );
			ArrayPool<double>.Shared.Return( OutputBuffer );
			ArrayPool<byte>.Shared.Return( ResultBuffer );
		}
	}

	private readonly record struct EncodedBlockInfo( int StartIndex, BlockInfo Info );

	internal delegate void BlockAction( ReadOnlySpan<byte> data, BlockIndex index );

	internal delegate bool BlockPredicate( BlockIndex index );

	internal delegate bool LayerPredicate( ushort index );
}