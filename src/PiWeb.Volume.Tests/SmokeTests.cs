#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.Tests
{
	#region usings

	using System.IO;
	using System.Linq;
	using NUnit.Framework;

	#endregion

	/// <summary>
	/// Smoke tests that perform a high level test for basic functionality. These tests basically make sure,
	/// that the code does not crash.
	/// </summary>
	[TestFixture]
	public class SmokeTests
	{
		#region members

		private static readonly string SamplesPath = Path.Combine( Path.GetDirectoryName( typeof( SampleFile ).Assembly.Location )!, "..\\..\\testdata" );

		public static SampleFile[] AllSamples { get; } = Directory
			.GetFiles( SamplesPath, "*.volx", SearchOption.AllDirectories )
			.Select( f => new SampleFile( f ) )
			.ToArray();

		#endregion

		#region methods

		[Test, TestCaseSource( nameof(AllSamples) )]
		public void LoadSamplesTest( SampleFile file )
		{
			using var stream = File.OpenRead( file.Filename );
			var logger = new ConsoleLogger();
			
			var compressedVolume = Volume.Load( stream, logger );
			
			Assert.That( compressedVolume, Is.Not.Null );
			Assert.That( compressedVolume.Metadata, Is.Not.Null );
		}

		[Test, TestCaseSource( nameof(AllSamples) )]
		public void DecompressSamplesTest( SampleFile file )
		{
			using var stream = File.OpenRead( file.Filename );
			var logger = new ConsoleLogger();
			
			var compressedVolume = Volume.Load( stream, logger );
			var decompressedVolume = compressedVolume.Decompress( logger: logger );
			
			Assert.That( decompressedVolume, Is.Not.Null );
			Assert.That( decompressedVolume.Metadata, Is.Not.Null );
		}

		[Test, TestCaseSource( nameof(AllSamples) )]
		public void CreatePreviewForCompressedVolumeTest( SampleFile file )
		{
			using var stream = File.OpenRead( file.Filename );
			var logger = new ConsoleLogger();
			
			var compressedVolume = Volume.Load( stream, logger );
			var preview = compressedVolume.CreatePreview( 4, logger: logger );
			
			Assert.That( preview, Is.Not.Null );
			Assert.That( preview.Metadata, Is.Not.Null );
		}

		[Test, TestCaseSource( nameof(AllSamples) )]
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

		[Test, TestCaseSource( nameof(AllSamples) )]
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