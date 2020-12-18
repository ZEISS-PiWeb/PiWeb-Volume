#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume
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

		public static VolumeSlice[] CreateSliceData( ushort sizeX, ushort sizeY, ushort sizeZ )
		{
			var previewSliceSize = sizeX * sizeY;
			return Enumerable
				.Range( 0, sizeZ )
				.Select( i => new VolumeSlice( Direction.Z, ( ushort ) i, new byte[previewSliceSize] ) )
				.ToArray();
		}

		#endregion
	}
}