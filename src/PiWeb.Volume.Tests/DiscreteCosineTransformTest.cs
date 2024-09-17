#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2024                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Tests;

#region usings

using System;
using NUnit.Framework;
using Zeiss.PiWeb.Volume.Block;

#endregion

[TestFixture]
public class DiscreteCosineTransformTest
{
	#region methods

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