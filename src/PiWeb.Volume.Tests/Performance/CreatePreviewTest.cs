namespace Zeiss.PiWeb.Volume.Tests.Performance;

using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NUnit.Framework;

[InProcess, IterationCount(5), WarmupCount(1)]
[MemoryDiagnoser]
public class CreatePreviewTest
{
	#region members

	private static readonly string SamplePath = Path.Combine( Paths.TestData, "seatbelt_button.volx" );

	private CompressedVolume _SampleCompressedVolume = null!;
	private UncompressedVolume _SampleDecompressedVolume = null!;

	#endregion

	#region methods

	[GlobalSetup]
	public void Setup()
	{
		_SampleCompressedVolume = Volume.Load( new MemoryStream( File.ReadAllBytes( SamplePath ) ) );
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