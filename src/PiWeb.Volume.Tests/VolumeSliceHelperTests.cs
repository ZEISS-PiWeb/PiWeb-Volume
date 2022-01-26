#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2021                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Tests
{
	#region usings

	using NUnit.Framework;

	#endregion

	/// <summary>
	/// This class contains tests for <see cref="VolumeSliceHelper"/>.
	/// </summary>
	[TestFixture]
	public class VolumeSliceHelperTests
	{
		#region methods

		[Test]
		public void Test_SliceBufferCreation()
		{
			var expectedBuffer = new byte[10 * 20];
			var sliceBuffers = VolumeSliceHelper.CreateSliceBuffer( 10, 20, 5 );

			Assert.That( sliceBuffers.Length, Is.EqualTo( 5 ) );
			for( var i = 0; i < 5; i++ )
			{
				Assert.That( sliceBuffers[ i ], Is.EquivalentTo( expectedBuffer ) );
			}
		}

		#endregion
	}
}