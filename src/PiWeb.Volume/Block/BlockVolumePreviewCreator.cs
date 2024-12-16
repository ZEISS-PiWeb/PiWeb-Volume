#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Block;

#region usings

using System;
using System.Linq;
using System.Threading;

#endregion

/// <summary>
/// Creates a minified preview of a block volume.
/// </summary>
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
	private readonly VolumeMetadata _Metadata;

	#endregion

	#region constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="BlockVolumePreviewCreator"/> class.
	/// </summary>
	internal BlockVolumePreviewCreator( BlockVolume volume, ushort minification )
	{
		_Volume = volume;
		_Minification = minification;
		_Metadata = _Volume.Metadata;

		_SizeZ = _Metadata.SizeZ;
		_SizeY = _Metadata.SizeY;
		_SizeX = _Metadata.SizeX;

		_PreviewSizeX = (ushort)( ( _SizeX - 1 ) / _Minification + 1 );
		_PreviewSizeY = (ushort)( ( _SizeY - 1 ) / _Minification + 1 );
		_PreviewSizeZ = (ushort)( ( _SizeZ - 1 ) / _Minification + 1 );

		if( _PreviewSizeX < 2 || _PreviewSizeY < 2 || _PreviewSizeZ < 2 )
			throw new ArgumentOutOfRangeException( nameof( minification ) );
	}

	#endregion

	#region methods

	internal UncompressedVolume CreatePreview( IProgress<VolumeSliceDefinition>? progress, CancellationToken ct )
	{
		var result = VolumeSliceHelper.CreateSliceBuffer( _PreviewSizeX, _PreviewSizeY, _PreviewSizeZ );

		BlockVolumeDecoder.Decode( _Volume, Direction.Z, ( block, index ) =>
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

		var volumeMetadata = new VolumeMetadata(
			_PreviewSizeX,
			_PreviewSizeY,
			_PreviewSizeZ,
			_Metadata.ResolutionX * _Minification,
			_Metadata.ResolutionY * _Minification,
			_Metadata.ResolutionZ * _Minification );

		var slices = result
			.AsParallel()
			.AsOrdered()
			.Select( ( s, i ) => new VolumeSlice( new VolumeSliceDefinition( Direction.Z, (ushort)i ), s ) )
			.ToArray();

		return new UncompressedVolume( volumeMetadata, slices );
	}

	#endregion
}