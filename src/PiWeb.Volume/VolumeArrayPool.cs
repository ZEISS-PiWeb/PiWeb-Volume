#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume
{
	#region usings

	using System.Buffers;

	#endregion

	/// <summary>
	/// An array pool that will reuse arrays up to a size of 2^24 bytes which is enough to store
	/// a slice of 4096 * 4096 voxels.
	/// </summary>
	public static class VolumeArrayPool
	{
		#region constants

		private const int MaxWidth = 4096;
		private const int MaxHeight = 4096;

		#endregion

		#region members

		/// <summary>
		/// The static array pool used for volume slices.
		/// </summary>
		public static readonly ArrayPool<byte> Shared = ArrayPool<byte>.Create( MaxWidth * MaxHeight, 10 );

		#endregion
	}
}