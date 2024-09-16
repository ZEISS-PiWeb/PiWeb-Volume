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
	using System.Runtime.Intrinsics;

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

		internal static void Transform( double[] values, double[] result, bool inverse = false )
		{
			var pU = inverse ? Ut : U;
			int u, p, x, y, z;

			Vector512<double> vecv;

			//iterieren über x (n 0..N): values[ n, y, z ] * u[ n, x ] ;
			for( z = 0, p = 0; z < BlockVolume.N; z++ )
			for( y = 0; y < BlockVolume.N; y++, p += BlockVolume.N )
			{
				vecv = Vector512.Create( values, p );
				for( x = 0, u = 0; x < BlockVolume.N; x++, u += BlockVolume.N )
				{
					//u = x * N;
					//p = z * NN + y * N;
					result[ z * BlockVolume.N2 + x * BlockVolume.N + y ] = Vector512.Sum( Vector512.Multiply( vecv, Vector512.Create( pU, u ) ) );
				}
			}

			//iterieren über y (n 0..N): values[ x, n, z ] * u[ n, y ]
			for( z = 0, p = 0; z < BlockVolume.N; z++ )
			for( x = 0; x < BlockVolume.N; x++, p += BlockVolume.N )
			{
				vecv = Vector512.Create( result, p );
				for( y = 0, u = 0; y < BlockVolume.N; y++, u += BlockVolume.N )
				{
					//u = y * N;
					//p = z * NN + x * N;
					values[ y * BlockVolume.N2 + x * BlockVolume.N + z ] = Vector512.Sum( Vector512.Multiply( vecv, Vector512.Create( pU, u ) ) );
				}
			}

			//iterieren über z (n 0..N): values[ x, y, n ] * u[ n, z ]
			for( y = 0, p = 0; y < BlockVolume.N; y++ )
			for( x = 0; x < BlockVolume.N; x++, p += BlockVolume.N )
			{
				vecv = Vector512.Create( values, p );
				for( z = 0, u = 0; z < BlockVolume.N; z++, u += BlockVolume.N )
				{
					//u = z * N;
					//p = y * NN + x * N;
					result[ z * BlockVolume.N2 + y * BlockVolume.N + x ] = Vector512.Sum( Vector512.Multiply( vecv, Vector512.Create( pU, u ) ) );
				}
			}
		}

		#endregion
	}
}