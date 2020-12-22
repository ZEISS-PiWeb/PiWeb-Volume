#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.Tests.Performance
{
	#region usings

	using System.IO;
	using BenchmarkDotNet.Attributes;
	using BenchmarkDotNet.Running;
	using NUnit.Framework;

	#endregion

	[InProcess, IterationCount(3), WarmupCount(0)]
	[MemoryDiagnoser]
	public class LoadVolumeTest
	{
		#region members

		private static readonly string SamplePath = Path.Combine( Paths.TestData, "volume.volx" );

		#endregion

		#region methods
		
		[Benchmark]
		public Volume LoadVolume()
		{
			using var stream = File.OpenRead( SamplePath );
			return Volume.Load( stream );
		}

		[Test]
		public void RunLoadVolumeTest()
		{
			BenchmarkRunner.Run<LoadVolumeTest>();
		}

		#endregion
	}
}