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
	using System.Linq;
	using NUnit.Framework;

	#endregion

	/// <summary>
	/// This class contains tests for <see cref="VolumeSlice"/>.
	/// </summary>
	[TestFixture]
	public class VolumeSliceTests
	{
		#region methods

		[Test]
		public void Test_Ctor()
		{
			const int numberOfBytes = 250;

			var bytes = GenerateData( numberOfBytes );
			var slice = new VolumeSlice( Direction.Y, 99, numberOfBytes, bytes );
			using var decompressed = slice.Decompress();

			Assert.That( slice.Index, Is.EqualTo( 99 ) );
			Assert.That( slice.Length, Is.EqualTo( numberOfBytes ) );
			Assert.That( decompressed.Data, Is.EquivalentTo( bytes ) );
		}

		[Test]
		public void Test_Ctor_SliceDefinition()
		{
			var bytes = GenerateData( 250 );
			var definition = new VolumeSliceDefinition( Direction.Y, 99 );

			var slice = new VolumeSlice( definition, bytes.Length, bytes );
			using var decompressed = slice.Decompress();

			Assert.That( slice.Index, Is.EqualTo( 99 ) );
			Assert.That( slice.Length, Is.EqualTo( bytes.Length ) );
			Assert.That( decompressed.Data, Is.EquivalentTo( bytes ) );
		}

		[Test]
		[TestCase( 1, 1 )]
		[TestCase( 10, 1 )]
		[TestCase( 10, 100 )]
		[TestCase( 255, 100 )]
		[TestCase( 10, 1000 )]
		public void Test_Ctor_Decompression( int count, int iterations )
		{
			var bytes = GenerateData( count, iterations );
			var definition = new VolumeSliceDefinition( Direction.Y, 99 );

			var slice = new VolumeSlice( definition, bytes.Length, bytes );
			using var decompressed = slice.Decompress();

			Assert.That( decompressed.Data, Is.EquivalentTo( bytes ) );
		}

		[Test]
		public void Test_CopyDataTo()
		{
			var bytes = GenerateData( 255, 50 );
			var definition = new VolumeSliceDefinition( Direction.Y, 99 );

			var slice = new VolumeSlice( definition, bytes.Length, bytes );

			var buffer = new byte[bytes.Length];
			slice.CopyDataTo( buffer );
			Assert.That( buffer, Is.EquivalentTo( bytes ) );
		}

		[Test]
		[TestCase( 0, 1 )]
		[TestCase( 0, 100 )]
		[TestCase( 100, 1 )]
		[TestCase( 100, 10 )]
		public void Test_CopyDataTo_Restricted( int start, int length )
		{
			var bytes = GenerateData( 255, 50 );
			var definition = new VolumeSliceDefinition( Direction.Y, 99 );

			var slice = new VolumeSlice( definition, bytes.Length, bytes );

			var buffer = new byte[length];
			slice.CopyDataTo( buffer, 0, start, length );
			Assert.That( buffer, Is.EquivalentTo( bytes[ new Range( start, start + length ) ] ) );
		}

		internal static byte[] GenerateData( int numberOfBytesPerIteration, int numberOfIterations = 1 )
		{
			return Enumerable.Range( 0, numberOfBytesPerIteration )
				.SelectMany( _ => Enumerable.Range( 0, numberOfIterations ) )
				.Select( i => ( byte ) i )
				.ToArray();
		}

		#endregion
	}
}