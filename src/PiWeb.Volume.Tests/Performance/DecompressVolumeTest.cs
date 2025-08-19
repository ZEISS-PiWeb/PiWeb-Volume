namespace Zeiss.PiWeb.Volume.Tests.Performance;

using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NUnit.Framework;

[InProcess, IterationCount(5), WarmupCount(1)]
[MemoryDiagnoser]
public class DecompressVolumeTest
{
	#region members

	private static readonly string SamplePath = Path.Combine( Paths.TestData, "seatbelt_button.volx" );

	private CompressedVolume _SampleCompressedVolume;

	#endregion

	#region methods

	[GlobalSetup]
	public void Setup()
	{
		_SampleCompressedVolume = Volume.Load( new MemoryStream( File.ReadAllBytes( SamplePath ) ) );
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