﻿#region copyright

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
	
	#endregion

	public static class ConvertVolume
	{
		#region methods

		/// <summary>
		/// Creates an uncompressed volume from vgi header stream and a data stream that contains uint8 or uint16 values
		/// that represent grayscale voxels. The voxels must be ordered in z, y, x direction.
		/// (slice by slice, row by row).
		/// </summary>
		/// <param name="streamed"></param>
		/// <param name="progress">Progress</param>
		/// <param name="dataStream">Stream containing the voxel data</param>
		/// <param name="vgiStream">Stream containing the vgi data.</param>
		/// <param name="extraPolate"></param>
		/// <param name="minValue"></param>
		/// <param name="maxValue"></param>
		public static Volume FromVgi(
			Stream vgiStream,
			Stream dataStream,
			bool extraPolate,
			byte minValue,
			byte maxValue,
			bool streamed,
			IProgress<double> progress )
		{
			var vgi = Vgi.Parse( vgiStream );

			dataStream.Seek( vgi.HeaderLength, SeekOrigin.Begin );

			switch( vgi.BitDepth )
			{
				case 16: 
					var uint16Stream = extraPolate 
						? new Uint16Stream( dataStream, minValue, maxValue ) 
						: new Uint16Stream( dataStream );
					return FromUint8( uint16Stream, vgi, streamed, progress );
				case 8: 
					var uint8Stream = extraPolate 
						? new Uint8Stream( dataStream, minValue, maxValue ) 
						: new Uint8Stream( dataStream );
					return FromUint8( uint8Stream, vgi, streamed, progress );
				default: throw new NotSupportedException( "This converter can only convert 8 bit and 16 bit volumes." );
			}
		}

		/// <summary>
		/// Creates an uncompressed volume from a signed calypso volume stream that contains an scv header and uint8 or
		/// uint16 values that represent grayscale voxels. The voxels must be ordered in z, y, x direction.
		/// (slice by slice, row by row).
		/// </summary>
		/// <param name="scvStream">Stream containing the scv data.</param>
		/// <param name="streamed"></param>
		/// <param name="progress">Progress</param>
		/// <param name="extraPolate"></param>
		/// <param name="minValue"></param>
		/// <param name="maxValue"></param>
		public static Volume FromScv(
			Stream scvStream,
			bool extraPolate,
			byte minValue,
			byte maxValue,
			bool streamed,
			IProgress<double> progress )
		{
			var scv = Scv.Parse( scvStream );

			scvStream.Seek( scv.HeaderLength, SeekOrigin.Begin );

			switch( scv.BitDepth )
			{
				case 16: 
					var uint16Stream = extraPolate 
						? new Uint16Stream( scvStream, minValue, maxValue ) 
						: new Uint16Stream( scvStream );
					return FromUint8( uint16Stream, scv, streamed, progress );
				case 8: 
					var uint8Stream = extraPolate 
						? new Uint8Stream( scvStream, minValue, maxValue ) 
						: new Uint8Stream( scvStream );
					return FromUint8( uint8Stream, scv, streamed, progress );
				default: throw new NotSupportedException( "This converter can only convert 8 bit and 16 bit volumes." );
			}
		}

		/// <summary>
		/// Creates an uncompressed volume from a stream that contains uint16 values that represent grayscale voxels.
		/// The voxels must be ordered in z, y, x direction (slice by slice, row by row).
		/// </summary>
		/// <param name="uint16Stream">Stream containing the uint16 data.</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="streamed"></param>
		/// <param name="progress">Progress</param>
		public static Volume FromUint16( Stream uint16Stream, VolumeMetadata metadata, bool streamed, IProgress<double> progress )
		{
			if (streamed)
				return new StreamedVolume( metadata, uint16Stream );
			
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

		/// <summary>
		/// Creates an uncompressed volume from a stream that contains uint8 values that represent grayscale voxels.
		/// The voxels must be ordered in z, y, x direction (slice by slice, row by row).
		/// </summary>
		/// <param name="uint8Stream">Stream containing the uint8 data.</param>
		/// <param name="metadata">Metadata</param>
		/// <param name="streamed"></param>
		/// <param name="progress">Progress</param>
		public static Volume FromUint8( Stream uint8Stream, VolumeMetadata metadata, bool streamed, IProgress<double> progress )
		{
			if (streamed)
				return new StreamedVolume( metadata, uint8Stream );
			
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