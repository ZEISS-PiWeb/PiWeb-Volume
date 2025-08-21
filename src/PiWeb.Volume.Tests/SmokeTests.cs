#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Tests;

#region usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Zeiss.PiWeb.Volume.Block;
using Zeiss.PiWeb.Volume.Convert;

#endregion

/// <summary>
/// Smoke tests that perform a high level test for basic functionality. These tests basically make sure,
/// that the code does not crash.
/// </summary>
[TestFixture]
public class SmokeTests
{
	#region properties

	public static SampleFile[] CompressedSamples { get; } = Directory
		.GetFiles( Paths.TestData, "*.volx", SearchOption.AllDirectories )
		.Select( f => new SampleFile( f ) )
		.ToArray();

	public static SampleFile[] UncompressedSamples { get; } = Directory
		.GetFiles( Paths.TestData, "*.uint8_scv", SearchOption.AllDirectories )
		.Select( f => new SampleFile( f ) )
		.ToArray();

	#endregion

	#region methods

	[Test, TestCaseSource( nameof( CompressedSamples ) )]
	public void LoadSamplesTest( SampleFile file )
	{
		using var stream = File.OpenRead( file.Filename );
		var logger = new ConsoleLogger();

		var compressedVolume = Volume.Load( stream, logger );

		Assert.That( compressedVolume, Is.Not.Null );
		Assert.That( compressedVolume.Metadata, Is.Not.Null );
	}

	[Test, TestCaseSource( nameof( CompressedSamples ) )]
	public void DecompressSamplesTest( SampleFile file )
	{
		using var stream = File.OpenRead( file.Filename );
		var logger = new ConsoleLogger();

		var compressedVolume = Volume.Load( stream, logger );
		var decompressedVolume = compressedVolume.Decompress( logger: logger );

		Assert.That( decompressedVolume, Is.Not.Null );
		Assert.That( decompressedVolume.Metadata, Is.Not.Null );
	}

	[Test, TestCaseSource( nameof( UncompressedSamples ) )]
	public void CompressAndDecompressSamplesTest( SampleFile file )
	{
		using var stream = File.OpenRead( file.Filename );
		var logger = new ConsoleLogger();

		var options = new VolumeCompressionOptions( BlockVolume.EncoderID, BlockVolume.PixelFormat, new Dictionary<string, string>
		{
			{ BlockVolume.QualityName, "95" }
		} );
		var uncompressedVolume = (UncompressedVolume)VolumeTestHelper.LoadUncompressedVolume( file.Filename );
		var compressedVolume = uncompressedVolume.Compress( options );
		var decompressedVolume = compressedVolume.Decompress( logger: logger );

		var noise = VolumeTestHelper.CalculateNoise( uncompressedVolume, decompressedVolume );

		Assert.That( noise, Is.Not.Null );
		Assert.That( noise?.Peak, Is.LessThanOrEqualTo( 16 ) );
	}

	[Test, TestCaseSource( nameof( CompressedSamples ) )]
	public void CreatePreviewForCompressedVolumeTest( SampleFile file )
	{
		using var stream = File.OpenRead( file.Filename );
		var logger = new ConsoleLogger();

		var compressedVolume = Volume.Load( stream, logger );
		var preview = compressedVolume.CreatePreview( 4, logger: logger );

		Assert.That( preview, Is.Not.Null );
		Assert.That( preview.Metadata, Is.Not.Null );
	}

	[Test, TestCaseSource( nameof( CompressedSamples ) )]
	public void CreatePreviewForDecompressedVolumeTest( SampleFile file )
	{
		using var stream = File.OpenRead( file.Filename );
		var logger = new ConsoleLogger();

		var compressedVolume = Volume.Load( stream, logger );
		var decompressedVolume = compressedVolume.Decompress( logger: logger );
		var preview = decompressedVolume.CreatePreview( 4, logger: logger );

		Assert.That( preview, Is.Not.Null );
		Assert.That( preview.Metadata, Is.Not.Null );
	}

	[Test, TestCaseSource( nameof( CompressedSamples ) )]
	public void SaveSamplesTest( SampleFile file )
	{
		var tempPath = Path.GetTempFileName();
		using var stream = File.OpenRead( file.Filename );

		using( var tempStream = File.OpenWrite( tempPath ) )
		{
			var logger = new ConsoleLogger();
			var options = new VolumeCompressionOptions();

			var volume = Volume.Load( stream, logger ).Decompress( logger: logger );
			volume.Save( tempStream, options, logger: logger );
		}

		File.Delete( tempPath );
	}

	[Test, Explicit]
	public void SaveLowNoiseVolume()
	{
		using var stream = File.Create( "low_noise_volume.uint8_scv" );

		var volume = VolumeTestHelper.CreateLowNoiseVolume();
		SaveUncompressedVolume( stream, volume );
	}

	[Test, Explicit]
	public void SaveHighNoiseVolume()
	{
		using var stream = File.Create( "high_noise_volume.uint8_scv" );

		var volume = VolumeTestHelper.CreateHighNoiseVolume();
		SaveUncompressedVolume( stream, volume );
	}

	[Test, Explicit]
	public void SaveSectionVolume()
	{
		using var stream = File.Create( "section_volume.uint8_scv" );

		var volume = VolumeTestHelper.CreateSectionVolume();
		SaveUncompressedVolume( stream, volume );
	}

	[Test, Explicit]
	public void SavePartialVolume()
	{
		using var outputStream = File.Create( "partial_volume.uint8_scv" );

		var sourceVolume = VolumeTestHelper.LoadUncompressedVolume( @"C:\Daten\Stecker.uint8_scv" );
		var partialVolume = VolumeTestHelper.CreatePartialVolume( sourceVolume, new VolumeRange( 824, 855 ), new VolumeRange( 496, 527 ), new VolumeRange( 496, 527 ) );
		SaveUncompressedVolume( outputStream, partialVolume );
	}

	[Test, Explicit]
	public void CompareHighNoiseVolume( [Values( 25, 50, 75, 85, 90, 95, 100 )] int quality )
	{
		var original = VolumeTestHelper.CreateHighNoiseVolume();
		var options = new VolumeCompressionOptions( BlockVolume.EncoderID, BlockVolume.PixelFormat, new Dictionary<string, string>
		{
			{ BlockVolume.QualityName, quality.ToString() }
		} );
		var compressed = original.Compress( options );

		var noise = VolumeTestHelper.CalculateNoise( original, compressed );

		Assert.That( noise, Is.Not.Null );

		Console.WriteLine( @$"Quality {quality} peak: {noise?.Peak}" );
		Console.WriteLine( @$"Quality {quality} average: {noise?.Average}" );
		Console.WriteLine( @$"Quality {quality} mean: {noise?.Mean}" );
		Console.WriteLine( @$"Quality {quality} q95: {noise?.Q95}" );
	}

	[Test, Explicit]
	public void CompareLowNoiseVolume( [Values( 25, 50, 75, 85, 90, 95, 100 )] int quality )
	{
		var original = VolumeTestHelper.CreateLowNoiseVolume();
		var options = new VolumeCompressionOptions( BlockVolume.EncoderID, BlockVolume.PixelFormat, new Dictionary<string, string>
		{
			{ BlockVolume.QualityName, quality.ToString() }
		} );
		var compressed = original.Compress( options );

		var noise = VolumeTestHelper.CalculateNoise( original, compressed );

		Assert.That( noise, Is.Not.Null );

		Console.WriteLine( @$"Quality {quality} peak: {noise?.Peak}" );
		Console.WriteLine( @$"Quality {quality} average: {noise?.Average}" );
		Console.WriteLine( @$"Quality {quality} mean: {noise?.Mean}" );
		Console.WriteLine( @$"Quality {quality} q95: {noise?.Q95}" );
	}


	private static void SaveUncompressedVolume( Stream stream, UncompressedVolume volume )
	{
		var scv = Scv.FromMetaData( volume.Metadata, 8 );
		var buffer = new byte[ volume.Metadata.GetSliceLength( Direction.Z ) ];

		scv.Write( stream );
		stream.Seek( scv.HeaderLength, SeekOrigin.Begin );

		for( ushort z = 0; z < volume.Metadata.SizeZ; z++ )
		{
			volume.GetSlice( new VolumeSliceDefinition( Direction.Z, z ), buffer );
			stream.Write( buffer );
		}
	}

	#endregion

	#region class SampleFile

	public readonly struct SampleFile
	{
		#region constructors

		public SampleFile( string filename )
		{
			Filename = filename;
		}

		#endregion

		#region properties

		public string Filename { get; }

		#endregion

		#region methods

		/// <inheritdoc />
		public override string ToString()
		{
			return Path.GetFileNameWithoutExtension( Filename );
		}

		#endregion
	}

	#endregion
}