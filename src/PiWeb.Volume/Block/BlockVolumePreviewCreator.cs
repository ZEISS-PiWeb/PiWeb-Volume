#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.Block
{
	#region usings

	using System;
	using System.IO;
	using System.Threading;

	#endregion

	internal class BlockVolumePreviewCreator
	{
		#region members

		private readonly BlockVolume _Volume;
		private readonly ushort _Minification;
		private readonly ushort _PreviewSizeX;
		private readonly ushort _PreviewSizeY;
		private readonly ushort _PreviewSizeZ;
		private readonly ushort _SizeZ;
		private readonly ushort _SizeY;
		private readonly ushort _SizeX;
		private VolumeMetadata _Metadata;

		#endregion

		#region constructors

		internal BlockVolumePreviewCreator( BlockVolume volume, ushort minification )
		{
			_Volume = volume;
			_Minification = minification;
			_Metadata = _Volume.Metadata;

			_SizeZ = _Metadata.SizeZ;
			_SizeY = _Metadata.SizeY;
			_SizeX = _Metadata.SizeX;

			_PreviewSizeX = ( ushort ) ( ( _SizeX - 1 ) / _Minification + 1 );
			_PreviewSizeY = ( ushort ) ( ( _SizeY - 1 ) / _Minification + 1 );
			_PreviewSizeZ = ( ushort ) ( ( _SizeZ - 1 ) / _Minification + 1 );

			if( _PreviewSizeX < 2 || _PreviewSizeY < 2 || _PreviewSizeZ < 2 )
				throw new ArgumentOutOfRangeException( nameof(minification) );
		}

		#endregion

		#region methods

		internal UncompressedVolume CreatePreview( IProgress<VolumeSliceDefinition> progress, CancellationToken ct )
		{
			if( _Volume.CompressedData[ Direction.Z ] == null )
				throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

			var decoder = new BlockVolumeDecoder( _Volume.CompressionOptions );
			var data = _Volume.CompressedData[ Direction.Z ];
			var input = new MemoryStream( data );
			
			var result = new byte[_PreviewSizeZ][];
			long sliceSize = _PreviewSizeX * _PreviewSizeY;

			for( var z = 0; z < _PreviewSizeZ; z++ )
				result[ z ] = new byte[sliceSize];

			decoder.Decode( input, _Metadata, (block, index) =>
			{
				for( var bz = 0; bz < BlockVolume.N; bz++ )
				for( var by = 0; by < BlockVolume.N; by++ )
				for( var bx = 0; bx < BlockVolume.N; bx++ )
				{
					var gz = index.Z * BlockVolume.N + bz;
					var gy = index.Y * BlockVolume.N + by;
					var gx = index.X * BlockVolume.N + bx;

					if( gz >= _SizeZ || gy >= _SizeY || gx >= _SizeX )
						continue;

					var px = gx / _Minification;
					var py = gy / _Minification;
					var pz = gz / _Minification;

					if( px * _Minification != gx || py * _Minification != gy || pz * _Minification != gz )
						continue;


					result[ pz ][ py * _PreviewSizeX + px ] = block[ bz * BlockVolume.N2 + by * BlockVolume.N + bx ];
				}
			}, null, null, progress, ct );

			return new UncompressedVolume( new VolumeMetadata( 
				_PreviewSizeX, 
				_PreviewSizeY, 
				_PreviewSizeZ, 
				_Metadata.ResolutionX * _Minification, 
				_Metadata.ResolutionY * _Minification, 
				_Metadata.ResolutionZ * _Minification ), 
				result );
		}

		#endregion
	}
}