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
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;
	using Zeiss.IMT.PiWeb.Volume.Interop;

	#endregion

	internal class SliceReader
	{
		#region members

		private readonly IReadOnlyList<VolumeSlice> _Slices;
		private readonly Direction _ReadDirection;
		private readonly IProgress<VolumeSliceDefinition> _ProgressNotifier;
		private readonly CancellationToken _Ct;

		private readonly ushort _SizeX;
		private readonly ushort _SizeY;
		private readonly ushort _SizeZ;

		private ushort _CurrentSlice;
		private byte[] _Buffer;

		#endregion

		#region constructors

		internal SliceReader( VolumeMetadata metadata, IReadOnlyList<VolumeSlice> slices, Direction readDirection = Direction.Z, IProgress<VolumeSliceDefinition> progressNotifier = null, CancellationToken ct = default )
		{
			_Slices = slices;
			_ReadDirection = readDirection;
			_ProgressNotifier = progressNotifier;
			_Ct = ct;
			_CurrentSlice = 0;

			_SizeX = metadata.SizeX;
			_SizeY = metadata.SizeY;
			_SizeZ = metadata.SizeZ;

			_Buffer = Array.Empty<byte>();

			Interop = new InteropSliceReader
			{
				ReadSlice = ReadSlice
			};
		}

		#endregion

		#region properties

		internal InteropSliceReader Interop { get; }

		#endregion

		#region methods

		private bool ReadSlice( IntPtr pv, ushort width, ushort height )
		{
			_Ct.ThrowIfCancellationRequested();

			return _ReadDirection switch
			{
				Direction.X => ReadInXDirection( pv, width, height ),
				Direction.Y => ReadInYDirection( pv, width, height ),
				Direction.Z => ReadInZDirection( pv, width, height ),
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		private bool ReadInXDirection( IntPtr pv, ushort width, ushort height )
		{
			if( _CurrentSlice >= _SizeX )
				return false;

			ReportProgress();

			var bufferSize = width * height;
			if( _Buffer.Length < bufferSize )
				_Buffer = new byte[bufferSize];

			var sx = _SizeX;

			Parallel.For( 0, Math.Min( _SizeZ, height ), z =>
			{
				var input = _Slices[ z ].Data;
				var output = _Buffer;
				int inputIndex = _CurrentSlice;
				int outputIndex = z * width;

				for( var y = 0; y < _SizeY; y++ )
				{
					output[ outputIndex ] = input[ inputIndex ];
					outputIndex++;
					inputIndex += sx;
				}
			} );

			Marshal.Copy( _Buffer, 0, pv, bufferSize );
			_CurrentSlice++;

			return true;
		}

		private bool ReadInYDirection( IntPtr pv, ushort width, ushort height )
		{
			if( _CurrentSlice >= _SizeY )
				return false;

			ReportProgress();

			Parallel.For( 0, Math.Min( _SizeZ, height ), z =>
			{
				var data = _Slices[ z ].Data;
				Marshal.Copy( data, _CurrentSlice * _SizeX, pv + z * width, _SizeX );
			} );
			_CurrentSlice++;

			return true;
		}


		private bool ReadInZDirection( IntPtr pv, ushort width, ushort height )
		{
			if( _CurrentSlice >= _SizeZ )
				return false;

			ReportProgress();

			var data = _Slices[ _CurrentSlice ].Data;

			// ReSharper disable once AccessToDisposedClosure
			Parallel.For( 0, Math.Min( _SizeY, height ), y => Marshal.Copy( data, y * _SizeX, pv + y * width, _SizeX ) );

			_CurrentSlice++;

			return true;
		}

		private void ReportProgress()
		{
			_ProgressNotifier?.Report( new VolumeSliceDefinition( _ReadDirection, _CurrentSlice ) );
		}

		#endregion
	}
}