namespace Zeiss.IMT.PiWeb.Volume.Tests.Performance
{
	using System.IO;
	using BenchmarkDotNet.Attributes;
	using BenchmarkDotNet.Running;
	using NUnit.Framework;

	[InProcess, IterationCount(3), WarmupCount(0)]
	[MemoryDiagnoser]
	public class DecompressVolumeTest
	{
		#region members

		private static readonly string SamplePath = Path.Combine( Paths.TestData, "volume.volx" );
		
		private CompressedVolume _SampleCompressedVolume;

		#endregion

		#region methods
		
		[GlobalSetup]
		public void Setup()
		{
			_SampleCompressedVolume = Volume.Load( File.OpenRead( SamplePath ) );
		}
	
		[Benchmark]
		public Volume DecompressVolume()
		{
			return _SampleCompressedVolume.Decompress();
		}

		[Test]
		public void RunDecompressVolumeTest()
		{
			BenchmarkRunner.Run<DecompressVolumeTest>();
		}

		#endregion
	}
}