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
	using System.Runtime.InteropServices;

	#endregion

	/// <summary>
	/// Helper class to calculate the quantization.
	/// </summary>
	internal static class Quantization
	{
		#region methods

		/// <summary>
		/// Reads a quantization matrix from the specified <paramref name="reader"/>
		/// </summary>
		public static double[] Read( BinaryReader reader, bool invert )
		{
			var data = reader.ReadBytes( BlockVolume.N3 * sizeof( double ) ).AsSpan();
			var values = MemoryMarshal.Cast<byte, double>( data );

			if( !invert )
				return values.ToArray();

			for( var i = 0; i < BlockVolume.N3; i++ )
				values[ i ] = 1.0 / values[ i ];

			return values.ToArray();
		}

		/// <summary>
		/// Writes a quantization matrix to the specified <paramref name="writer"/>
		/// </summary>
		public static void Write( BinaryWriter writer, double[] values )
		{
			var data = MemoryMarshal.Cast<double, byte>( values );
			writer.Write( data );
		}

		/// <summary>
		/// Calculates a generic quantization matrix based on the quality setting.
		/// </summary>
		public static double[] Calculate( VolumeCompressionOptions options, bool invert = false )
		{
			var quality = 75;

			if( options.EncoderOptions.TryGetValue( "quality", out var qualityString ) && int.TryParse( qualityString, out var parsedQuality ) )
				quality = parsedQuality;

			return Calculate( quality, invert );
		}

		internal static double[] Calculate( int quality = 75, bool invert = false )
		{
			var scale = QualityScaling( quality );
			var values = new double[ BlockVolume.N3 ];

			var i = 0;

			for( var z = 0; z < BlockVolume.N; z++ )
			for( var y = 0; y < BlockVolume.N; y++ )
			for( var x = 0; x < BlockVolume.N; x++ )
			{
				var distance = Math.Max( 0, Math.Max( x, Math.Max( y, z ) ) - 1 );
				var baseValue = 12 * ( 1 + distance );

				var value = Math.Max( 1, Math.Min( short.MaxValue, ( baseValue * 2.0 * scale + 50 ) / 100 ) );
				values[ i++ ] = invert ? value : 1.0 / value;
			}

			return values;
		}

		/// <summary>
		/// Quality scaling as in libjpeg.
		/// </summary>
		private static int QualityScaling( int quality )
		{
			if( quality <= 0 ) quality = 1;
			if( quality > 100 ) quality = 100;

			if( quality < 50 )
				quality = 5000 / quality;
			else
				quality = 200 - quality * 2;

			return quality;
		}

		#endregion
	}
}