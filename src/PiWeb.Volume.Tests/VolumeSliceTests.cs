#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Tests
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
			var slice = new VolumeSlice( new VolumeSliceDefinition( Direction.Y, 99 ), bytes );


			Assert.That( slice.Index, Is.EqualTo( 99 ) );
			Assert.That( slice.Direction, Is.EqualTo( Direction.Y ) );
			Assert.That( slice.Data, Is.EquivalentTo( bytes ) );
		}

		internal static byte[] GenerateData( int numberOfBytesPerIteration, int numberOfIterations = 1 )
		{
			return Enumerable.Range( 0, numberOfBytesPerIteration )
				.SelectMany( _ => Enumerable.Range( 0, numberOfIterations ) )
				.Select( i => (byte)i )
				.ToArray();
		}

		#endregion
	}
}