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

	using System;
	using System.Threading.Tasks;
	using NUnit.Framework;

	#endregion

	/// <summary>
	/// This class contains tests for <see cref="CompressedBytes"/>.
	/// </summary>
	[TestFixture]
	public class CompressedBytesTests
	{
		#region methods

		[Test]
		public void Test_Creation()
		{
			var data = VolumeSliceTests.GenerateData( 255, 4 );
			var decompressedBytes = new byte[data.Length];

			var compressed = CompressedBytes.Create( data );
			compressed.DecompressTo( decompressedBytes );

			Assert.That( compressed.Length, Is.EqualTo( data.Length ) );
			Assert.That( compressed.CompressedLength, Is.AtMost( data.Length ) );

			Assert.That( decompressedBytes, Is.EquivalentTo( data ) );
		}

		[Test]
		public void Test_Indexer()
		{
			var data = VolumeSliceTests.GenerateData( 255, 10 );
			var compressed = CompressedBytes.Create( data );

			Assert.That( compressed.Length, Is.EqualTo( data.Length ) );
			for( var i = 0; i < data.Length; i++ )
			{
				Assert.That( compressed[ i ], Is.EqualTo( data[ i ] ) );
			}
		}

		[Test]
		[TestCase( 1, 1 )]
		[TestCase( 1, 10 )]
		[TestCase( 1, 100 )]
		[TestCase( 10, 1 )]
		[TestCase( 10, 10 )]
		[TestCase( 10, 100 )]
		[TestCase( 100, 1 )]
		[TestCase( 100, 10 )]
		[TestCase( 100, 100 )]
		[TestCase( 255, 1 )]
		[TestCase( 255, 10 )]
		[TestCase( 255, 100 )]
		public void Test_Decompression( int numberOfBytes, int numberOfIterations )
		{
			var data = VolumeSliceTests.GenerateData( numberOfBytes, numberOfIterations );
			var compressed = CompressedBytes.Create( data );
			var decompressed = new byte[data.Length];
			compressed.DecompressTo( decompressed );

			Assert.That( decompressed, Is.EquivalentTo( data ) );
		}

		[TestCase( 0, 0, 1 )]
		[TestCase( 1, 0, 1 )]
		[TestCase( 100, 0, 1 )]
		[TestCase( 0, 0, 10 )]
		[TestCase( 1, 0, 10 )]
		[TestCase( 100, 0, 10 )]
		[TestCase( 0, 10, 10 )]
		[TestCase( 1, 10, 10 )]
		[TestCase( 100, 10, 10 )]
		[TestCase( 0, 1, 1 )]
		[TestCase( 0, 1, 1 )]
		[TestCase( 0, 1, 1 )]
		public void Test_ConstrainedDecompression( int destinationIndex, int srcIndex, int count )
		{
			var data = VolumeSliceTests.GenerateData( 255, 5 );
			var compressed = CompressedBytes.Create( data );
			
			var decompressed = new byte[data.Length];
			compressed.DecompressTo( decompressed, destinationIndex, srcIndex, count );

			Assert.That( decompressed[ new Range( destinationIndex, destinationIndex + count ) ], Is.EquivalentTo( data[ new Range( srcIndex, srcIndex + count ) ] ) );
		}

		[Test]
		[TestCase( 1, 1 )]
		[TestCase( 1, 10 )]
		[TestCase( 1, 100 )]
		[TestCase( 10, 1 )]
		[TestCase( 10, 10 )]
		[TestCase( 10, 100 )]
		[TestCase( 100, 1 )]
		[TestCase( 100, 10 )]
		[TestCase( 100, 100 )]
		[TestCase( 255, 1 )]
		[TestCase( 255, 10 )]
		[TestCase( 255, 100 )]
		public void Test_NeverIncreasesDataSize( int numberOfBytes, int numberOfIterations )
		{
			var data = VolumeSliceTests.GenerateData( numberOfBytes, numberOfIterations );
			var compressed = CompressedBytes.Create( data );

			Assert.That( compressed.Length, Is.EqualTo( data.Length ) );
			Assert.That( compressed.CompressedLength, Is.AtMost( data.Length ) );
		}

		[Test]
		public void Test_Creation_Works_In_Parallel()
		{
			const int numberOfRuns = 1000;
			var data = VolumeSliceTests.GenerateData( 255, 1000 );

			Parallel.For( 0, numberOfRuns, _ =>
			{
				var compressed = CompressedBytes.Create( data );
				Assert.That( compressed.Length, Is.EqualTo( data.Length ) );
			} );
		}
		
		#endregion
	}
}