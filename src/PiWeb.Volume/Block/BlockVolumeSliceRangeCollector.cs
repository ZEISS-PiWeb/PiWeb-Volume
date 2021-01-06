#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.Block
{
	#region usings

	using System;
	using System.Collections.Generic;
	using System.IO;
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

		private readonly Dictionary<ushort, List<VolumeSliceBuffer>> _SlicesX = new Dictionary<ushort, List<VolumeSliceBuffer>>();
		private readonly Dictionary<ushort, List<VolumeSliceBuffer>> _SlicesY = new Dictionary<ushort, List<VolumeSliceBuffer>>();
		private readonly Dictionary<ushort, List<VolumeSliceBuffer>> _SlicesZ = new Dictionary<ushort, List<VolumeSliceBuffer>>();
		private readonly VolumeMetadata _Metadata;

		#endregion

		#region constructors

		internal BlockVolumeSliceRangeCollector( BlockVolume volume, VolumeSliceDefinition definition, VolumeSliceBuffer sliceBuffer )
		{
			_Volume = volume;
			_Metadata = _Volume.Metadata;

			_SizeZ = _Metadata.SizeZ;
			_SizeY = _Metadata.SizeY;
			_SizeX = _Metadata.SizeX;

			var map = GetSlices( definition.Direction );
			_Metadata.GetSliceSize( definition.Direction, out var width, out var height );
			sliceBuffer.Initialize( definition, width * height );
			
			var block = ( ushort ) ( definition.Index / BlockVolume.N );
			map[ block ] = new List<VolumeSliceBuffer> { sliceBuffer }; 
		}

		internal BlockVolumeSliceRangeCollector( BlockVolume volume, IReadOnlyCollection<VolumeSliceRangeDefinition> ranges )
		{
			_Volume = volume;
			_Metadata = _Volume.Metadata;

			_SizeZ = _Metadata.SizeZ;
			_SizeY = _Metadata.SizeY;
			_SizeX = _Metadata.SizeX;

			foreach( var range in ranges )
			{
				var map = GetSlices( range.Direction );

				_Metadata.GetSliceSize( range.Direction, out var width, out var height );

				foreach( var definition in range )
				{
					var block = ( ushort ) ( definition.Index / BlockVolume.N );

					if( !map.TryGetValue( block, out var list ) )
						map[ block ] = list = new List<VolumeSliceBuffer>();

					list.Add( new VolumeSliceBuffer( definition, width * height ) );
				}
			}
		}

		#endregion

		#region methods

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private Dictionary<ushort, List<VolumeSliceBuffer>> GetSlices( Direction direction )
		{
			return direction switch
			{
				Direction.X => _SlicesX,
				Direction.Y => _SlicesY,
				Direction.Z => _SlicesZ,
				_ => throw new ArgumentOutOfRangeException( nameof(direction), direction, null )
			};
		}

		internal VolumeSliceCollection CollectSliceRanges( IProgress<VolumeSliceDefinition> progress, CancellationToken ct )
		{
			if( _Volume.CompressedData[ Direction.Z ] == null )
				throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

			var decoder = new BlockVolumeDecoder( _Volume.CompressionOptions );
			var data = _Volume.CompressedData[ Direction.Z ];
			var input = new MemoryStream( data );

			decoder.Decode( input, _Metadata, ( block, index ) =>
				{
					if( _SlicesZ.TryGetValue( index.Z, out var slicesZ ) )
						ReadZSlices( block, index, slicesZ );

					if( _SlicesY.TryGetValue( index.Y, out var slicesY ) )
						ReadYSlices( block, index, slicesY );

					if( _SlicesX.TryGetValue( index.X, out var slicesX ) )
						ReadXSlices( block, index, slicesX );
				},
				layerIndex => _SlicesZ.ContainsKey( layerIndex ) || _SlicesX.Count > 0 || _SlicesY.Count > 0,
				blockIndex => _SlicesZ.ContainsKey( blockIndex.Z ) || _SlicesY.ContainsKey( blockIndex.Y ) ||
				              _SlicesX.ContainsKey( blockIndex.X ), progress, ct );

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

		private void ReadZSlices( byte[] block, BlockIndex index, List<VolumeSliceBuffer> slices )
		{
			foreach( var slice in slices )
				ReadZSlice( block, index, slice );
		}

		private void ReadZSlice( byte[] block, BlockIndex index, VolumeSliceBuffer slice )
		{
			var bz = slice.Definition.Index - index.Z * BlockVolume.N;
			if( bz < 0 || bz > BlockVolume.N )
				throw new ArgumentOutOfRangeException( nameof(slice) );

			for( var by = 0; by < BlockVolume.N; by++ )
			for( var bx = 0; bx < BlockVolume.N; bx++ )
			{
				var gz = index.Z * BlockVolume.N + bz;
				var gy = index.Y * BlockVolume.N + by;
				var gx = index.X * BlockVolume.N + bx;

				if( gz >= _SizeZ || gy >= _SizeY || gx >= _SizeX )
					continue;

				slice.Data[ gy * _SizeX + gx ] = block[ bz * BlockVolume.N2 + by * BlockVolume.N + bx ];
			}
		}

		private void ReadYSlices( byte[] block, BlockIndex index, List<VolumeSliceBuffer> slices )
		{
			foreach( var slice in slices )
				ReadYSlice( block, index, slice );
		}

		private void ReadYSlice( byte[] block, BlockIndex index, VolumeSliceBuffer slice )
		{
			var by = slice.Definition.Index - index.Y * BlockVolume.N;
			if( by < 0 || by > BlockVolume.N )
				throw new ArgumentOutOfRangeException( nameof(slice) );

			for( var bz = 0; bz < BlockVolume.N; bz++ )
			for( var bx = 0; bx < BlockVolume.N; bx++ )
			{
				var gz = index.Z * BlockVolume.N + bz;
				var gy = index.Y * BlockVolume.N + by;
				var gx = index.X * BlockVolume.N + bx;

				if( gz >= _SizeZ || gy >= _SizeY || gx >= _SizeX )
					continue;

				slice.Data[ gz * _SizeX + gx ] = block[ bz * BlockVolume.N2 + by * BlockVolume.N + bx ];
			}
		}

		private void ReadXSlices( byte[] block, BlockIndex index, List<VolumeSliceBuffer> slices )
		{
			foreach( var slice in slices )
				ReadXSlice( block, index, slice );
		}

		private void ReadXSlice( byte[] block, BlockIndex index, VolumeSliceBuffer slice )
		{
			var bx = slice.Definition.Index - index.X * BlockVolume.N;
			if( bx < 0 || bx > BlockVolume.N )
				throw new ArgumentOutOfRangeException( nameof(slice) );

			for( var bz = 0; bz < BlockVolume.N; bz++ )
			for( var by = 0; by < BlockVolume.N; by++ )
			{
				var gz = index.Z * BlockVolume.N + bz;
				var gy = index.Y * BlockVolume.N + by;
				var gx = index.X * BlockVolume.N + bx;

				if( gz >= _SizeZ || gy >= _SizeY || gx >= _SizeX )
					continue;

				slice.Data[ gz * _SizeY + gy ] = block[ bz * BlockVolume.N2 + by * BlockVolume.N + bx ];
			}
		}

		#endregion
	}
}