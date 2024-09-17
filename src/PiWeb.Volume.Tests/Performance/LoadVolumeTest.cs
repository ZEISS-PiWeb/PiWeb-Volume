#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Tests.Performance
{
	#region usings

	using System.IO;
	using BenchmarkDotNet.Attributes;
	using BenchmarkDotNet.Running;
	using NUnit.Framework;

	#endregion

	[InProcess, IterationCount(5), WarmupCount(1)]
	[MemoryDiagnoser]
	public class LoadVolumeTest
	{
		#region members

		private static readonly string SamplePath = Path.Combine( Paths.TestData, "testcube_singledirection.volx" );
		private byte[] _VolumeData;

		#endregion

		#region methods

		[GlobalSetup]
		public void Setup()
		{
			_VolumeData = File.ReadAllBytes( SamplePath );
		}

		[Benchmark]
		public Volume LoadVolume()
		{
			return Volume.Load( new MemoryStream( _VolumeData ) );
		}

		[Test]
		public void RunLoadVolumeTest()
		{
			BenchmarkRunner.Run<LoadVolumeTest>();
		}

		#endregion
	}
}