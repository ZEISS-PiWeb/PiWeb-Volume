#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Block;

#region usings

using System;
using System.Threading;

#endregion

/// <summary>
/// Can be used to search through a block volume and extract single slices and slice ranges.
/// </summary>
internal class BlockVolumeSliceCollector
{
	#region members

	private readonly BlockVolume _Volume;

	private readonly ushort _SizeZ;
	private readonly ushort _SizeY;
	private readonly ushort _SizeX;
	private readonly byte[] _SliceBuffer;
	private readonly VolumeRange? _RangeU;
	private readonly VolumeRange? _RangeV;
	private readonly ushort _Index;
	private readonly BlockVolumeDecoder.BlockAction _BlockAction;
	private readonly Direction _Direction;
	private readonly ushort _BlockLayer;
	private readonly BlockVolumeDecoder.BlockPredicate _BlockPredicate;

	#endregion

	#region constructors

	internal BlockVolumeSliceCollector( BlockVolume volume, VolumeSliceDefinition definition, byte[] sliceBuffer )
	{
		_Volume = volume;
		_Index = definition.Index;
		_Direction = definition.Direction;
		_SliceBuffer = sliceBuffer;

		_SizeZ = volume.Metadata.SizeZ;
		_SizeY = volume.Metadata.SizeY;
		_SizeX = volume.Metadata.SizeX;

		_BlockLayer = (ushort)( definition.Index / BlockVolume.N );

		if( definition.RegionOfInterest is { } region )
		{
			_RangeU = new VolumeRange(
				(ushort)( region.U.Start / BlockVolume.N ),
				(ushort)( region.U.End / BlockVolume.N ) );
			_RangeV = new VolumeRange(
				(ushort)( region.V.Start / BlockVolume.N ),
				(ushort)( region.V.End / BlockVolume.N ) );
		}


		switch( definition.Direction )
		{
			case Direction.Z:
			{
				_BlockPredicate = BlockPredicateZ;
				_BlockAction = BlockActionZ;
				break;
			}

			case Direction.Y:
			{
				_BlockPredicate = BlockPredicateY;
				_BlockAction = BlockActionY;
				break;
			}

			case Direction.X:
			{
				_BlockPredicate = BlockPredicateX;
				_BlockAction = BlockActionX;
				break;
			}

			default:
				throw new ArgumentOutOfRangeException( $"Unsupported definition direction {definition.Direction}" );
		}
	}

	#endregion

	#region methods

	internal void CollectSlice( IProgress<VolumeSliceDefinition>? progress, CancellationToken ct )
	{
		BlockVolumeDecoder.Decode( _Volume, _Direction, _BlockAction, LayerPredicate, _BlockPredicate, progress, ct );
	}

	private bool LayerPredicate( ushort layerIndex )
	{
		return _BlockLayer == layerIndex;
	}

	private bool BlockPredicateX( BlockIndex blockIndex )
	{
		return
			( _RangeU is null || _RangeU.Value.Contains( blockIndex.Y ) ) &&
			( _RangeV is null || _RangeV.Value.Contains( blockIndex.Z ) );
	}

	private bool BlockPredicateY( BlockIndex blockIndex )
	{
		return
			( _RangeU is null || _RangeU.Value.Contains( blockIndex.X ) ) &&
			( _RangeV is null || _RangeV.Value.Contains( blockIndex.Z ) );
	}

	private bool BlockPredicateZ( BlockIndex blockIndex )
	{
		return
			( _RangeU is null || _RangeU.Value.Contains( blockIndex.X ) ) &&
			( _RangeV is null || _RangeV.Value.Contains( blockIndex.Y ) );
	}

	private void BlockActionZ( ReadOnlySpan<byte> block, BlockIndex index )
	{
		var bz = _Index - index.Z * BlockVolume.N;
		if( bz is < 0 or > BlockVolume.N )
			throw new ArgumentOutOfRangeException( nameof( index ) );

		var gz = index.Z * BlockVolume.N + bz;
		if( gz > _SizeZ )
			return;

		var sliceData = _SliceBuffer.AsSpan();

		var gx = index.X * BlockVolume.N;
		var sx = Math.Min( BlockVolume.N, _SizeX - gx );

		for( var by = 0; by < BlockVolume.N; by++ )
		{
			var gy = index.Y * BlockVolume.N + by;
			if( gy >= _SizeY )
				continue;

			block.Slice( bz * BlockVolume.N2 + by * BlockVolume.N, sx ).CopyTo( sliceData.Slice( gy * _SizeX + gx, sx ) );
		}
	}

	private void BlockActionY( ReadOnlySpan<byte> block, BlockIndex index )
	{
		var by = _Index - index.Y * BlockVolume.N;
		if( by is < 0 or > BlockVolume.N )
			throw new ArgumentOutOfRangeException( nameof( index ) );

		var gy = index.Y * BlockVolume.N + by;
		if( gy >= _SizeY )
			return;

		var sliceData = _SliceBuffer.AsSpan();
		var gx = index.X * BlockVolume.N;
		var sx = Math.Min( BlockVolume.N, _SizeX - gx );

		for( var bz = 0; bz < BlockVolume.N; bz++ )
		{
			var gz = index.Z * BlockVolume.N + bz;
			if( gz >= _SizeZ )
				continue;

			block.Slice( bz * BlockVolume.N2 + by * BlockVolume.N, sx ).CopyTo( sliceData.Slice( gz * _SizeX + gx, sx ) );
		}
	}

	private void BlockActionX( ReadOnlySpan<byte> block, BlockIndex index )
	{
		var bx = _Index - index.X * BlockVolume.N;
		if( bx is < 0 or > BlockVolume.N )
			throw new ArgumentOutOfRangeException( nameof( index ) );

		var gx = index.X * BlockVolume.N + bx;
		if( gx >= _SizeX )
			return;

		var data = _SliceBuffer.AsSpan();

		for( var bz = 0; bz < BlockVolume.N; bz++ )
		{
			var gz = index.Z * BlockVolume.N + bz;
			if( gz >= _SizeZ )
				continue;

			for( var by = 0; by < BlockVolume.N; by++ )
			{
				var gy = index.Y * BlockVolume.N + by;
				if( gy >= _SizeY )
					continue;

				data[ gz * _SizeY + gy ] = block[ bz * BlockVolume.N2 + by * BlockVolume.N + bx ];
			}
		}
	}

	#endregion
}