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
using Zeiss.PiWeb.Volume.Block;
using Zeiss.PiWeb.Volume.Convert;

#endregion

public static class VolumeTestHelper
{
	#region methods

	/// <summary>
	/// Creates a volume that resembles the dct frequencies and will therefore have very low to zero noise when
	/// compressed.
	/// The output volume has N * N * N blocks, where every block has different frequencies.
	/// </summary>
	public static UncompressedVolume CreateLowNoiseVolume()
	{
		var slices = new byte[ BlockVolume.N2 ][];
		for( var z = 0; z < BlockVolume.N2; z++ )
			slices[ z ] = new byte[ BlockVolume.N2 * BlockVolume.N2 ];

		for( var w = 0; w < BlockVolume.N; w++ )
		for( var v = 0; v < BlockVolume.N; v++ )
		for( var u = 0; u < BlockVolume.N; u++ )
		{
			var block = CreateLowNoiseBlock( u, v, w );
			WriteBlockToSlices( u, v, w, block, slices );
		}

		var metaData = new VolumeMetadata( BlockVolume.N2, BlockVolume.N2, BlockVolume.N2, 1, 1, 1 );

		return new UncompressedVolume( metaData, slices );
	}

	public static byte[] CreateLowNoiseBlock( int u, int v, int w )
	{
		var values = new double[ BlockVolume.N3 ];
		var result = new byte[ BlockVolume.N3 ];

		var min = double.MaxValue;
		var max = double.MinValue;
		for( var z = 0; z < BlockVolume.N; z++ )
		for( var y = 0; y < BlockVolume.N; y++ )
		for( var x = 0; x < BlockVolume.N; x++ )
		{
			var value = DiscreteCosineTransform.U[ u ][ x ] *
				DiscreteCosineTransform.U[ v ][ y ] *
				DiscreteCosineTransform.U[ w ][ z ];

			min = Math.Min( min, value );
			max = Math.Max( max, value );
			values[ z * BlockVolume.N2 + y * BlockVolume.N + x ] = value;
		}

		var center = ( max + min ) / 2;
		var range = Math.Max( Math.Abs( max - center ), Math.Abs( min - center ) );
		var factor = range == 0 ? 0 : sbyte.MaxValue / range;

		for( var i = 0; i < BlockVolume.N3; i++ )
			result[ i ] = (byte)Math.Max( 0, Math.Min( byte.MaxValue, sbyte.MaxValue + ( values[ i ] - center ) * factor ) );

		return result;
	}

	private static void WriteBlockToSlices( int bx, int by, int bz, byte[] block, byte[][] slices )
	{
		for( var z = 0; z < BlockVolume.N; z++ )
		for( var y = 0; y < BlockVolume.N; y++ )
		for( var x = 0; x < BlockVolume.N; x++ )
		{
			var indexInSlice = ( by * BlockVolume.N + y ) * BlockVolume.N2 + bx * BlockVolume.N + x;
			var sliceIndex = bz * BlockVolume.N + z;
			var indexInBlock = z * BlockVolume.N2 + y * BlockVolume.N + x;
			slices[ sliceIndex ][ indexInSlice ] = block[ indexInBlock ];
		}
	}

	/// <summary>
	/// Creates a volume that's black except for white voxels in different positions of the block. This kind of volume
	/// will yield a high PSNR.
	/// The output volume has N * N * N blocks, where every block has the white voxels on a different position.
	/// </summary>
	public static UncompressedVolume CreateHighNoiseVolume()
	{
		var slices = new VolumeSlice[ BlockVolume.N2 ];

		const int hf = BlockVolume.N + 1;

		for( ushort z = 0; z < BlockVolume.N2; z++ )
		{
			var slice = new byte[ BlockVolume.N2 * BlockVolume.N2 ];

			for( var x = 0; x < BlockVolume.N2; x++ )
			for( var y = 0; y < BlockVolume.N2; y++ )
			{
				slice[ y * BlockVolume.N2 + x ] = z % hf == 0 || y % hf == 0 || x % hf == 0 ? byte.MaxValue : byte.MinValue;
				slices[ z ] = new VolumeSlice( new VolumeSliceDefinition( Direction.Z, z ), slice );
			}
		}

		var metaData = new VolumeMetadata( BlockVolume.N2, BlockVolume.N2, BlockVolume.N2, 1, 1, 1 );

		return new UncompressedVolume( metaData, slices );
	}


	public static UncompressedVolume CreateSectionVolume()
	{
		var slices = new byte[ BlockVolume.N ][];
		for( var z = 0; z < BlockVolume.N; z++ )
			slices[ z ] = new byte[ BlockVolume.N2 * BlockVolume.N2 ];

		for( var x = 0; x < BlockVolume.N; x++ )
		for( var y = 0; y < BlockVolume.N; y++ )
		{
			var m = ( x + 1 ) * 0.25;
			var n = y * -1;
			var block = CreateSectionBlock( m, n );
			WriteBlockToSlices( x, y, 0, block, slices );
		}

		var metaData = new VolumeMetadata( BlockVolume.N2, BlockVolume.N2, BlockVolume.N, 1, 1, 1 );

		return new UncompressedVolume( metaData, slices );
	}

	public static UncompressedVolume CreatePartialVolume( Volume source, VolumeRange rx, VolumeRange ry, VolumeRange rz )
	{
		var region = new VolumeRegion( rx, ry );
		var slices = new byte[ rz.Size ][];
		var sx = rx.Size;
		var sy = ry.Size;
		var sliceSize = sx * sy;
		for( var z = 0; z < rz.Size; z++ )
			slices[ z ] = new byte[ sliceSize ];

		var buffer = new byte[ source.Metadata.GetSliceLength( Direction.Z ) ];
		var stride = source.Metadata.SizeX;

		for( ushort z = rz.Start, lz = 0; z <= rz.End; z++, lz++ )
		{
			source.GetSlice( new VolumeSliceDefinition( Direction.Z, z, region ), buffer );
			for( ushort y = ry.Start, ly = 0; y <= ry.End; y++, ly++ )
				Array.Copy( buffer, y * stride + rx.Start, slices[ lz ], ly * sx, sx );
		}

		var metaData = new VolumeMetadata( rx.Size, ry.Size, rz.Size, 1, 1, 1 );

		return new UncompressedVolume( metaData, slices );
	}

	public static byte[] CreateSectionBlock( double m, double n )
	{
		var result = new byte[ BlockVolume.N3 ];

		for( var z = 0; z < BlockVolume.N; z++ )
		for( var y = 0; y < BlockVolume.N; y++ )
		for( var x = 0; x < BlockVolume.N; x++ )
		{
			var value = y < m * x + n ? byte.MaxValue : byte.MinValue;
			result[ z * BlockVolume.N2 + y * BlockVolume.N + x ] = value;
		}

		return result;
	}

	public static byte[] CreateHighNoiseBlock( int u, int v, int w )
	{
		var result = new byte[ BlockVolume.N3 ];

		for( var z = 0; z < BlockVolume.N; z++ )
		for( var y = 0; y < BlockVolume.N; y++ )
		for( var x = 0; x < BlockVolume.N; x++ )
		{
			var value = x == u || y == v || z == w
				? byte.MaxValue
				: byte.MinValue;

			result[ z * BlockVolume.N2 + y * BlockVolume.N + x ] = value;
		}

		return result;
	}

	public static Noise? CalculateNoise( UncompressedVolume original, Volume compressed )
	{
		var sx = original.Metadata.SizeX;
		var sy = original.Metadata.SizeY;
		var sz = original.Metadata.SizeZ;

		if( compressed.Metadata.SizeX != sx || compressed.Metadata.SizeY != sy || compressed.Metadata.SizeZ != sz )
			return null;

		var sxy = sx * sy;
		var noises = new ulong[ byte.MaxValue ];
		var originalBuffer = new byte[ sxy ];
		var compressedBuffer = new byte[ sxy ];

		for( ushort z = 0; z < sz; z++ )
		{
			original.GetSlice( new VolumeSliceDefinition( Direction.Z, z ), originalBuffer );
			compressed.GetSlice( new VolumeSliceDefinition( Direction.Z, z ), compressedBuffer );

			for( var i = 0; i < sxy; i++ )
			{
				var noise = Math.Abs( originalBuffer[ i ] - compressedBuffer[ i ] );
				noises[ noise ]++;
			}
		}

		var totalCount = sxy * sz;
		var countQ95 = totalCount * 0.95;
		var countMean = totalCount * 0.5;
		var sum = 0UL;
		var sumNoise = 0UL;
		byte peak = 0;
		byte mean = 0;
		byte q95 = 0;

		for( byte i = 0; i < byte.MaxValue; i++ )
		{
			var noise = noises[ i ];

			if( noise == 0 )
				continue;

			sum += noise;
			sumNoise += i * noise;

			peak = i;
			if( sum >= countMean && mean == 0 )
				mean = i;
			if( sum >= countQ95 && q95 == 0 )
				q95 = i;
		}

		return new Noise( peak, mean, (double)sumNoise / totalCount, q95, noises );
	}

	public static UncompressedVolume LoadUncompressedVolume( string fileName )
	{
		using var scv = File.OpenRead( fileName );
		var bitDepthFromExtension = string.Equals( Path.GetExtension( fileName ), ".uint16_scv" ) ? 16 : 8;

		return ConvertVolume.FromScv(
			scv,
			bitDepthFromExtension,
			false, 0, 0, false ) as UncompressedVolume;
	}

	#endregion

	public readonly record struct Noise( byte Peak, byte Mean, double Average, byte Q95, ulong[] Values );
}