﻿#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume
{
	#region usings

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;
	using Zeiss.IMT.PiWeb.Volume.Interop;

	#endregion

	internal sealed class VolumeSliceRangeCollector
	{
		#region members

		private readonly Direction _Direction;
		private readonly IProgress<VolumeSliceDefinition> _ProgressNotifier;
		private readonly CancellationToken _Ct;

		private readonly Dictionary<ushort, byte[]> _SlicesX = new Dictionary<ushort, byte[]>();
		private readonly Dictionary<ushort, byte[]> _SlicesY = new Dictionary<ushort, byte[]>();
		private readonly Dictionary<ushort, byte[]> _SlicesZ = new Dictionary<ushort, byte[]>();

		private readonly ushort _X;
		private readonly ushort _Y;
		private readonly ushort _Z;

		#endregion

		#region constructors

		internal VolumeSliceRangeCollector( VolumeMetadata metaData, Direction direction, IReadOnlyCollection<VolumeSliceRangeDefinition> ranges, IProgress<VolumeSliceDefinition> progressNotifier = null, CancellationToken ct = default )
		{
			if( direction != Direction.Z && ranges.Any( r => r.Direction != direction ) )
				throw new ArgumentException( "Collecting slices ranges for different directions than input is only allowed for z-directed input" );

			_Direction = direction;
			_ProgressNotifier = progressNotifier;
			_Ct = ct;

			_X = metaData.SizeX;
			_Y = metaData.SizeY;
			_Z = metaData.SizeZ;

			foreach( var range in ranges )
			{
				var set = GetSlices( range.Direction );
				for( var i = range.First; i <= range.Last; i++ )
				{
					metaData.GetSliceSize( range.Direction, out var x, out var y );
					set[ i ] = new byte[x * y];
				}
			}

			Interop = new InteropSliceWriter
			{
				WriteSlice = WriteSlice
			};
		}

		#endregion

		#region properties

		internal InteropSliceWriter Interop { get; }

		#endregion

		#region methods

		internal VolumeSliceCollection GetSliceRangeCollection()
		{
			return new VolumeSliceCollection(
				_SlicesX.ToDictionary( k => k.Key, v => new VolumeSlice( Direction.X, v.Key, v.Value ) ),
				_SlicesY.ToDictionary( k => k.Key, v => new VolumeSlice( Direction.Y, v.Key, v.Value ) ),
				_SlicesZ.ToDictionary( k => k.Key, v => new VolumeSlice( Direction.Z, v.Key, v.Value ) )
			);
		}

		internal VolumeSliceRange GetSliceRange( VolumeSliceRangeDefinition definition )
		{
			var slices = new List<VolumeSlice>();
			var set = GetSlices( definition.Direction );
			for( var s = definition.First; s <= definition.Last; s++ )
			{
				if( set.TryGetValue( s, out var data ) )
					slices.Add( new VolumeSlice( definition.Direction, s, data ) );
				else
					throw new ArgumentOutOfRangeException( nameof(definition) );
			}

			return new VolumeSliceRange( definition, slices );
		}

		internal VolumeSlice GetSlice( Direction direction, ushort index )
		{
			var set = GetSlices( direction );
			if( set.TryGetValue( index, out var data ) )
				return new VolumeSlice( direction, index, data );

			throw new ArgumentOutOfRangeException( nameof(index) );
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private Dictionary<ushort, byte[]> GetSlices( Direction direction )
		{
			return direction switch
			{
				Direction.X => _SlicesX,
				Direction.Y => _SlicesY,
				Direction.Z => _SlicesZ,
				_ => throw new ArgumentOutOfRangeException( nameof(direction), direction, null )
			};
		}

		internal void WriteSlice( IntPtr slice, ushort width, ushort height, ushort index )
		{
			_Ct.ThrowIfCancellationRequested();

			switch( _Direction )
			{
				case Direction.X:
					WriteXSlice( slice, width, height, index );
					break;
				case Direction.Y:
					WriteYSlice( slice, width, height, index );
					break;
				case Direction.Z:
					WriteZSlice( slice, width, height, index );
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		internal void WriteXSlice( IntPtr slice, ushort width, ushort height, ushort x )
		{
			_ProgressNotifier?.Report( new VolumeSliceDefinition( Direction.X, x ) );

			if( x >= _X )
				return;

			if( !_SlicesX.TryGetValue( x, out var sliceX ) ) 
				return;
			
			for( var z = 0; z < _Z; z++ )
				Marshal.Copy( slice + z * width, sliceX, z * _Y, _Y );
		}

		internal void WriteYSlice( IntPtr slice, ushort width, ushort height, ushort y )
		{
			_ProgressNotifier?.Report( new VolumeSliceDefinition( Direction.Y, y ) );

			if( y >= _Y )
				return;

			if( !_SlicesY.TryGetValue( y, out var sliceY ) )
				return;

			Parallel.For( 0, _Z, z => Marshal.Copy( slice + z * width, sliceY, z * _X, _X ) );
		}

		internal void WriteZSlice( IntPtr slice, ushort width, ushort height, ushort z )
		{
			_ProgressNotifier?.Report( new VolumeSliceDefinition( Direction.Z, z ) );

			if( z >= _Z )
				return;

			if( _SlicesZ.TryGetValue( z, out var sliceZ ) )
				Parallel.For( 0, _Y, y => Marshal.Copy( slice + y * width, sliceZ, y * _X, _X ) );

			foreach( var slicey in _SlicesY )
			{
				var y = slicey.Key;
				Marshal.Copy( slice + y * width, slicey.Value, z * _X, _X );
			}

			if( !_SlicesX.Any() )
				return;

			foreach( var sliceX in _SlicesX )
			{
				var x = sliceX.Key;
				Parallel.For( 0, _Y, y => 
				{
					sliceX.Value[ y + z * _Y ] = Marshal.ReadByte( slice, x + width * y ); 
				} );
			}
		}

		#endregion
	}
}