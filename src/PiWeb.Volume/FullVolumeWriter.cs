#region Copyright

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
	using System.Runtime.InteropServices;
	using System.Threading;
	using Zeiss.IMT.PiWeb.Volume.Interop;

	#endregion

	internal class FullVolumeWriter
	{
		#region members

		private readonly IProgress<VolumeSliceDefinition> _ProgressNotifier;
		private readonly CancellationToken _Ct;

		private readonly VolumeSlice[] _Data;

		private readonly ushort _SizeX;
		private readonly ushort _SizeY;
		private readonly ushort _SizeZ;

		#endregion

		#region constructors

		internal FullVolumeWriter( VolumeMetadata metadata, Direction direction, IProgress<VolumeSliceDefinition> progressNotifier = null, CancellationToken ct = default )
		{
			if( metadata == null )
				throw new ArgumentNullException( nameof(metadata) );

			_ProgressNotifier = progressNotifier;
			_Ct = ct;

			metadata.GetSliceSize( direction, out _SizeX, out _SizeY );

			_SizeZ = metadata.GetSize( direction );
			_Data = VolumeSliceHelper.CreateSliceData( _SizeX,_SizeY, _SizeZ );

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
			return _Data;
		}

		internal void WriteSlice( IntPtr line, ushort width, ushort height, ushort z )
		{
			_Ct.ThrowIfCancellationRequested();
			_ProgressNotifier?.Report( new VolumeSliceDefinition( Direction.Z, z ) );

			if( z >= _SizeZ )
				return;

			var data = _Data[ z ].Data;
			for( var y = 0; y < _SizeY; y++ )
			{
				Marshal.Copy( line + y * width, data, y * _SizeX, _SizeX );
			}
		}

		#endregion
	}
}