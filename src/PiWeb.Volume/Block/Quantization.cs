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

	#endregion

	/// <summary>
	/// Helper class to calculate the quantization.
	/// </summary>
	internal static class Quantization
	{
		#region members

		private static readonly int[] BaseValues =
		{
			16, 11, 10, 16, 24, 40, 51, 61,
			12, 12, 14, 19, 26, 58, 60, 55,
			14, 13, 16, 24, 40, 57, 69, 56,
			14, 17, 22, 29, 51, 87, 80, 62,
			18, 22, 37, 56, 68, 109, 103, 77,
			24, 35, 55, 64, 81, 104, 113, 92,
			49, 64, 78, 87, 103, 121, 120, 101,
			72, 92, 95, 98, 112, 100, 103, 99
		};

		#endregion

		#region methods

		/// <summary>
		/// Origin of base values: libjpeg
		/// </summary>
		internal static double[] Calculate( VolumeCompressionOptions options, bool invert )
		{
			var scale = 100;

			if( options.EncoderOptions.TryGetValue( "quality", out var qualityString ) && int.TryParse( qualityString, out var quality ) )
				scale = QualityScaling( quality );

			var values = new double[BlockVolume.N3];

			var i = 0;
			for( var z = 0; z < BlockVolume.N; z++ )
			for( var y = 0; y < BlockVolume.N; y++ )
			for( var x = 0; x < BlockVolume.N; x++ )
			{
				var a = x;
				var b = y;
				var c = z;

				if( a > c )
					Swap( ref a, ref c );

				if( a > b )
					Swap( ref a, ref b );

				if( b > c )
					Swap( ref b, ref c );

				var value = Math.Max( 1, Math.Min( short.MaxValue, ( BaseValues[ c + BlockVolume.N * b ] * 2.0 * scale + 50 ) / 100 ) );
				values[ i++ ] = invert ? value : 1.0 / value;
			}

			return values;
		}

		/// <summary>
		/// Origin: libjpeg
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

		private static void Swap( ref int a, ref int b )
		{
			var temp = a;
			a = b;
			b = temp;
		}

		#endregion
	}
}