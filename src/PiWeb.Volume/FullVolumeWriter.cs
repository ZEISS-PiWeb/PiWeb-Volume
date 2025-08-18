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
	using System.Collections.Generic;
	using System.Runtime.InteropServices;
	using System.Threading;
	using Zeiss.PiWeb.Volume.Interop;

	#endregion

	internal class FullVolumeWriter
	{
		#region members

		private readonly IProgress<VolumeSliceDefinition>? _ProgressNotifier;
		private readonly CancellationToken _Ct;

		private readonly VolumeSlice[] _Slices;

		private readonly ushort _SizeX;
		private readonly ushort _SizeY;
		private readonly ushort _SizeZ;
		private readonly int _Length;

		#endregion

		#region constructors

		internal FullVolumeWriter( VolumeMetadata metadata, Direction direction, IProgress<VolumeSliceDefinition>? progressNotifier = null, CancellationToken ct = default )
		{
			ArgumentNullException.ThrowIfNull( metadata );

			_ProgressNotifier = progressNotifier;
			_Ct = ct;

			metadata.GetSliceSize( direction, out _SizeX, out _SizeY );

			_Length = _SizeX * _SizeY;
			_SizeZ = metadata.GetSize( direction );
			_Slices = new VolumeSlice[ _SizeZ ];

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

		internal IReadOnlyList<VolumeSlice> GetData()
		{
			return _Slices;
		}

		private void WriteSlice( IntPtr ptr, ushort width, ushort height, ushort z )
		{
			_Ct.ThrowIfCancellationRequested();
			_ProgressNotifier?.Report( new VolumeSliceDefinition( Direction.Z, z ) );

			if( z >= _SizeZ )
				return;

			var data = new byte[ _Length ];

			for( var y = 0; y < _SizeY; y++ )
			{
				Marshal.Copy( ptr + y * width, data, y * _SizeX, _SizeX );
			}

			_Slices[ z ] = new VolumeSlice( new VolumeSliceDefinition( Direction.Z, z ), data );
		}

		#endregion
	}
}