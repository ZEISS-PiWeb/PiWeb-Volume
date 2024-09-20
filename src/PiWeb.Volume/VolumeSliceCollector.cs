#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume
{
	#region usings

	using System;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;
	using Zeiss.PiWeb.Volume.Interop;

	#endregion

	internal sealed class VolumeSliceCollector
	{
		#region members

		private readonly Direction _VolumeDirection;
		private readonly VolumeSliceDefinition _Slice;
		private readonly byte[] _SliceBuffer;
		private readonly IProgress<VolumeSliceDefinition>? _ProgressNotifier;
		private readonly CancellationToken _Ct;
		private readonly ushort _X;
		private readonly ushort _Y;
		private readonly ushort _Z;

		#endregion

		#region constructors

		internal VolumeSliceCollector(
			VolumeMetadata metaData,
			Direction volumeDirection,
			VolumeSliceDefinition slice,
			byte[] sliceBuffer,
			IProgress<VolumeSliceDefinition>? progressNotifier = null,
			CancellationToken ct = default )
		{
			_VolumeDirection = volumeDirection;
			_Slice = slice;
			_SliceBuffer = sliceBuffer;
			_ProgressNotifier = progressNotifier;
			_Ct = ct;

			_X = metaData.SizeX;
			_Y = metaData.SizeY;
			_Z = metaData.SizeZ;

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

		private void WriteSlice( IntPtr slice, ushort width, ushort height, ushort index )
		{
			_Ct.ThrowIfCancellationRequested();

			switch( _VolumeDirection )
			{
				case Direction.X:

					if( index == _Slice.Index )
						WriteXSlice( slice, width, index );
					break;
				case Direction.Y:
					if( index == _Slice.Index )
						WriteYSlice( slice, width, index );
					break;
				case Direction.Z:
					WriteZSlice( slice, width, index );
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void WriteXSlice( IntPtr slice, ushort width, ushort x )
		{
			_ProgressNotifier?.Report( new VolumeSliceDefinition( Direction.X, x ) );

			if( x >= _X )
				return;

			for( var z = 0; z < _Z; z++ )
				Marshal.Copy( slice + z * width, _SliceBuffer, z * _Y, _Y );
		}

		private void WriteYSlice( IntPtr slice, ushort width, ushort y )
		{
			_ProgressNotifier?.Report( new VolumeSliceDefinition( Direction.Y, y ) );

			if( y >= _Y )
				return;

			Parallel.For( 0, _Z, z => Marshal.Copy( slice + z * width, _SliceBuffer, z * _X, _X ) );
		}

		private void WriteZSlice( IntPtr slice, ushort width, ushort z )
		{
			_ProgressNotifier?.Report( new VolumeSliceDefinition( Direction.Z, z ) );

			if( z >= _Z )
				return;

			var index = _Slice.Index;

			switch( _Slice.Direction )
			{
				case Direction.X:
					Parallel.For( 0, _Y, y => { _SliceBuffer[ y + z * _Y ] = Marshal.ReadByte( slice, index + width * y ); } );
					break;
				case Direction.Y:
					Marshal.Copy( slice + index * width, _SliceBuffer, z * _X, _X );
					break;
				case Direction.Z:
					if( index == z )
						Parallel.For( 0, _Y, y => Marshal.Copy( slice + y * width, _SliceBuffer, y * _X, _X ) );
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion
	}
}