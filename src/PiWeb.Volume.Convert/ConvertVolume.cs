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
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;

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
			IProgress<double> progress,
			ILogger logger = null )
		{
			var sw = Stopwatch.StartNew();
			try
			{
				var vgi = Vgi.Parse( vgiStream );

				dataStream.Seek( vgi.HeaderLength, SeekOrigin.Begin );

				switch( vgi.BitDepth )
				{
					case 16:
						var uint16Stream = extraPolate
							? new Uint16Stream( dataStream, minValue, maxValue )
							: new Uint16Stream( dataStream );
						return streamed ? new StreamedVolume( vgi, uint16Stream ) : FromUint8( uint16Stream, vgi, progress, logger );
					
					case 8:
						var uint8Stream = extraPolate
							? new Uint8Stream( dataStream, minValue, maxValue )
							: new Uint8Stream( dataStream );
						return streamed ? new StreamedVolume( vgi, uint8Stream ) : FromUint8( uint8Stream, vgi, progress, logger );
					
					default: throw new NotSupportedException( "This converter can only convert 8 bit and 16 bit volumes." );
				}
			}
			finally
			{
				logger?.Log( LogLevel.Info, $"Loaded VGI volume data in {sw.ElapsedMilliseconds} ms." );

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
			IProgress<double> progress,
			ILogger logger = null )
		{
			var sw = Stopwatch.StartNew();
			try
			{
				var scv = Scv.Parse( scvStream );

				scvStream.Seek( scv.HeaderLength, SeekOrigin.Begin );

				switch( scv.BitDepth )
				{
					case 16:
						var uint16Stream = extraPolate
							? new Uint16Stream( scvStream, minValue, maxValue )
							: new Uint16Stream( scvStream );
						return streamed ? new StreamedVolume( scv, uint16Stream ) : FromUint8( uint16Stream, scv, progress, logger );
					case 8:
						var uint8Stream = extraPolate
							? new Uint8Stream( scvStream, minValue, maxValue )
							: new Uint8Stream( scvStream );

						return streamed ? new StreamedVolume( scv, uint8Stream ) : FromUint8( uint8Stream, scv, progress, logger );

					default: throw new NotSupportedException( "This converter can only convert 8 bit and 16 bit volumes." );
				}
			}
			finally
			{
				logger?.Log( LogLevel.Info, $"Loaded SCV volume data in {sw.ElapsedMilliseconds} ms." );
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
		public static Volume FromUint16( Stream uint16Stream, VolumeMetadata metadata, bool streamed, IProgress<double> progress, ILogger logger = null )
		{
			var sw = Stopwatch.StartNew();
			try
			{
				var sx = metadata.SizeX;
				var sy = metadata.SizeY;
				var sz = metadata.SizeZ;

				var data = VolumeSliceHelper.CreateSliceBuffer( sx, sy, sz );
				
				var buffer = new byte[sx * sy * 2];
				for( var z = 0; z < sz; z++ )
				{
					progress?.Report( ( double ) z / sz );

					uint16Stream.Read( buffer, 0, sx * sy * 2 );
					var layer = data[ z ].Data;

					for( var index = 0; index < sx * sy; index++ )
						layer[ index ] = buffer[ index * 2 + 1 ];
				}

				var slices = data
					.AsParallel()
					.AsOrdered()
					.Select( s => s.ToVolumeSlice() )
					.ToArray();
				
				return new UncompressedVolume( metadata, slices );
			}
			finally
			{
				logger?.Log( LogLevel.Info, $"Loaded UINT16 volume data in {sw.ElapsedMilliseconds} ms." );
			}
		}

		/// <summary>
		/// Creates an uncompressed volume from a stream that contains uint8 values that represent grayscale voxels.
		/// The voxels must be ordered in z, y, x direction (slice by slice, row by row).
		/// </summary>
		public static Volume FromUint8( Stream uint8Stream, VolumeMetadata metadata, IProgress<double> progress, ILogger logger = null )
		{
			var sw = Stopwatch.StartNew();
			try
			{
				var sx = metadata.SizeX;
				var sy = metadata.SizeY;
				var sz = metadata.SizeZ;

				var sliceSize = sx * sy;
				var slices = new List<Task<VolumeSlice>>( sz );

				for( ushort z = 0; z < sz; z++ )
				{
					var definition = new VolumeSliceDefinition( Direction.Z, z );
					progress?.Report( ( double ) z / sz );
					
					var buffer = new VolumeSliceBuffer( definition, sliceSize );
					uint8Stream.Read( buffer.Data, 0, sliceSize );

					slices.Add( Task.Run( () => buffer.ToVolumeSlice() ) );
				}

				Task.WaitAll( slices.ToArray() );
				var allSlices = slices
					.Select( s => s.Result )
					.ToArray();
				
				return new UncompressedVolume( metadata, allSlices );
			}
			finally
			{
				logger?.Log( LogLevel.Info, $"Loaded UINT8 volume data in {sw.ElapsedMilliseconds} ms." );
			}
		}

		#endregion
	}
}