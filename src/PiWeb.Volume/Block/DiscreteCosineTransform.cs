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
	/// Helper class to perform the discrete cosine transformation.
	/// </summary>
	internal static class DiscreteCosineTransform
	{
		#region members

		public static readonly double[] U = CalculateCoefficients( false );
		public static readonly double[] Ut = CalculateCoefficients( true );

		#endregion

		#region methods

		//https://dev.to/marycheung021213/understanding-dct-and-quantization-in-jpeg-compression-1col
		private static double[] CalculateCoefficients( bool invert )
		{
			var result = new double[ BlockVolume.N2 ];

			for( var u = 0; u < BlockVolume.N; u++ )
			{
				for( var v = 0; v < BlockVolume.N; v++ )
				{
					var c = u == 0
						? Math.Sqrt( 1.0 / BlockVolume.N )
						: Math.Sqrt( 2.0 / BlockVolume.N ) * Math.Cos( ( ( v * 2 + 1 ) * u * Math.PI ) / ( BlockVolume.N * 2 ) );
					result[ invert ? u + BlockVolume.N * v : u * BlockVolume.N + v ] = c;
				}
			}

			return result;
		}

		internal static unsafe void Transform( double[] values, double[] result, bool inverse = false )
		{
			fixed( double* pU = inverse ? Ut : U )
			fixed( double* pValues = values )
			fixed( double* pResult = result )
			{
				int u, p, x, y, z;

				//iterieren über x (n 0..N): values[ n, y, z ] * u[ n, x ] ;
				for( z = 0, p = 0; z < BlockVolume.N; z++ )
				for( y = 0; y < BlockVolume.N; y++, p += BlockVolume.N )
				for( x = 0, u = 0; x < BlockVolume.N; x++, u += BlockVolume.N )
				{
					//u = x * N;
					//p = z * NN + y * N;
					var r = 0.0;
					for( var n = 0; n < BlockVolume.N; n++ )
						r += pValues[ p + n ] * pU[ u + n ];

					pResult[ z * BlockVolume.N2 + x * BlockVolume.N + y ] = r;
				}

				//iterieren über y (n 0..N): values[ x, n, z ] * u[ n, y ]
				for( z = 0, p = 0; z < BlockVolume.N; z++ )
				for( x = 0; x < BlockVolume.N; x++, p += BlockVolume.N )
				for( y = 0, u = 0; y < BlockVolume.N; y++, u += BlockVolume.N )
				{
					//u = y * N;
					//p = z * NN + x * N;
					var r = 0.0;
					for( var n = 0; n < BlockVolume.N; n++ )
						r += pResult[ p + n ] * pU[ u + n ];
					pValues[ y * BlockVolume.N2 + x * BlockVolume.N + z ] = r;
				}

				//iterieren über z (n 0..N): values[ x, y, n ] * u[ n, z ]
				for( y = 0, p = 0; y < BlockVolume.N; y++ )
				for( x = 0; x < BlockVolume.N; x++, p += BlockVolume.N )
				for( z = 0, u = 0; z < BlockVolume.N; z++, u += BlockVolume.N )
				{
					//u = z * N;
					//p = y * NN + x * N;
					var r = 0.0;
					for( var n = 0; n < BlockVolume.N; n++ )
						r += pValues[ p + n ] * pU[ u + n ];
					pResult[ z * BlockVolume.N2 + y * BlockVolume.N + x ] = r;
				}
			}
		}

		#endregion
	}
}