﻿#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume
{
	#region usings

	using System.Linq;

	#endregion

	/// <summary>
	/// Helper class for <see cref="VolumeSlice"/>.
	/// </summary>
	public static class VolumeSliceHelper
	{
		#region methods

		/// <summary>
		/// Creates a new slice buffer with the specified dimensions.
		/// </summary>
		public static byte[][] CreateSliceBuffer( ushort sizeX, ushort sizeY, ushort sizeZ )
		{
			var previewSliceSize = sizeX * sizeY;
			return Enumerable
				.Range( 0, sizeZ )
				.Select( i => new byte[ previewSliceSize ] )
				.ToArray();
		}

		#endregion
	}
}