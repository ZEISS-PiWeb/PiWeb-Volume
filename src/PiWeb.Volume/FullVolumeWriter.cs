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
	using System.Runtime.InteropServices;
	using System.Threading;
	using Zeiss.IMT.PiWeb.Volume.Interop;

	#endregion

	internal class FullVolumeWriter
	{
		#region members

		private readonly IProgress<VolumeSliceDefinition> _ProgressNotifier;
		private readonly CancellationToken _Ct;

		private readonly byte[][] _Data;

		private readonly ushort _SizeX;
		private readonly ushort _SizeY;
		private readonly ushort _SizeZ;

		#endregion

		#region constructors

		internal FullVolumeWriter( VolumeMetadata metadata, Direction direction, IProgress<VolumeSliceDefinition> progressNotifier = null, CancellationToken ct = default( CancellationToken ) )
		{
			if( metadata == null )
				throw new ArgumentNullException( nameof(metadata) );

			_ProgressNotifier = progressNotifier;
			_Ct = ct;

			metadata.GetSliceSize( direction, out _SizeX, out _SizeY );

			_SizeZ = metadata.GetSize( direction );

			_Data = new byte[_SizeZ][];

			for( var z = 0; z < _SizeZ; z++ )
				_Data[ z ] = new byte[_SizeX * _SizeY];

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

		internal byte[][] GetData()
		{
			return _Data;
		}

		internal void WriteSlice( IntPtr line, ushort width, ushort height, ushort z )
		{
			_Ct.ThrowIfCancellationRequested();

			if( z >= _SizeZ )
				return;

			_ProgressNotifier?.Report( new VolumeSliceDefinition( Direction.Z, z ) );

			for( var y = 0; y < _SizeY; y++ )
				Marshal.Copy( line + y * width, _Data[ z ], y * _SizeX, _SizeX );
		}

		#endregion
	}
}