namespace Zeiss.IMT.PiWeb.Volume.Tests.Performance
{
	using System.IO;
	using BenchmarkDotNet.Attributes;
	using BenchmarkDotNet.Running;
	using NUnit.Framework;

	[InProcess, IterationCount(3), WarmupCount(0)]
	[MemoryDiagnoser]
	public class CreatePreviewTest
	{
		#region members

		private static readonly string SamplePath = Path.Combine( Paths.TestData, "volume.volx" );
		
		private CompressedVolume _SampleCompressedVolume;
		private UncompressedVolume _SampleDecompressedVolume;

		#endregion

		#region methods
		
		[GlobalSetup]
		public void Setup()
		{
			_SampleCompressedVolume = Volume.Load( File.OpenRead( SamplePath ) );
			_SampleDecompressedVolume = _SampleCompressedVolume.Decompress();
		}

		[Benchmark]
		public Volume CreatePreviewFromDecompressed()
		{
			return _SampleDecompressedVolume.CreatePreview( 4 );
		}

		[Benchmark( Baseline = true )]
		public Volume CreatePreviewFromCompressed()
		{
			return _SampleCompressedVolume.CreatePreview( 4 );
		}

		[Test]
		public void RunCreatePreviewTest()
		{
			BenchmarkRunner.Run<CreatePreviewTest>();
		}

		#endregion
	}
}