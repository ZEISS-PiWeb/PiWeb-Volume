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
	/// Decodes the blocks from the given <paramref name="volume"/>. The decoded blocks can be handled with the
	/// <paramref name="blockAction"/>. Use the <paramref name="layerPredicate"/> and
	/// <paramref name="blockPredicate"/> to decode only specific blocks or layers.
	/// </summary>
	internal static void Decode(
		BlockVolume volume,
		Direction direction,
		BlockAction blockAction,
		LayerPredicate? layerPredicate = null,
		BlockPredicate? blockPredicate = null,
		IProgress<VolumeSliceDefinition>? progress = null,
		CancellationToken ct = default )
	{
		var metaData = volume.BlockVolumeMetaData;
		var quantization = volume.InverseQuantization;
		var blockInfos = volume.EncodedBlockInfos;
		var data = volume.Data;

		var (bcx, bcy, bcz) = metaData.GetBlockCount();
		var layerCount = metaData.GetBlockCount( direction );
		var (u, v) = metaData.GetLayerBlockCount( direction );
		var blockInfoLayer = new BlockVolume.EncodedBlockInfo[ u * v ];

		for( ushort layer = 0; layer < layerCount; layer++ )
		{
			ct.ThrowIfCancellationRequested();
			if( layerPredicate?.Invoke( layer ) is false )
				continue;

			GetBlockInfoLayer( direction, layer, bcx, bcy, bcz, blockInfos, blockInfoLayer );
			DecodeLayer( data, blockInfoLayer, direction, u, layer, quantization, blockPredicate, blockAction );

			progress?.Report( new VolumeSliceDefinition( Direction.Z, (ushort)( layer * BlockVolume.N ) ) );
		}
	}

	private static void GetBlockInfoLayer(
		Direction direction,
		ushort layer,
		ushort bcx,
		ushort bcy,
		ushort bcz,
		ReadOnlySpan<BlockVolume.EncodedBlockInfo> allInfos,
		BlockVolume.EncodedBlockInfo[] blockInfoLayer )
	{
		switch( direction )
		{
			case Direction.Z:
				allInfos.Slice( layer * bcx * bcy, bcx * bcy ).CopyTo( blockInfoLayer.AsSpan() );
				break;
			case Direction.Y:
				for( var z = 0; z < bcz; z++ )
					allInfos.Slice( z * bcx * bcy + layer * bcx, bcx ).CopyTo( blockInfoLayer.AsSpan().Slice( z * bcx, bcx ) );
				break;
			case Direction.X:
				for( var z = 0; z < bcz; z++ )
				for( var y = 0; y < bcy; y++ )
					blockInfoLayer[ z * bcy + y ] = allInfos[ z * bcx * bcy + y * bcx + layer ];
				break;
			default:
				throw new ArgumentOutOfRangeException( nameof( direction ), direction, null );
		}
	}

	private static void DecodeLayer(
		byte[] encodedBlocks,
		BlockVolume.EncodedBlockInfo[] encodedBlockInfos,
		Direction direction,
		ushort stride,
		ushort layerIndex,
		double[] quantization,
		BlockPredicate? blockPredicate,
		BlockAction blockAction )
	{
#if DEBUG
		var buffers = new DecodingBuffers();
		for( var index = 0; index < encodedBlockInfos.Length; index++ )
		{
#else
		Parallel.For( 0, encodedBlockInfos.Length, () => new DecodingBuffers(), ( index, _, buffers ) =>
		{
#endif
			var blockIndex = direction switch
			{
				Direction.X => new BlockIndex( layerIndex, (ushort)( index % stride ), (ushort)( index / stride ) ),
				Direction.Y => new BlockIndex( (ushort)( index % stride ), layerIndex, (ushort)( index / stride ) ),
				Direction.Z => new BlockIndex( (ushort)( index % stride ), (ushort)( index / stride ), layerIndex ),
				_           => throw new ArgumentOutOfRangeException( nameof( direction ), direction, null )
			};

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

	private static void ReadBlock( ReadOnlySpan<byte> data, BlockVolume.EncodedBlockInfo blockInfo, Span<double> result )
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

	internal delegate void BlockAction( ReadOnlySpan<byte> data, BlockIndex index );

	internal delegate bool BlockPredicate( BlockIndex index );

	internal delegate bool LayerPredicate( ushort index );
}