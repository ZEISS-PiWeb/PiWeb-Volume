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
	using System.Buffers;
	using System.Collections.Generic;
	using System.IO;
	using System.Runtime.InteropServices;
	using System.Threading;
	using Zeiss.IMT.PiWeb.Volume.Interop;

	#endregion

	internal class PreviewCreator
	{
		#region members

		private readonly VolumeMetadata _Metadata;
		private readonly ushort _Minification;
		private readonly IProgress<VolumeSliceDefinition> _Progress;
		private readonly CancellationToken _Ct;

		private readonly VolumeSlice[] _PreviewData;

		private readonly ushort _PreviewSizeX;
		private readonly ushort _PreviewSizeY;
		private readonly ushort _PreviewSizeZ;

		#endregion

		#region constructors

		internal PreviewCreator( VolumeMetadata metadata, ushort minification, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			_Metadata = metadata ?? throw new ArgumentNullException( nameof(metadata) );
			_Minification = minification;
			_Progress = progress;
			_Ct = ct;

			_PreviewSizeX = ( ushort ) ( ( metadata.SizeX - 1 ) / minification + 1 );
			_PreviewSizeY = ( ushort ) ( ( metadata.SizeY - 1 ) / minification + 1 );
			_PreviewSizeZ = ( ushort ) ( ( metadata.SizeZ - 1 ) / minification + 1 );

			if( _PreviewSizeX < 2 || _PreviewSizeY < 2 || _PreviewSizeZ < 2 )
				throw new ArgumentOutOfRangeException( nameof(minification) );

			_PreviewData = VolumeSliceHelper.CreateSliceData( _PreviewSizeX, _PreviewSizeY, _PreviewSizeZ );

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
			var sizeY = metadata.SizeY;
			var sizeZ = metadata.SizeZ;
			var previewSizeX = ( ushort ) ( ( metadata.SizeX - 1 ) / minification + 1 );
			var previewSizeY = ( ushort ) ( ( metadata.SizeY - 1 ) / minification + 1 );
			var previewSizeZ = ( ushort ) ( ( metadata.SizeZ - 1 ) / minification + 1 );

			if( previewSizeX < 2 || previewSizeY < 2 || previewSizeZ < 2 )
				throw new ArgumentOutOfRangeException( nameof(minification) );

			var result = VolumeSliceHelper.CreateSliceData( previewSizeX, previewSizeY, previewSizeZ );

			for( ushort z = 0, oz = 0; z < previewSizeZ && oz < sizeZ; z++, oz += minification )
			{
				var position = 0;

				for( ushort y = 0, oy = 0; y < previewSizeY && oy < sizeY; y++, oy += minification )
				for( ushort x = 0, ox = 0; x < previewSizeX && ox < sizeX; x++, ox += minification )
					result[ z ].Data[ position++ ] = slices[ oz ].Data[ oy * sizeX + ox ];
			}

			var volumeMetadata = new VolumeMetadata( 
				previewSizeX, 
				previewSizeY, 
				previewSizeZ, 
				metadata.ResolutionX * minification, 
				metadata.ResolutionY * minification, 
				metadata.ResolutionZ * minification );
			
			return Volume.CreateUncompressed( volumeMetadata, result );
		}

		internal static UncompressedVolume CreatePreview( Stream stream, VolumeMetadata metadata, ushort minification, IProgress<VolumeSliceDefinition> progress )
		{
			var sizeX = metadata.SizeX;
			var sizeY = metadata.SizeY;
			var sizeZ = metadata.SizeZ;
			var previewSizeX = ( ushort ) ( ( metadata.SizeX - 1 ) / minification + 1 );
			var previewSizeY = ( ushort ) ( ( metadata.SizeY - 1 ) / minification + 1 );
			var previewSizeZ = ( ushort ) ( ( metadata.SizeZ - 1 ) / minification + 1 );

			if( previewSizeX < 2 || previewSizeY < 2 || previewSizeZ < 2 )
				throw new ArgumentOutOfRangeException( nameof(minification) );

			var sliceBufferSize = sizeX * sizeY;
			var sliceBuffer = ArrayPool<byte>.Shared.Rent( sliceBufferSize );

			var result = VolumeSliceHelper.CreateSliceData( previewSizeX, previewSizeY, previewSizeZ );
			for( ushort oz = 0, z = 0; oz < sizeZ && z < previewSizeZ; oz++ )
			{
				progress?.Report( new VolumeSliceDefinition( Direction.Z, oz ) );

				if( oz % minification == 0 )
				{
					stream.Read( sliceBuffer, 0, sliceBufferSize );
					var position = 0;

					for( ushort y = 0, oy = 0; y < previewSizeY && oy < sizeY; y++, oy += minification )
					for( ushort x = 0, ox = 0; x < previewSizeX && ox < sizeX; x++, ox += minification )
						result[ z ].Data[ position++ ] = sliceBuffer[ oy * sizeX + ox ];

					z++;
				}
				else
				{
					stream.Seek( sliceBufferSize, SeekOrigin.Current );
				}
			}
			ArrayPool<byte>.Shared.Return( sliceBuffer );

			var volumeMetadata = new VolumeMetadata( 
				previewSizeX, 
				previewSizeY, 
				previewSizeZ, 
				metadata.ResolutionX * minification, 
				metadata.ResolutionY * minification,
				metadata.ResolutionZ * minification );
			
			return Volume.CreateUncompressed( volumeMetadata, result );
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
			
			return Volume.CreateUncompressed( volumeMetadata, _PreviewData );
		}

		private void WriteSlice( IntPtr line, ushort width, ushort height, ushort z )
		{
			_Ct.ThrowIfCancellationRequested();

			if( z % _Minification != 0 )
				return;

			_Progress?.Report( new VolumeSliceDefinition( Direction.Z, z ) );

			var pz = z / _Minification;
			if( pz >= _PreviewSizeZ )
				return;

			for( int oy = 0, py = 0; py < _PreviewSizeY && oy < height; py++, oy += _Minification )
			for( int ox = 0, px = 0; px < _PreviewSizeX && ox < width; px++, ox += _Minification )
			{
				_PreviewData[ pz ].Data[ py * _PreviewSizeX + px ] = Marshal.ReadByte( line, oy * width + ox );
			}
		}

		#endregion
	}
}