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
using System.Linq;

#endregion

internal static class ZigZag
{
	#region members

	private static int[]? _Values;

	#endregion

	#region properties

	private static int[] Values => _Values ??= Calculate();

	#endregion

	#region methods

	private static int[] Calculate()
	{
		var entries = new List<ZigZagEntry>();

		var i = 0;
		for( var z = 0; z < BlockVolume.N; z++ )
		for( var y = 0; y < BlockVolume.N; y++ )
		for( var x = 0; x < BlockVolume.N; x++ )
		{
			entries.Add( new ZigZagEntry( x, y, z, i++ ) );
		}

		return entries.OrderBy( e => e.X * e.X + e.Y * e.Y + e.Z * e.Z )
			.ThenBy( e => e.Z )
			.ThenBy( e => e.Y )
			.ThenBy( e => e.X )
			.Select( e => e.Index )
			.ToArray();
	}

	/// <summary>
	/// Reorders the buffer so the lower frequencies come first.
	/// </summary>
	public static void Apply( Span<double> values, Span<double> result )
	{
		for( var i = 0; i < BlockVolume.N3; i++ )
			result[ i ] = values[ Values[ i ] ];
	}

	/// <summary>
	/// Reverses the order performed by the <see cref="Apply"/> method.
	/// </summary>
	public static void Reverse( Span<double> values, Span<double> result )
	{
		for( var i = 0; i < BlockVolume.N3; i++ )
			result[ Values[ i ] ] = values[ i ];
	}

	#endregion

	private readonly record struct ZigZagEntry( int X, int Y, int Z, int Index );
}