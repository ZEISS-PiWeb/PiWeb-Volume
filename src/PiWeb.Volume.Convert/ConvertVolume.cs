#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.Convert
{
	#region usings

	using System;
	using System.IO;
	using System.Threading;

	#endregion

	public static class ConvertVolume
	{
		#region methods

		/// <summary>
		/// Creates an uncompressed volume from vgi header stream and a data stream that contains uint8 or uint16 values
		/// that represent grayscale voxels. The voxels must be ordered in z, y, x direction.
		/// (slice by slice, row by row).
		/// </summary>
		/// <param name="progress">Progress</param>
		/// <param name="dataStream">Stream containing the voxel data</param>
		/// <param name="vgiStream">Stream containing the vgi data.</param>
		public static UncompressedVolume FromVgi(
			Stream vgiStream,
			Stream dataStream,
			bool extraPolate,
			byte? minValue,
			byte? maxValue,
			IProgress<double> progress )
		{
			var vgi = Vgi.Parse( vgiStream );

			dataStream.Seek( vgi.HeaderLength, SeekOrigin.Begin );

			switch( vgi.BitDepth )
			{
				case 16: return FromUint16( dataStream, vgi, extraPolate, minValue, maxValue, progress );
				case 8: return FromUint8( dataStream, vgi, progress );
				default: throw new NotSupportedException( "This converter can only convert 8 bit and 16 bit volumes." );
			}
		}

		/// <summary>
		/// Creates an uncompressed volume from a signed calypso volume stream that contains an scv header and uint8 or
		/// uint16 values that represent grayscale voxels. The voxels must be ordered in z, y, x direction.
		/// (slice by slice, row by row).
		/// </summary>
		/// <param name="scvStream">Stream containing the scv data.</param>
		/// <param name="progress">Progress</param>
		public static UncompressedVolume FromScv(
			Stream scvStream,
			bool extraPolate,
			byte? minValue,
			byte? maxValue,
			IProgress<double> progress )
		{
			var scv = Scv.Parse( scvStream );

			scvStream.Seek( scv.HeaderLength, SeekOrigin.Begin );

			switch( scv.BitDepth )
			{
				case 16: return FromUint16( scvStream, scv, extraPolate, minValue, maxValue, progress );
				case 8: return FromUint8( scvStream, scv, progress );
				default: throw new NotSupportedException( "This converter can only convert 8 bit and 16 bit volumes." );
			}
		}

		/// <summary>
		/// Creates an uncompressed volume from a stream that contains uint16 values that represent grayscale voxels.
		/// The voxels must be ordered in z, y, x direction (slice by slice, row by row).
		/// </summary>
		/// <param name="uint16Stream">Stream containing the uint16 data.</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="maxValue">Maximum value for extrapolation. </param>
		/// <param name="progress">Progress</param>
		/// <param name="extrapolate">Determines whether or not to extrapolate the value range.</param>
		/// <param name="minValue"></param>
		public static UncompressedVolume FromUint16( Stream uint16Stream, VolumeMetadata metadata, bool extrapolate, byte? minValue, byte? maxValue, IProgress<double>? progress )
		{
			if( extrapolate )
			{
				if( !minValue.HasValue || !maxValue.HasValue )
				{
					var (automaticMin, automaticMax) = GetMinMax16( uint16Stream, metadata, progress );
					minValue ??= automaticMin;
					maxValue ??= automaticMax;
				}

				return ReadExtrapolated16( uint16Stream, metadata, minValue.Value, maxValue.Value, progress );
			}
			else
			{
				return Read16( uint16Stream, metadata, progress );
			}
		}

		private static UncompressedVolume Read16( Stream uint16Stream, VolumeMetadata metadata, IProgress<double>? progress )
		{
			var sx = metadata.SizeX;
			var sy = metadata.SizeY;
			var sz = metadata.SizeZ;

			var data = new byte[sz][];

			for( var z = 0; z < sz; z++ )
				data[ z ] = new byte[sy * sx];

			var buffer = new byte[sx * sy * 2];
			for( var z = 0; z < sz; z++ )
			{
				progress?.Report( ( double ) z / sz );

				uint16Stream.Read( buffer, 0, sx * sy * 2 );
				var layer = data[ z ];

				for( var index = 0; index < sx * sy; index++ )
					layer[ index ] = buffer[ index * 2 + 1 ];
			}

			return new UncompressedVolume( metadata, data );
		}

		private static UncompressedVolume ReadExtrapolated16( Stream uint16Stream, VolumeMetadata metadata, byte minValue, byte maxValue, IProgress<double>? progress )
		{
			var sx = metadata.SizeX;
			var sy = metadata.SizeY;
			var sz = metadata.SizeZ;

			var data = new byte[sz][];

			for( var z = 0; z < sz; z++ )
				data[ z ] = new byte[sy * sx];

			if( maxValue <= minValue )
				return new UncompressedVolume( metadata, data );

			var buffer = new byte[sx * sy * 2];
			var shortbuffer = new ushort[sx * sy];
			var shortMin = minValue << 8;
			
			var factor = 256 / ( double ) ( maxValue - minValue );
			var toByteFactor = 1.0 / 256;
			
			for( var z = 0; z < sz; z++ )
			{
				progress?.Report( ( double ) z / sz );

				uint16Stream.Read( buffer, 0, sx * sy * 2 );
				Buffer.BlockCopy( buffer, 0, shortbuffer, 0, sx * sy * 2 );
				
				var layer = data[ z ];

				for( var index = 0; index < sx * sy; index++ )
					layer[ index ] = ( byte ) (Math.Max( ushort.MinValue, Math.Min( ushort.MaxValue, ( shortbuffer[ index ] - shortMin ) * factor ) ) * toByteFactor );
			}

			return new UncompressedVolume( metadata, data );
		}

		private static (byte Min, byte Max) GetMinMax16( Stream uint16Stream, VolumeMetadata metadata, IProgress<double> progress )
		{
			byte? minimum = null;
			byte maximum = 0;
			var histogram = CreateHistogram( uint16Stream, metadata, progress );

			var threshold = ( long ) metadata.SizeX * metadata.SizeY * metadata.SizeZ / 1000;

			for( var i = 0; i <= byte.MaxValue; i++ )
			{
				if( histogram[ i ] < threshold )
					continue;

				if( !minimum.HasValue )
					minimum = ( byte ) i;

				maximum = ( byte ) i;
			}

			return ( minimum ?? 0, maximum );
		}

		private static long[] CreateHistogram( Stream uint16Stream, VolumeMetadata metadata, IProgress<double> progress )
		{
			var result = new long[256];

			var sx = metadata.SizeX;
			var sy = metadata.SizeY;
			var sz = metadata.SizeZ;

			var buffer = new byte[sx * sy * 2];

			var position = uint16Stream.Position;

			for( var z = 0; z < sz; z++ )
			{
				progress?.Report( ( double ) z / sz );

				uint16Stream.Read( buffer, 0, sx * sy * 2 );

				for( var index = 0; index < sx * sy; index++ )
				{
					var value = buffer[ index * 2 + 1 ];
					Interlocked.Increment( ref result[ value ] );
				}
			}

			uint16Stream.Seek( position, SeekOrigin.Begin );

			return result;
		}

		/// <summary>
		/// Creates an uncompressed volume from a stream that contains uint8 values that represent grayscale voxels.
		/// The voxels must be ordered in z, y, x direction (slice by slice, row by row).
		/// </summary>
		/// <param name="uint8Stream">Stream containing the uint8 data.</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="progress">Progress</param>
		public static UncompressedVolume FromUint8( Stream uint8Stream, VolumeMetadata metadata, IProgress<double>? progress )
		{
			var sx = metadata.SizeX;
			var sy = metadata.SizeY;
			var sz = metadata.SizeZ;

			var data = new byte[sz][];

			for( var z = 0; z < sz; z++ )
				data[ z ] = new byte[sy * sx];

			for( var z = 0; z < sz; z++ )
			{
				progress?.Report( ( double ) z / sz );

				uint8Stream.Read( data[ z ], 0, sx * sy );
			}

			return new UncompressedVolume( metadata, data );
		}

		#endregion
	}
}