#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume
{
	#region usings

	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;
	using Zeiss.PiWeb.Volume.Interop;

	#endregion

	/// <summary>
	/// This class is responsible for creating previews. Previews are smaller versions of a volume where every
	/// n'th voxel is sampled.
	/// </summary>
	internal class PreviewCreator
	{
		#region members

		private readonly VolumeMetadata _Metadata;
		private readonly ushort _Minification;
		private readonly IProgress<VolumeSliceDefinition> _Progress;
		private readonly CancellationToken _Ct;

		private readonly List<Task<VolumeSlice>> _PreviewData;

		private readonly ushort _PreviewSizeX;
		private readonly ushort _PreviewSizeY;
		private readonly ushort _PreviewSizeZ;

		#endregion

		#region constructors

		internal PreviewCreator( VolumeMetadata metadata, ushort minification, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			_Metadata = metadata ?? throw new ArgumentNullException( nameof( metadata ) );
			_Minification = minification;
			_Progress = progress;
			_Ct = ct;

			_PreviewSizeX = (ushort)( ( metadata.SizeX - 1 ) / minification + 1 );
			_PreviewSizeY = (ushort)( ( metadata.SizeY - 1 ) / minification + 1 );
			_PreviewSizeZ = (ushort)( ( metadata.SizeZ - 1 ) / minification + 1 );

			if( _PreviewSizeX < 2 || _PreviewSizeY < 2 || _PreviewSizeZ < 2 )
				throw new ArgumentOutOfRangeException( nameof( minification ) );

			_PreviewData = new List<Task<VolumeSlice>>( _PreviewSizeZ );

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

		internal static UncompressedVolume CreatePreview( IReadOnlyList<VolumeSlice> slices, VolumeMetadata metadata, ushort minification )
		{
			var sizeX = metadata.SizeX;
			var sizeZ = metadata.SizeZ;

			var previewSizeX = (ushort)( ( metadata.SizeX - 1 ) / minification + 1 );
			var previewSizeY = (ushort)( ( metadata.SizeY - 1 ) / minification + 1 );
			var previewSizeZ = (ushort)( ( metadata.SizeZ - 1 ) / minification + 1 );

			if( previewSizeX < 2 || previewSizeY < 2 || previewSizeZ < 2 )
				throw new ArgumentOutOfRangeException( nameof( minification ) );

			var result = new List<Task<VolumeSlice>>( previewSizeZ );

			for( ushort z = 0, oz = 0; z < previewSizeZ && oz < sizeZ; z++, oz += minification )
			{
				var slice = slices[ oz ];
				var previewSlizeIndex = z;

				result.Add( Task.Run( () => CreateMinifiedSlice( previewSlizeIndex, minification, previewSizeX, previewSizeY, sizeX, slice.Data ) ) );
			}

			var volumeMetadata = new VolumeMetadata(
				previewSizeX,
				previewSizeY,
				previewSizeZ,
				metadata.ResolutionX * minification,
				metadata.ResolutionY * minification,
				metadata.ResolutionZ * minification );

			return Volume.CreateUncompressed( volumeMetadata, CreateVolumeSlices( result ) );
		}

		private static VolumeSlice[] CreateVolumeSlices( List<Task<VolumeSlice>> result )
		{
			// ReSharper disable once CoVariantArrayConversion
			Task.WaitAll( result.ToArray() );

			return result
				.Select( t => t.Result )
				.ToArray();
		}

		internal static UncompressedVolume CreatePreview( Stream stream, VolumeMetadata metadata, ushort minification, IProgress<VolumeSliceDefinition> progress )
		{
			var sizeX = metadata.SizeX;
			var sizeY = metadata.SizeY;
			var sizeZ = metadata.SizeZ;
			var previewSizeX = (ushort)( ( metadata.SizeX - 1 ) / minification + 1 );
			var previewSizeY = (ushort)( ( metadata.SizeY - 1 ) / minification + 1 );
			var previewSizeZ = (ushort)( ( metadata.SizeZ - 1 ) / minification + 1 );

			if( previewSizeX < 2 || previewSizeY < 2 || previewSizeZ < 2 )
				throw new ArgumentOutOfRangeException( nameof( minification ) );

			var sliceBufferSize = sizeX * sizeY;

			var result = new List<Task<VolumeSlice>>( previewSizeZ );
			for( ushort oz = 0, z = 0; oz < sizeZ && z < previewSizeZ; oz++ )
			{
				progress?.Report( new VolumeSliceDefinition( Direction.Z, oz ) );

				if( oz % minification == 0 )
				{
					var sliceBuffer = VolumeArrayPool.Shared.Rent( sliceBufferSize );
					stream.Read( sliceBuffer, 0, sliceBufferSize );

					var previewSlizeIndex = z;
					result.Add( Task.Run( () =>
					{
						var previewSlice = CreateMinifiedSlice( previewSlizeIndex, minification, previewSizeX, previewSizeY, sizeX, sliceBuffer );
						VolumeArrayPool.Shared.Return( sliceBuffer );

						return previewSlice;
					} ) );

					z++;
				}
				else
				{
					stream.Seek( sliceBufferSize, SeekOrigin.Current );
				}
			}

			var volumeMetadata = new VolumeMetadata(
				previewSizeX,
				previewSizeY,
				previewSizeZ,
				metadata.ResolutionX * minification,
				metadata.ResolutionY * minification,
				metadata.ResolutionZ * minification );

			return Volume.CreateUncompressed( volumeMetadata, CreateVolumeSlices( result ) );
		}

		internal UncompressedVolume GetPreview()
		{
			var volumeMetadata = new VolumeMetadata(
				_PreviewSizeX,
				_PreviewSizeY,
				_PreviewSizeZ,
				_Metadata.ResolutionX * _Minification,
				_Metadata.ResolutionY * _Minification,
				_Metadata.ResolutionZ * _Minification );

			return Volume.CreateUncompressed( volumeMetadata, CreateVolumeSlices( _PreviewData ) );
		}

		private static VolumeSlice CreateMinifiedSlice( ushort index, ushort minification, ushort previewSizeX, ushort previewSizeY, ushort stride, ArraySegment<byte> srcBuffer )
		{
			var position = 0;

			var length = previewSizeX * previewSizeY;
			var buffer = new byte[ length ];

			for( ushort y = 0, oy = 0; y < previewSizeY; y++, oy += minification )
			for( ushort x = 0, ox = 0; x < previewSizeX; x++, ox += minification )
				buffer[ position++ ] = srcBuffer[ oy * stride + ox ];

			var volumeSlice = new VolumeSlice( new VolumeSliceDefinition( Direction.Z, index ), buffer );

			return volumeSlice;
		}

		private void WriteSlice( IntPtr line, ushort width, ushort height, ushort z )
		{
			_Ct.ThrowIfCancellationRequested();

			if( z % _Minification != 0 )
				return;

			_Progress?.Report( new VolumeSliceDefinition( Direction.Z, z ) );

			var pz = (ushort)( z / _Minification );
			if( pz >= _PreviewSizeZ )
				return;

			var size = width * height;
			var buffer = VolumeArrayPool.Shared.Rent( size );

			Marshal.Copy( line, buffer, 0, size );

			_PreviewData.Add( Task.Run( () =>
			{
				var previewSlice = CreateMinifiedSlice( pz, _Minification, _PreviewSizeX, _PreviewSizeY, width, buffer );
				VolumeArrayPool.Shared.Return( buffer );

				return previewSlice;
			}, _Ct ) );
		}

		#endregion
	}
}