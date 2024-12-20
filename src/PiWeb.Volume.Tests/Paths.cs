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

	using System.IO;

	#endregion

	/// <summary>
	/// Global class with a bunch of paths to specific files and folders.
	/// </summary>
	public static class Paths
	{
		#region members

		public static readonly string TestData = Path.Combine( Path.GetDirectoryName( typeof( Paths ).Assembly.Location )!, "..\\..\\testdata" );

		#endregion
	}
}