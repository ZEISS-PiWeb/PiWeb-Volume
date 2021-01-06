#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume
{
	#region usings

	using System.Buffers;

	#endregion

	public static class VolumeArrayPool
	{
		#region constants

		private const int MaxWidth = 4096;
		private const int MaxHeight = 4096;

		#endregion

		#region members

		public static ArrayPool<byte> Shared = ArrayPool<byte>.Create( MaxWidth * MaxHeight, 10 );

		#endregion
	}
}