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

		private readonly byte[][] _PreviewData;

		private readonly ushort _PreviewSizeX;
		private readonly ushort _PreviewSizeY;
		private readonly ushort _PreviewSizeZ;

		#endregion

		#region constructors

		internal PreviewCreator( VolumeMetadata metadata, ushort minification, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default( CancellationToken ) )
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

			_PreviewData = new byte[_PreviewSizeZ][];
			long sliceSize = _PreviewSizeX * _PreviewSizeY;

			for( var z = 0; z < _PreviewSizeZ; z++ ) _PreviewData[ z ] = new byte[sliceSize];

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

		internal static UncompressedVolume CreatePreview( byte[][] data, VolumeMetadata metadata, ushort minification )
		{
			var sizeX = metadata.SizeX;
			var sizeY = metadata.SizeY;
			var sizeZ = metadata.SizeZ;
			var previewSizeX = ( ushort ) ( ( metadata.SizeX - 1 ) / minification + 1 );
			var previewSizeY = ( ushort ) ( ( metadata.SizeY - 1 ) / minification + 1 );
			var previewSizeZ = ( ushort ) ( ( metadata.SizeZ - 1 ) / minification + 1 );

			if( previewSizeX < 2 || previewSizeY < 2 || previewSizeZ < 2 )
				throw new ArgumentOutOfRangeException( nameof(minification) );

			var result = new byte[previewSizeZ][];
			long sliceSize = previewSizeX * previewSizeY;

			for( var z = 0; z < previewSizeZ; z++ ) result[ z ] = new byte[sliceSize];

			for( ushort z = 0, oz = 0; z < previewSizeZ && oz < sizeZ; z++, oz += minification )
			{
				var position = 0;

				for( ushort y = 0, oy = 0; y < previewSizeY && oy < sizeY; y++, oy += minification )
				for( ushort x = 0, ox = 0; x < previewSizeX && ox < sizeX; x++, ox += minification )
					result[ z ][ position++ ] = data[ oz ][ oy * sizeX + ox ];
			}

			return Volume.CreateUncompressed( new VolumeMetadata( previewSizeX, previewSizeY, previewSizeZ, metadata.ResolutionX * minification, metadata.ResolutionY * minification, metadata.ResolutionZ * minification ), result );
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

			var sliceBuffer = new byte[sizeX * sizeY];
			var result = new byte[previewSizeZ][];
			long sliceSize = previewSizeX * previewSizeY;

			for( var z = 0; z < previewSizeZ; z++ )
				result[ z ] = new byte[sliceSize];

			for( ushort oz = 0, z = 0; oz < sizeZ && z < previewSizeZ; oz++ )
			{
				progress?.Report( new VolumeSliceDefinition( Direction.Z, oz ) );

				if( oz % minification == 0 )
				{
					stream.Read( sliceBuffer, 0, sizeX * sizeY );
					var position = 0;

					for( ushort y = 0, oy = 0; y < previewSizeY && oy < sizeY; y++, oy += minification )
					for( ushort x = 0, ox = 0; x < previewSizeX && ox < sizeX; x++, ox += minification )
						result[ z ][ position++ ] = sliceBuffer[ oy * sizeX + ox ];

					z++;
				}
				else
				{
					stream.Seek( sizeX * sizeY, SeekOrigin.Current );
				}
			}

			return Volume.CreateUncompressed( new VolumeMetadata( previewSizeX, previewSizeY, previewSizeZ, metadata.ResolutionX * minification, metadata.ResolutionY * minification, metadata.ResolutionZ * minification ), result );
		}

		internal UncompressedVolume GetPreview()
		{
			return Volume.CreateUncompressed( new VolumeMetadata( _PreviewSizeX, _PreviewSizeY, _PreviewSizeZ, _Metadata.ResolutionX * _Minification, _Metadata.ResolutionY * _Minification, _Metadata.ResolutionZ * _Minification ), _PreviewData );
		}


		internal void WriteSlice( IntPtr line, ushort width, ushort height, ushort z )
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
				_PreviewData[ pz ][ py * _PreviewSizeX + px ] =
					Marshal.ReadByte( line, oy * width + ox );
		}

		#endregion
	}
}