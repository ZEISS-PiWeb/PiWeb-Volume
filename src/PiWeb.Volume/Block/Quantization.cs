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

	#endregion

	/// <summary>
	/// Helper class to calculate the quantization.
	/// </summary>
	internal static class Quantization
	{
		#region methods

		/// <summary>
		/// Calculates a generic quantization matrix based on the quality setting.
		/// </summary>
		public static double[] Calculate( VolumeCompressionOptions options, bool invert )
		{
			var scale = 100;

			if( options.EncoderOptions.TryGetValue( "quality", out var qualityString ) && int.TryParse( qualityString, out var quality ) )
				scale = QualityScaling( quality );

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