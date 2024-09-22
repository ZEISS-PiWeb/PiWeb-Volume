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

		public static readonly Vector512<double>[] U = CalculateCoefficients( false );
		public static readonly Vector512<double>[] Ut = CalculateCoefficients( true );

		#endregion

		#region methods

		//https://dev.to/marycheung021213/understanding-dct-and-quantization-in-jpeg-compression-1col
		private static Vector512<double>[] CalculateCoefficients( bool invert )
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

			return
			[
				Vector512.Create( result, 0 ),
				Vector512.Create( result, BlockVolume.N ),
				Vector512.Create( result, BlockVolume.N * 2 ),
				Vector512.Create( result, BlockVolume.N * 3 ),
				Vector512.Create( result, BlockVolume.N * 4 ),
				Vector512.Create( result, BlockVolume.N * 5 ),
				Vector512.Create( result, BlockVolume.N * 6 ),
				Vector512.Create( result, BlockVolume.N * 7 ),
			];
		}

		internal static void Transform( Span<double> values, Span<double> result, bool inverse = false )
		{
			var pU = inverse ? Ut.AsSpan() : U.AsSpan();
			int p, x, y, z;

			Vector512<double> vecv;

			//iterieren über x (n 0..N): values[ n, y, z ] * u[ n, x ] ;
			for( z = 0, p = 0; z < BlockVolume.N; z++ )
			for( y = 0; y < BlockVolume.N; y++, p += BlockVolume.N )
			{
				vecv = Vector512.Create<double>( values.Slice( p, BlockVolume.N ) );
				for( x = 0; x < BlockVolume.N; x++ )
				{
					//u = x * N;
					//p = z * NN + y * N;
					result[ z * BlockVolume.N2 + x * BlockVolume.N + y ] = Vector512.Sum( Vector512.Multiply( vecv, pU[ x ] ) );
				}
			}

			//iterieren über y (n 0..N): values[ x, n, z ] * u[ n, y ]
			for( z = 0, p = 0; z < BlockVolume.N; z++ )
			for( x = 0; x < BlockVolume.N; x++, p += BlockVolume.N )
			{
				vecv = Vector512.Create<double>( result.Slice( p, BlockVolume.N ) );
				for( y = 0; y < BlockVolume.N; y++ )
				{
					//u = y * N;
					//p = z * NN + x * N;
					values[ y * BlockVolume.N2 + x * BlockVolume.N + z ] = Vector512.Sum( Vector512.Multiply( vecv, pU[ y ] ) );
				}
			}

			//iterieren über z (n 0..N): values[ x, y, n ] * u[ n, z ]
			for( y = 0, p = 0; y < BlockVolume.N; y++ )
			for( x = 0; x < BlockVolume.N; x++, p += BlockVolume.N )
			{
				vecv = Vector512.Create<double>( values.Slice( p, BlockVolume.N ) );
				for( z = 0; z < BlockVolume.N; z++ )
				{
					//u = z * N;
					//p = y * NN + x * N;
					result[ z * BlockVolume.N2 + y * BlockVolume.N + x ] = Vector512.Sum( Vector512.Multiply( vecv, pU[ z ] ) );
				}
			}
		}

		#endregion
	}
}