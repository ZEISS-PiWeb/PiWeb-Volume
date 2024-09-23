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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

#endregion

/// <summary>
/// Helper class to calculate the quantization.
/// </summary>
internal static class Quantization
{
	#region methods

	/// <summary>
	/// Reads a quantization matrix from the specified <paramref name="reader"/>
	/// </summary>
	public static double[] Read( BinaryReader reader, bool invert )
	{
		var data = reader.ReadBytes( BlockVolume.N3 * sizeof( double ) ).AsSpan();
		var values = MemoryMarshal.Cast<byte, double>( data );

		if( !invert )
			return values.ToArray();

		for( var i = 0; i < BlockVolume.N3; i++ )
			values[ i ] = 1.0 / values[ i ];

		return values.ToArray();
	}

	/// <summary>
	/// Writes a quantization matrix to the specified <paramref name="writer"/>
	/// </summary>
	public static void Write( BinaryWriter writer, double[] values )
	{
		var data = MemoryMarshal.Cast<double, byte>( values );
		writer.Write( data );
	}

	/// <summary>
	/// Calculates a generic quantization matrix based on the quality setting.
	/// </summary>
	public static double[] Calculate( VolumeCompressionOptions options )
	{
		if( options.EncoderOptions.TryGetQuantization( out var result ) )
			return result;
		if( !options.EncoderOptions.TryGetDouble( "quality", out var quality ) )
			quality = 75;
		if( !options.EncoderOptions.TryGetDouble( "quantizationBase", out var quantizationBase ) )
			quantizationBase = 12;
		if( !options.EncoderOptions.TryGetDouble( "quantizationGain", out var quantizationGain ) )
			quantizationGain = 1;

		return Calculate( quality, quantizationBase, quantizationGain );
	}

	private static bool TryGetQuantization( this IReadOnlyDictionary<string, string> options, [NotNullWhen( true )] out double[]? result )
	{
		result = default;
		if( !options.TryGetValue( "quantization", out var quantizationString ) )
			return false;

		var parts = quantizationString.Split( ';' );
		if( parts.Length != BlockVolume.N3 )
			return false;

		result = new double[ BlockVolume.N3 ];
		for( var i = 0; i < BlockVolume.N3; i++ )
		{
			if( !double.TryParse( parts[ i ], NumberStyles.Float, CultureInfo.InvariantCulture, out var value ) )
				return false;

			result[ i ] = value;
		}

		return true;
	}

	private static bool TryGetDouble( this IReadOnlyDictionary<string, string> options, string name, out double value )
	{
		value = default;
		return
			options.TryGetValue( name, out var valueString ) &&
			double.TryParse( valueString, NumberStyles.Float, CultureInfo.InvariantCulture, out value );
	}

	private static double[] Calculate( double quality = 75, double quantizationBase = 12, double quantizationGain = 1 )
	{
		var scale = QualityScaling( (int)quality );
		var values = new double[ BlockVolume.N3 ];

		var i = 0;

		for( var z = 0; z < BlockVolume.N; z++ )
		for( var y = 0; y < BlockVolume.N; y++ )
		for( var x = 0; x < BlockVolume.N; x++ )
		{
			var distance = Math.Max( 0, Math.Max( x, Math.Max( y, z ) ) - 1 );
			var baseValue = quantizationBase * ( 1 + quantizationGain * distance );

			var value = Math.Max( 1, Math.Min( short.MaxValue, ( baseValue * 2.0 * scale + 50 ) / 100 ) );
			values[ i++ ] = 1.0 / value;
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

	/// <summary>
	/// Multiplies the <paramref name="quantization"/> with <paramref name="values"/> and stores the result in
	/// <paramref name="result"/>.
	/// </summary>
	public static void Apply( ReadOnlySpan<double> quantization, ReadOnlySpan<double> values, Span<double> result )
	{
		for( var i = 0; i < BlockVolume.N3; i += 8 )
		{
			Vector512.Multiply(
					Vector512.Create( values.Slice( i, 8 ) ),
					Vector512.Create( quantization.Slice( i, 8 ) ) )
				.CopyTo( result.Slice( i, 8 ) );
		}
	}

	#endregion
}