#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2024                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Tests.Block;

#region usings

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using NUnit.Framework;
using Zeiss.PiWeb.Volume.Block;

#endregion

[TestFixture]
public class DiscreteCosineTransformTest
{
	#region methods

	[Test]
	public void TestBlock()
	{
		var values = new double[]
		{
			77, 72, 83, 126, 172, 182, 185, 182,
			72, 68, 84, 129, 174, 181, 185, 182,
			71, 70, 83, 128, 174, 185, 185, 184,
			74, 78, 91, 133, 171, 181, 184, 185,
			73, 78, 94, 134, 169, 179, 184, 187,
			68, 75, 97, 138, 171, 181, 180, 185,
			69, 81, 98, 144, 173, 180, 182, 183,
			70, 76, 102, 149, 176, 182, 183, 186
		};

		var result = new double[ 64 ];


		var valueVectors = MemoryMarshal.Cast<double, Vector512<double>>( values );


		DiscreteCosineTransform.TransformDirection2D( valueVectors, result, DiscreteCosineTransform.U );

		for( var y = 0; y < 8; y++ )
		{
			for( var x = y + 1; x < 8; x++ )
			{
				var tmp = result[ x * 8 + y ];
				result[ x * 8 + y ] = result[ y * 8 + x ];
				result[ y * 8 + x ] = tmp;
			}
		}

		var resulVectors = MemoryMarshal.Cast<double, Vector512<double>>( result );

		for( var y = 0; y < 8; y++ )
		{
			for( var x = 0; x < 8; x++ )
				Debug.Write( string.Format( CultureInfo.InvariantCulture, "{0:0.00},", result[ y * 8 + x ] ) );

			Debug.WriteLine( "\r\n" );
		}

		var quantization = new double[]
		{
			6, 4, 4, 6, 10, 16, 20, 24,
			5, 5, 6, 8, 10, 23, 24, 22,
			6, 5, 6, 10, 16, 23, 28, 22,
			6, 7, 9, 12, 20, 35, 32, 25,
			7, 9, 15, 22, 27, 44, 41, 31,
			10, 14, 22, 26, 32, 42, 45, 37,
			20, 26, 31, 35, 41, 48, 48, 40,
			29, 37, 38, 39, 45, 40, 41, 40,
		};

		DiscreteCosineTransform.TransformDirection2D( resulVectors, values, DiscreteCosineTransform.U );

		for( var y = 0; y < 8; y++ )
		{
			for( var x = 0; x < 8; x++ )
				Debug.Write( string.Format( CultureInfo.InvariantCulture, "{0:0.00},", 1.0 / quantization[ y * 8 + x ] ) );

			Debug.WriteLine( "\r\n" );
		}

		Debug.WriteLine( "\r\n" );
		Debug.WriteLine( "\r\n" );
		Debug.WriteLine( "\r\n" );

		for( var y = 0; y < 8; y++ )
		{
			for( var x = 0; x < 8; x++ )
				Debug.Write( string.Format( CultureInfo.InvariantCulture, "{0:0.00},", DiscreteCosineTransform.Ut[ y ][ x ] ) );

			Debug.WriteLine( "\r\n" );
		}
	}

	[Test]
	public void Test_SingleColors( [Values( 0, 63, 64, 127, 128, 192, 255 )] byte value )
	{
		var input = new double[ BlockVolume.N3 ];
		var transformed = new double[ BlockVolume.N3 ];

		for( var i = 0; i < input.Length; i++ )
			input[ i ] = value;

		DiscreteCosineTransform.Transform( input, transformed );
		DiscreteCosineTransform.Transform( transformed, input, true );

		foreach( var resultValue in input )
			Assert.That( Math.Abs( resultValue - value ), Is.LessThan( 1e-6 ) );
	}

	[Test]
	public void Test_LowNoise_Blocks(
		[Range( 0, BlockVolume.N - 1 )] int u,
		[Range( 0, BlockVolume.N - 1 )] int v,
		[Range( 0, BlockVolume.N - 1 )] int w )
	{
		var input = VolumeTestHelper.CreateLowNoiseBlock( u, v, w );

		var buffer = new double[ BlockVolume.N3 ];
		var transformed = new double[ BlockVolume.N3 ];
		var transformedBack = new double[ BlockVolume.N3 ];

		Array.Copy( input, buffer, BlockVolume.N3 );

		DiscreteCosineTransform.Transform( buffer, transformed );

		Array.Copy( transformed, buffer, BlockVolume.N3 );

		DiscreteCosineTransform.Transform( buffer, transformedBack, true );

		for( var i = 0; i < BlockVolume.N3; i++ )
			Assert.That( Math.Abs( transformedBack[ i ] - input[ i ] ), Is.LessThan( 1e-6 ) );
	}

	[Test]
	public void Test_HighNoise_Blocks(
		[Range( 0, BlockVolume.N - 1 )] int u,
		[Range( 0, BlockVolume.N - 1 )] int v,
		[Range( 0, BlockVolume.N - 1 )] int w )
	{
		var input = VolumeTestHelper.CreateHighNoiseBlock( u, v, w );

		var buffer = new double[ BlockVolume.N3 ];
		var transformed = new double[ BlockVolume.N3 ];
		var transformedBack = new double[ BlockVolume.N3 ];

		Array.Copy( input, buffer, BlockVolume.N3 );

		DiscreteCosineTransform.Transform( buffer, transformed );

		Array.Copy( transformed, buffer, BlockVolume.N3 );

		DiscreteCosineTransform.Transform( buffer, transformedBack, true );

		for( var i = 0; i < BlockVolume.N3; i++ )
			Assert.That( Math.Abs( transformedBack[ i ] - input[ i ] ), Is.LessThan( 1e-6 ) );
	}

	#endregion
}