#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Block;

#region usings

using System;
using System.Runtime.InteropServices;
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

	internal static void Transform(
		Span<double> values,
		Span<double> result,
		bool inverse = false,
		ulong nonEmptyVectors = ulong.MaxValue )
	{
		var pU = inverse ? Ut.AsSpan() : U.AsSpan();

		var inputVectors = MemoryMarshal.Cast<double, Vector512<double>>( values );
		var resultVectors = MemoryMarshal.Cast<double, Vector512<double>>( result );

		nonEmptyVectors = TransformDirection( inputVectors, result, pU, nonEmptyVectors );
		nonEmptyVectors = TransformDirection( resultVectors, values, pU, nonEmptyVectors );
		TransformDirection( inputVectors, result, pU, nonEmptyVectors );
	}

	private static ulong TransformDirection(
		Span<Vector512<double>> inputVectors,
		Span<double> result,
		Span<Vector512<double>> coefficients,
		ulong nonEmptyInputVectors )
	{
		result.Clear();

		var nonEmptyResultVectors = 0UL;

		for( ushort p = 0; p < BlockVolume.N2; p++ )
		{
			if( ( nonEmptyInputVectors & ( 1UL << p ) ) == 0 )
				continue;

			var input = inputVectors[ p ];

			//This loop is unrolled for performance optimization
			result[ 0 * BlockVolume.N2 + p ] = Vector512.Sum( Vector512.Multiply( input, coefficients[ 0 ] ) );
			result[ 1 * BlockVolume.N2 + p ] = Vector512.Sum( Vector512.Multiply( input, coefficients[ 1 ] ) );
			result[ 2 * BlockVolume.N2 + p ] = Vector512.Sum( Vector512.Multiply( input, coefficients[ 2 ] ) );
			result[ 3 * BlockVolume.N2 + p ] = Vector512.Sum( Vector512.Multiply( input, coefficients[ 3 ] ) );
			result[ 4 * BlockVolume.N2 + p ] = Vector512.Sum( Vector512.Multiply( input, coefficients[ 4 ] ) );
			result[ 5 * BlockVolume.N2 + p ] = Vector512.Sum( Vector512.Multiply( input, coefficients[ 5 ] ) );
			result[ 6 * BlockVolume.N2 + p ] = Vector512.Sum( Vector512.Multiply( input, coefficients[ 6 ] ) );
			result[ 7 * BlockVolume.N2 + p ] = Vector512.Sum( Vector512.Multiply( input, coefficients[ 7 ] ) );

			nonEmptyResultVectors |= 0x0101010101010101UL << ( p / BlockVolume.N );
		}

		return nonEmptyResultVectors;
	}

	#endregion
}