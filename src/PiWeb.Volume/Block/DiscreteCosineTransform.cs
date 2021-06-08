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
	/// Helper class to perform the discrete cosine transformation.
	/// </summary>
	internal static class DiscreteCosineTransform
	{
		#region members

		private static readonly double[] U = CalculateCoefficients( false );
		private static readonly double[] Ut = CalculateCoefficients( true );

		#endregion

		#region methods

		private static double[] CalculateCoefficients( bool invert )
		{
			var result = new double[ BlockVolume.N2 ];

			for( var y = 0; y < BlockVolume.N; y++ )
			{
				for( var x = 0; x < BlockVolume.N; x++ )
				{
					var v = 0.5 * ( y == 0 ? 0.5 * Math.Sqrt( 2 ) : Math.Cos( y * ( x * 2 + 1 ) * Math.PI / ( BlockVolume.N * 2 ) ) );
					result[ invert ? x * BlockVolume.N + y : x + BlockVolume.N * y ] = v;
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

					pResult[ z * BlockVolume.N2 + x * BlockVolume.N + y ] =
						pValues[ p + 0 ] * pU[ u + 0 ] +
						pValues[ p + 1 ] * pU[ u + 1 ] +
						pValues[ p + 2 ] * pU[ u + 2 ] +
						pValues[ p + 3 ] * pU[ u + 3 ] +
						pValues[ p + 4 ] * pU[ u + 4 ] +
						pValues[ p + 5 ] * pU[ u + 5 ] +
						pValues[ p + 6 ] * pU[ u + 6 ] +
						pValues[ p + 7 ] * pU[ u + 7 ];
				}

				//iterieren über y (n 0..N): values[ x, n, z ] * u[ n, y ]
				for( z = 0, p = 0; z < BlockVolume.N; z++ )
				for( x = 0; x < BlockVolume.N; x++, p += BlockVolume.N )
				for( y = 0, u = 0; y < BlockVolume.N; y++, u += BlockVolume.N )
				{
					//u = y * N;
					//p = z * NN + x * N;

					pValues[ y * BlockVolume.N2 + x * BlockVolume.N + z ] =
						pResult[ p + 0 ] * pU[ u + 0 ] +
						pResult[ p + 1 ] * pU[ u + 1 ] +
						pResult[ p + 2 ] * pU[ u + 2 ] +
						pResult[ p + 3 ] * pU[ u + 3 ] +
						pResult[ p + 4 ] * pU[ u + 4 ] +
						pResult[ p + 5 ] * pU[ u + 5 ] +
						pResult[ p + 6 ] * pU[ u + 6 ] +
						pResult[ p + 7 ] * pU[ u + 7 ];
				}

				//iterieren über z (n 0..N): values[ x, y, n ] * u[ n, z ]
				for( y = 0, p = 0; y < BlockVolume.N; y++ )
				for( x = 0; x < BlockVolume.N; x++, p += BlockVolume.N )
				for( z = 0, u = 0; z < BlockVolume.N; z++, u += BlockVolume.N )
				{
					//u = z * N;
					//p = y * NN + x * N;

					pResult[ z * BlockVolume.N2 + y * BlockVolume.N + x ] =
						pValues[ p + 0 ] * pU[ u + 0 ] +
						pValues[ p + 1 ] * pU[ u + 1 ] +
						pValues[ p + 2 ] * pU[ u + 2 ] +
						pValues[ p + 3 ] * pU[ u + 3 ] +
						pValues[ p + 4 ] * pU[ u + 4 ] +
						pValues[ p + 5 ] * pU[ u + 5 ] +
						pValues[ p + 6 ] * pU[ u + 6 ] +
						pValues[ p + 7 ] * pU[ u + 7 ];
				}
			}
		}

		#endregion
	}
}