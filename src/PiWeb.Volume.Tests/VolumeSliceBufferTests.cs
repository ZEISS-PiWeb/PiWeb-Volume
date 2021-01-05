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

	using NUnit.Framework;

	#endregion

	/// <summary>
	/// This class contains tests for <see cref="VolumeSliceBuffer"/>.
	/// </summary>
	[TestFixture]
	public class VolumeSliceBufferTests
	{
		#region methods

		[Test]
		public void Test_Default_Ctor()
		{
			var buffer = new VolumeSliceBuffer();
			Assert.That( buffer.Data, Is.Empty );
		}

		[Test]
		public void Test_Ctor()
		{
			const int size = 9991;
			var defininiton = new VolumeSliceDefinition( Direction.Y, 99 );

			var buffer = new VolumeSliceBuffer( defininiton, size );

			Assert.That( buffer.Data, Has.Exactly( size ).Items );
			Assert.That( buffer.Definition.Index, Is.EqualTo( 99 ) );
			Assert.That( buffer.Definition.Direction, Is.EqualTo( Direction.Y ) );
		}

		[Test]
		public void Test_Initialize()
		{
			const int size = 9991;
			var defininiton = new VolumeSliceDefinition( Direction.Y, 99 );

			var buffer = new VolumeSliceBuffer();
			Assert.That( buffer.Data, Is.Empty );

			buffer.Initialize( defininiton, size );

			Assert.That( buffer.Data, Has.Exactly( size ).Items );
			Assert.That( buffer.Definition.Index, Is.EqualTo( 99 ) );
			Assert.That( buffer.Definition.Direction, Is.EqualTo( Direction.Y ) );
		}

		[Test]
		public void Test_ToVolumeSlice()
		{
			const int size = 9991;
			var defininiton = new VolumeSliceDefinition( Direction.Y, 99 );

			var buffer = new VolumeSliceBuffer( defininiton, size );
			var slice = buffer.ToVolumeSlice();

			Assert.That( slice.Length, Is.EqualTo( size ) );
			Assert.That( slice.Index, Is.EqualTo( 99 ) );
			Assert.That( slice.Direction, Is.EqualTo( Direction.Y ) );
		}

		#endregion
	}
}