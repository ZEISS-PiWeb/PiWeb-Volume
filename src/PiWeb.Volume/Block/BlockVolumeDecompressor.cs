#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Block
{
	#region usings

	using System;
	using System.IO;
	using System.Linq;
	using System.Threading;

	#endregion

	/// <summary>
	/// Decompresses the block volume completely
	/// </summary>
	internal class BlockVolumeDecompressor
	{
		#region members

		private readonly BlockVolume _Volume;
		private readonly VolumeMetadata _Metadata;
		private readonly ushort _SizeZ;
		private readonly ushort _SizeY;
		private readonly ushort _SizeX;

		#endregion

		#region constructors

		internal BlockVolumeDecompressor( BlockVolume volume )
		{
			_Volume = volume;

			_Metadata = _Volume.Metadata;

			_SizeZ = _Metadata.SizeZ;
			_SizeY = _Metadata.SizeY;
			_SizeX = _Metadata.SizeX;
		}

		#endregion

		#region methods

		internal VolumeSlice[] Decompress( IProgress<VolumeSliceDefinition>? progress = null, CancellationToken ct = default )
		{
			if( _Volume.CompressedData[ Direction.Z ] is not {} data )
				throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

			var decoder = new BlockVolumeDecoder();
			var input = new MemoryStream( data );
			var result = VolumeSliceHelper.CreateSliceBuffer( _SizeX, _SizeY, _SizeZ );

			decoder.Decode( input, _Metadata, ( block, index ) =>
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

					result[ gz ][ gy * _SizeX + gx ] = block[ bz * BlockVolume.N2 + by * BlockVolume.N + bx ];
				}
			}, null, null, progress, ct );

			return result
				.AsParallel()
				.AsOrdered()
				.Select( ( s, i ) => new VolumeSlice( new VolumeSliceDefinition( Direction.Z, (ushort)i ), s ) )
				.ToArray();
		}

		#endregion
	}
}