#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
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
		private readonly byte[][] _Result;

		#endregion

		#region constructors

		internal BlockVolumeDecompressor( BlockVolume volume )
		{
			_Volume = volume;

			_Metadata = _Volume.Metadata;

			_SizeZ = _Metadata.SizeZ;
			_SizeY = _Metadata.SizeY;
			_SizeX = _Metadata.SizeX;

			_Result = VolumeSliceHelper.CreateSliceBuffer( _SizeX, _SizeY, _SizeZ );
		}

		#endregion

		#region methods

		internal VolumeSlice[] Decompress( IProgress<VolumeSliceDefinition>? progress = null, CancellationToken ct = default )
		{
			if( _Volume.CompressedData[ Direction.Z ] is not { } data )
				throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

			BlockVolumeDecoder.Decode( data, _Metadata, CopyBlockToResult, null, null, progress, ct );

			return _Result
				.AsParallel()
				.AsOrdered()
				.Select( ( s, i ) => new VolumeSlice( new VolumeSliceDefinition( Direction.Z, (ushort)i ), s ) )
				.ToArray();
		}

		private void CopyBlockToResult( ReadOnlySpan<byte> block, BlockIndex index )
		{
			var gx = index.X * BlockVolume.N;
			var sx = Math.Min( _SizeX - index.X * BlockVolume.N, BlockVolume.N );
			var sy = Math.Min( _SizeY - index.Y * BlockVolume.N, BlockVolume.N );
			var sz = Math.Min( _SizeZ - index.Z * BlockVolume.N, BlockVolume.N );

			for( var bz = 0; bz < sz; bz++ )
			{
				var gz = index.Z * BlockVolume.N + bz;
				var sliceData = _Result[ gz ].AsSpan();

				for( var by = 0; by < sy; by++ )
				{
					var gy = index.Y * BlockVolume.N + by;
					block.Slice( bz * BlockVolume.N2 + by * BlockVolume.N, sx ).CopyTo( sliceData.Slice( gy * _SizeX + gx, sx ) );
				}
			}
		}

		#endregion
	}
}