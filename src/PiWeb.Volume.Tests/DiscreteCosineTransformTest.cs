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
using System.IO;
using NUnit.Framework;
using Zeiss.PiWeb.Volume.Block;
using Zeiss.PiWeb.Volume.Convert;

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
	public void Test_Dct_Blocks(
		[Range( 0, BlockVolume.N - 1 )] int u,
		[Range( 0, BlockVolume.N - 1 )] int v,
		[Range( 0, BlockVolume.N - 1 )] int w )
	{
		var input = CreateTestBlock( u, v, w );
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

	[Test, Explicit]
	public void CreateTestVolume()
	{
		var data = new byte[ BlockVolume.N3 * BlockVolume.N3 ];
		const int s = BlockVolume.N2;
		const int ss = s * s;

		for( var u = 0; u < BlockVolume.N; u++ )
		{
			var gx = u * BlockVolume.N;
			for( var v = 0; v < BlockVolume.N; v++ )
			{
				var gy = v * BlockVolume.N;
				for( var w = 0; w < BlockVolume.N; w++ )
				{
					var gz = w * BlockVolume.N;
					var block = CreateTestBlock( u, v, w );

					for( var x = 0; x < BlockVolume.N; x++ )
					for( var y = 0; y < BlockVolume.N; y++ )
					for( var z = 0; z < BlockVolume.N; z++ )
					{
						var gi = ( gz + z ) * ss + ( gy + y ) * s + gx + x;
						var i = z * BlockVolume.N2 + y * BlockVolume.N + x;
						data[ gi ] = (byte)Math.Max( 0, Math.Min( byte.MaxValue, block[ i ] ) );
					}
				}
			}
		}

		using var stream = File.Create( "C:/temp/dct_test.uint8_scv" );

		var scv = Scv.FromMetaData( new VolumeMetadata( s, s, s, 1, 1, 1 ), 8 );
		scv.Write( stream );
		stream.Seek( scv.HeaderLength, SeekOrigin.Begin );
		stream.Write( data, 0, data.Length );
	}

	[Test, Explicit]
	public void CreateGridTestVolume()
	{
		var data = new byte[ BlockVolume.N3 * BlockVolume.N3 ];
		const int s = BlockVolume.N2;
		const int ss = s * s;

		var hf = BlockVolume.N + 1;

		for( var x = 0; x < BlockVolume.N2; x++ )
		for( var y = 0; y < BlockVolume.N2; y++ )
		for( var z = 0; z < BlockVolume.N2; z++ )
		{
			var i = z * ss + y * s + x;
			data[ i ] = z % hf == 0 || y % hf == 0 || x % hf == 0 ? byte.MaxValue : byte.MinValue;
		}

		using var stream = File.Create( "C:/temp/dct_hf_test.uint8_scv" );

		var scv = Scv.FromMetaData( new VolumeMetadata( s, s, s, 1, 1, 1 ), 8 );
		scv.Write( stream );
		stream.Seek( scv.HeaderLength, SeekOrigin.Begin );
		stream.Write( data, 0, data.Length );
	}

	private static double[] CreateTestBlock( int u, int v, int w )
	{
		var input = new double[ BlockVolume.N3 ];

		var min = double.MaxValue;
		var max = double.MinValue;
		for( var z = 0; z < BlockVolume.N; z++ )
		for( var y = 0; y < BlockVolume.N; y++ )
		for( var x = 0; x < BlockVolume.N; x++ )
		{
			var value = DiscreteCosineTransform.U[ u * BlockVolume.N + x ] *
				DiscreteCosineTransform.U[ v * BlockVolume.N + y ] *
				DiscreteCosineTransform.U[ w * BlockVolume.N + z ];

			min = Math.Min( min, value );
			max = Math.Max( max, value );
			input[ z * BlockVolume.N2 + y * BlockVolume.N + x ] = value;
		}

		var center = ( max + min ) / 2;
		var range = Math.Max( Math.Abs( max - center ), Math.Abs( min - center ) );
		var factor = range == 0 ? 0 : sbyte.MaxValue / range;
		for( var i = 0; i < BlockVolume.N3; i++ )
			input[ i ] = sbyte.MaxValue + ( input[ i ] - center ) * factor;

		return input;
	}

	#endregion
}