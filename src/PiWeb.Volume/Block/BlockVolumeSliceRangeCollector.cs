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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

#endregion

/// <summary>
/// Can be used to search through a block volume and extract single slices and slice ranges.
/// </summary>
internal class BlockVolumeSliceRangeCollector
{
	#region members

	private readonly BlockVolume _Volume;

	private readonly ushort _SizeZ;
	private readonly ushort _SizeY;
	private readonly ushort _SizeX;

	private readonly Dictionary<ushort, List<BlockSliceBuffer>> _SlicesX = new Dictionary<ushort, List<BlockSliceBuffer>>();
	private readonly Dictionary<ushort, List<BlockSliceBuffer>> _SlicesY = new Dictionary<ushort, List<BlockSliceBuffer>>();
	private readonly Dictionary<ushort, List<BlockSliceBuffer>> _SlicesZ = new Dictionary<ushort, List<BlockSliceBuffer>>();
	private readonly HashSet<VolumeRange> _VerticalRanges = [];
	private readonly VolumeMetadata _Metadata;

	#endregion

	#region constructors

	internal BlockVolumeSliceRangeCollector( BlockVolume volume, IReadOnlyCollection<VolumeSliceRangeDefinition> ranges )
	{
		_Volume = volume;
		_Metadata = _Volume.Metadata;

		_SizeZ = _Metadata.SizeZ;
		_SizeY = _Metadata.SizeY;
		_SizeX = _Metadata.SizeX;

		foreach( var range in ranges )
			AddRangeDefinition( range );

		if( _VerticalRanges.Count == 0 && ( _SlicesX.Count > 0 || _SlicesY.Count > 0 ) )
			_VerticalRanges.Add( new VolumeRange( 0, (ushort)( _Metadata.SizeZ - 1 ) ) );
	}

	#endregion

	#region methods

	private void AddRangeDefinition( VolumeSliceRangeDefinition range )
	{
		var map = GetSlices( range.Direction );

		_Metadata.GetSliceSize( range.Direction, out var width, out var height );

		foreach( var definition in range )
		{
			var block = (ushort)( definition.Index / BlockVolume.N );

			if( !map.TryGetValue( block, out var list ) )
				map[ block ] = list = new List<BlockSliceBuffer>();

			list.Add( new BlockSliceBuffer( definition, new byte[ width * height ] ) );

			if( definition.Direction != Direction.Z && definition.RegionOfInterest.HasValue )
				_VerticalRanges.Add( definition.RegionOfInterest.Value.V );
		}
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private Dictionary<ushort, List<BlockSliceBuffer>> GetSlices( Direction direction )
	{
		return direction switch
		{
			Direction.X => _SlicesX,
			Direction.Y => _SlicesY,
			Direction.Z => _SlicesZ,
			_           => throw new ArgumentOutOfRangeException( nameof( direction ), direction, null )
		};
	}

	internal VolumeSliceCollection CollectSliceRanges( IProgress<VolumeSliceDefinition>? progress, CancellationToken ct )
	{
		BlockVolumeDecoder.Decode( _Volume, Direction.Z, BlockAction, LayerPredicate, BlockPredicate, progress, ct );

		return new VolumeSliceCollection(
			_SlicesX.Values
				.SelectMany( list => list )
				.AsParallel()
				.AsOrdered()
				.ToDictionary( s => s.Definition.Index, s => s.ToVolumeSlice() ),
			_SlicesY.Values
				.SelectMany( list => list )
				.AsParallel()
				.AsOrdered()
				.ToDictionary( s => s.Definition.Index, s => s.ToVolumeSlice() ),
			_SlicesZ.Values
				.SelectMany( list => list )
				.AsParallel()
				.AsOrdered()
				.ToDictionary( s => s.Definition.Index, s => s.ToVolumeSlice() )
		);
	}

	private void BlockAction( ReadOnlySpan<byte> data, BlockIndex index )
	{
		if( _SlicesZ.TryGetValue( index.Z, out var slicesZ ) )
			ReadZSlices( data, index, slicesZ );

		if( _SlicesY.TryGetValue( index.Y, out var slicesY ) )
			ReadYSlices( data, index, slicesY );

		if( _SlicesX.TryGetValue( index.X, out var slicesX ) )
			ReadXSlices( data, index, slicesX );
	}

	private bool LayerPredicate( ushort layerIndex )
	{
		var vz = (ushort)( layerIndex * BlockVolume.N );
		var rz = new VolumeRange( vz, (ushort)( vz + BlockVolume.N - 1 ) );

		return _SlicesZ.ContainsKey( layerIndex ) || _VerticalRanges.Any( r => r.Intersects( rz ) );
	}

	private bool BlockPredicate( BlockIndex blockIndex )
	{
		var vx = (ushort)( blockIndex.X * BlockVolume.N );
		var vy = (ushort)( blockIndex.Y * BlockVolume.N );
		var vz = (ushort)( blockIndex.Z * BlockVolume.N );

		var rx = new VolumeRange( vx, (ushort)( vx + BlockVolume.N - 1 ) );
		var ry = new VolumeRange( vy, (ushort)( vy + BlockVolume.N - 1 ) );
		var rz = new VolumeRange( vz, (ushort)( vz + BlockVolume.N - 1 ) );

		return _SlicesZ.TryGetValue( blockIndex.Z, out var slices ) && SlicesContainRegion( slices, new VolumeRegion( rx, ry ) ) ||
			_SlicesY.TryGetValue( blockIndex.Y, out slices ) && SlicesContainRegion( slices, new VolumeRegion( rx, rz ) ) ||
			_SlicesX.TryGetValue( blockIndex.X, out slices ) && SlicesContainRegion( slices, new VolumeRegion( ry, rz ) );
	}

	private static bool SlicesContainRegion( List<BlockSliceBuffer> slices, VolumeRegion region )
	{
		// ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
		foreach( var slice in slices )
		{
			var roi = slice.Definition.RegionOfInterest;
			if( roi is null || roi.Value.U.Intersects( region.U ) && roi.Value.V.Intersects( region.V ) )
				return true;
		}

		return false;
	}

	private void ReadZSlices( ReadOnlySpan<byte> block, BlockIndex index, List<BlockSliceBuffer> slices )
	{
		foreach( var slice in slices )
			ReadZSlice( block, index, slice );
	}

	private void ReadZSlice( ReadOnlySpan<byte> block, BlockIndex index, BlockSliceBuffer slice )
	{
		var bz = slice.Definition.Index - index.Z * BlockVolume.N;
		if( bz is < 0 or > BlockVolume.N )
			throw new ArgumentOutOfRangeException( nameof( slice ) );

		var gz = index.Z * BlockVolume.N + bz;
		if( gz > _SizeZ )
			return;

		var sliceData = slice.Data.AsSpan();

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

	private void ReadYSlices( ReadOnlySpan<byte> block, BlockIndex index, List<BlockSliceBuffer> slices )
	{
		foreach( var slice in slices )
			ReadYSlice( block, index, slice );
	}

	private void ReadYSlice( ReadOnlySpan<byte> block, BlockIndex index, BlockSliceBuffer slice )
	{
		var by = slice.Definition.Index - index.Y * BlockVolume.N;
		if( by is < 0 or > BlockVolume.N )
			throw new ArgumentOutOfRangeException( nameof( slice ) );

		var gy = index.Y * BlockVolume.N + by;
		if( gy >= _SizeY )
			return;

		var sliceData = slice.Data.AsSpan();
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

	private void ReadXSlices( ReadOnlySpan<byte> block, BlockIndex index, List<BlockSliceBuffer> slices )
	{
		foreach( var slice in slices )
			ReadXSlice( block, index, slice );
	}

	private void ReadXSlice( ReadOnlySpan<byte> block, BlockIndex index, BlockSliceBuffer slice )
	{
		var bx = slice.Definition.Index - index.X * BlockVolume.N;
		if( bx is < 0 or > BlockVolume.N )
			throw new ArgumentOutOfRangeException( nameof( slice ) );

		var gx = index.X * BlockVolume.N + bx;
		if( gx >= _SizeX )
			return;

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

				slice.Data[ gz * _SizeY + gy ] = block[ bz * BlockVolume.N2 + by * BlockVolume.N + bx ];
			}
		}
	}

	#endregion

	#region struct BlockSliceBuffer

	private readonly struct BlockSliceBuffer( VolumeSliceDefinition definition, byte[] data )
	{
		#region properties

		/// <summary>
		/// The slice definition that is associated with this buffer.
		/// </summary>
		public VolumeSliceDefinition Definition { get; } = definition;

		/// <summary>
		/// The buffer that holds the slice data.
		/// </summary>
		public byte[] Data { get; } = data;

		#endregion

		#region methods

		/// <summary>
		/// Converts this buffer to a <see cref="VolumeSlice"/>.
		/// </summary>
		public VolumeSlice ToVolumeSlice()
		{
			return new VolumeSlice( Definition, Data );
		}

		#endregion
	}

	#endregion
}