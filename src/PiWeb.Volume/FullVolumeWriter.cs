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
	using System.Threading.Tasks;
	using Zeiss.IMT.PiWeb.Volume.Interop;

	#endregion

	internal class FullVolumeWriter
	{
		#region members

		private readonly IProgress<VolumeSliceDefinition> _ProgressNotifier;
		private readonly CancellationToken _Ct;

		private readonly List<Task<VolumeSlice>> _DataTasks;

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
			_DataTasks = new List<Task<VolumeSlice>>( _SizeZ );

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
			// ReSharper disable once CoVariantArrayConversion
			Task.WaitAll( _DataTasks.ToArray() );

			return _DataTasks
				.Select( t => t.Result )
				.ToArray();
		}

		private void WriteSlice( IntPtr line, ushort width, ushort height, ushort z )
		{
			_Ct.ThrowIfCancellationRequested();
			_ProgressNotifier?.Report( new VolumeSliceDefinition( Direction.Z, z ) );

			if( z >= _SizeZ )
				return;

			var length = _SizeX * _SizeY;
			var data = VolumeArrayPool.Shared.Rent( length );
			for( var y = 0; y < _SizeY; y++ )
			{
				Marshal.Copy( line + y * width, data, y * _SizeX, _SizeX );
			}

			_DataTasks.Add( Task.Run( () =>
			{
				var slice = new VolumeSlice( Direction.Z, z, length, data );
				VolumeArrayPool.Shared.Return( data );

				return slice;
			}, _Ct ) );
		}

		#endregion
	}
}