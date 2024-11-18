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
	private readonly VolumeRange _RangeX;
	private readonly VolumeRange _RangeY;
	private readonly VolumeRange _RangeZ;
	private readonly ushort _Index;
	private readonly BlockVolumeDecoder.BlockAction _BlockAction;

	#endregion

	#region constructors

	internal BlockVolumeSliceCollector( BlockVolume volume, VolumeSliceDefinition definition, byte[] sliceBuffer )
	{
		_Volume = volume;
		_Index = definition.Index;
		_SliceBuffer = sliceBuffer;

		_SizeZ = volume.Metadata.SizeZ;
		_SizeY = volume.Metadata.SizeY;
		_SizeX = volume.Metadata.SizeX;

		var (bsx, bsy, bsz) = BlockVolume.GetBlockCount( _SizeX, _SizeY, _SizeZ );
		var blockIndex = (ushort)( definition.Index / BlockVolume.N );

		_RangeX = new VolumeRange( 0, (ushort)( bsx - 1 ) );
		_RangeY = new VolumeRange( 0, (ushort)( bsy - 1 ) );
		_RangeZ = new VolumeRange( 0, (ushort)( bsz - 1 ) );

		switch( definition.Direction )
		{
			case Direction.Z:
			{
				_RangeZ = new VolumeRange( blockIndex, blockIndex );
				_BlockAction = ReadZSlice;
				if( definition.RegionOfInterest is not { } region )
					break;

				_RangeX = new VolumeRange(
					(ushort)( region.U.Start / BlockVolume.N ),
					(ushort)( region.U.End / BlockVolume.N ) );
				_RangeY = new VolumeRange(
					(ushort)( region.V.Start / BlockVolume.N ),
					(ushort)( region.V.End / BlockVolume.N ) );

				break;
			}

			case Direction.Y:
			{
				_RangeY = new VolumeRange( blockIndex, blockIndex );
				_BlockAction = ReadYSlice;

				if( definition.RegionOfInterest is not { } region )
					break;

				_RangeX = new VolumeRange(
					(ushort)( region.U.Start / BlockVolume.N ),
					(ushort)( region.U.End / BlockVolume.N ) );
				_RangeZ = new VolumeRange(
					(ushort)( region.V.Start / BlockVolume.N ),
					(ushort)( region.V.End / BlockVolume.N ) );

				break;
			}

			case Direction.X:
			{
				_RangeX = new VolumeRange( blockIndex, blockIndex );
				_BlockAction = ReadXSlice;

				if( definition.RegionOfInterest is not { } region )
					break;

				_RangeY = new VolumeRange(
					(ushort)( region.U.Start / BlockVolume.N ),
					(ushort)( region.U.End / BlockVolume.N ) );
				_RangeZ = new VolumeRange(
					(ushort)( region.V.Start / BlockVolume.N ),
					(ushort)( region.V.End / BlockVolume.N ) );

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
		if( _Volume.CompressedData[ Direction.Z ] is not { } data )
			throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

		BlockVolumeDecoder.Decode( data, _BlockAction, LayerPredicate, BlockPredicate, progress, ct );
	}

	private bool LayerPredicate( ushort layerIndex )
	{
		return _RangeZ.Contains( layerIndex );
	}

	private bool BlockPredicate( BlockIndex blockIndex )
	{
		//Z is already tested by the layer predicate
		return _RangeX.Contains( blockIndex.X ) && _RangeY.Contains( blockIndex.Y );
	}

	private void ReadZSlice( ReadOnlySpan<byte> block, BlockIndex index )
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

	private void ReadYSlice( ReadOnlySpan<byte> block, BlockIndex index )
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

	private void ReadXSlice( ReadOnlySpan<byte> block, BlockIndex index )
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