#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Tests
{
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

		public static SampleFile[] AllSamples { get; } = Directory
			.GetFiles( Paths.TestData, "*.volx", SearchOption.AllDirectories )
			.Select( f => new SampleFile( f ) )
			.ToArray();

		#endregion

		#region methods

		[Test, TestCaseSource( nameof( AllSamples ) )]
		public void LoadSamplesTest( SampleFile file )
		{
			using var stream = File.OpenRead( file.Filename );
			var logger = new ConsoleLogger();

			var compressedVolume = Volume.Load( stream, logger );

			Assert.That( compressedVolume, Is.Not.Null );
			Assert.That( compressedVolume.Metadata, Is.Not.Null );
		}

		[Test, TestCaseSource( nameof( AllSamples ) )]
		public void DecompressSamplesTest( SampleFile file )
		{
			using var stream = File.OpenRead( file.Filename );
			var logger = new ConsoleLogger();

			var compressedVolume = Volume.Load( stream, logger );
			var decompressedVolume = compressedVolume.Decompress( logger: logger );

			Assert.That( decompressedVolume, Is.Not.Null );
			Assert.That( decompressedVolume.Metadata, Is.Not.Null );
		}

		[Test, TestCaseSource( nameof( AllSamples ) )]
		public void CreatePreviewForCompressedVolumeTest( SampleFile file )
		{
			using var stream = File.OpenRead( file.Filename );
			var logger = new ConsoleLogger();

			var compressedVolume = Volume.Load( stream, logger );
			var preview = compressedVolume.CreatePreview( 4, logger: logger );

			Assert.That( preview, Is.Not.Null );
			Assert.That( preview.Metadata, Is.Not.Null );
		}

		[Test, TestCaseSource( nameof( AllSamples ) )]
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

		[Test, TestCaseSource( nameof( AllSamples ) )]
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
		public void CompareHighNoiseVolume( [Values( 25, 50, 75, 85, 90, 95, 100 )] int quality )
		{
			var original = VolumeTestHelper.CreateHighNoiseVolume();
			var compressed = original.Compress( new VolumeCompressionOptions( BlockVolume.EncoderID, "gray8", new Dictionary<string, string>
			{
				{
					"quality", quality.ToString()
				}
			} ) );

			var noise = VolumeTestHelper.CalculateNoise( original, compressed );

			Assert.That( noise, Is.Not.Null );

			Console.WriteLine( @$"Quality {quality} peak: {noise.Value.Peak}" );
			Console.WriteLine( @$"Quality {quality} average: {noise.Value.Average}" );
			Console.WriteLine( @$"Quality {quality} mean: {noise.Value.Mean}" );
			Console.WriteLine( @$"Quality {quality} q95: {noise.Value.Q95}" );
		}

		[Test, Explicit]
		public void CompareLowNoiseVolume( [Values( 25, 50, 75, 85, 90, 95, 100 )] int quality )
		{
			var original = VolumeTestHelper.CreateLowNoiseVolume();
			var compressed = original.Compress( new VolumeCompressionOptions( BlockVolume.EncoderID, "gray8", new Dictionary<string, string>
			{
				{
					"quality", quality.ToString()
				}
			} ) );

			var noise = VolumeTestHelper.CalculateNoise( original, compressed );

			Assert.That( noise, Is.Not.Null );

			Console.WriteLine( @$"Quality {quality} peak: {noise.Value.Peak}" );
			Console.WriteLine( @$"Quality {quality} average: {noise.Value.Average}" );
			Console.WriteLine( @$"Quality {quality} mean: {noise.Value.Mean}" );
			Console.WriteLine( @$"Quality {quality} q95: {noise.Value.Q95}" );
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
}