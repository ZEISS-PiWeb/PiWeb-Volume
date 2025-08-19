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
using System.IO;
using NUnit.Framework;
using Zeiss.PiWeb.Volume.Block;

#endregion

[TestFixture]
public class BlockVolumeSliceRangeCollectorTest
{
	#region members

	private CompressedVolume _Volume = null!;

	//The volume is compressed and has compression artifacts. This is the estimated PSNR for the test volume.
	private const int Tolerance = 16;

	#endregion

	#region methods

	[OneTimeSetUp]
	public void SetUpTests()
	{
		using var stream = File.OpenRead( Path.Combine( Paths.TestData, "high_noise_volume.volx" ) );
		_Volume = Volume.Load( stream );

		Assert.That( _Volume, Is.InstanceOf<BlockVolume>() );
		Assert.That( _Volume.Metadata.SizeX, Is.EqualTo( BlockVolume.N2 ) );
		Assert.That( _Volume.Metadata.SizeY, Is.EqualTo( BlockVolume.N2 ) );
		Assert.That( _Volume.Metadata.SizeZ, Is.EqualTo( BlockVolume.N2 ) );
	}

	[Test]
	public void Test_Get_Single_Full_Slice( [Values] Direction direction, [Values( 0, 8, 16, 32, 48 )] int index )
	{
		var slice = _Volume.GetSlice( new VolumeSliceDefinition( direction, (ushort)index ) );
		AssertSliceValues( slice, index );
	}

	public static VolumeRange[] Ranges =
	[
		new VolumeRange( 0, 7 ),
		new VolumeRange( 0, 15 ),
		new VolumeRange( 4, 12 ),
		new VolumeRange( 4, 60 ),
		new VolumeRange( 50, 63 ),
		new VolumeRange( 56, 63 )
	];

	[Test]
	public void Test_Get_Single_Slice_With_ROI(
		[Values] Direction direction,
		[Values( 0, 8, 16, 32, 48 )] int index,
		[ValueSource( nameof( Ranges ) )] VolumeRange u,
		[ValueSource( nameof( Ranges ) )] VolumeRange v )
	{
		var region = new VolumeRegion( u, v );
		var slice = _Volume.GetSlice( new VolumeSliceDefinition( direction, (ushort)index, region ) );
		AssertSliceValues( slice, index, region );
	}

	[Test]
	public void Test_Get_Multiple_Slices_With_ROI(
		[Values] Direction direction,
		[Values( 0, 8, 16, 32, 48 )] int index,
		[Values( 1, 8, 12 )] int count,
		[ValueSource( nameof( Ranges ) )] VolumeRange u,
		[ValueSource( nameof( Ranges ) )] VolumeRange v )
	{
		var region = new VolumeRegion( u, v );
		var rangeDefinition = new VolumeSliceRangeDefinition( direction, (ushort)index, (ushort)( index + count ), region );
		var range = _Volume.GetSliceRange( rangeDefinition );

		foreach( var slice in range )
			AssertSliceValues( slice, slice.Index, region );
	}

	private static void AssertSliceValues( VolumeSlice slice, int index, VolumeRegion? region = null )
	{
		for( ushort b = 0; b < BlockVolume.N2; b++ )
		for( ushort a = 0; a < BlockVolume.N2; a++ )
		{
			if( region.HasValue && ( !region.Value.U.Contains( a ) || !region.Value.V.Contains( b ) ) )
				continue;

			var value = slice.Data[ a + b * BlockVolume.N2 ];
			var expectedValue = index % ( BlockVolume.N + 1 ) == 0 ||
				a % ( BlockVolume.N + 1 ) == 0 ||
				b % ( BlockVolume.N + 1 ) == 0
					? byte.MaxValue
					: byte.MinValue;

			var difference = Math.Abs( value - expectedValue );
			Assert.That( difference, Is.LessThanOrEqualTo( Tolerance ) );
		}
	}

	#endregion
}