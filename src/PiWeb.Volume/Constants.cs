#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume
{
	internal static class Constants
	{
		#region constants

		internal const ushort MinimumEncodingSize = 64;

		internal const ushort EncodingBlockSize = 4;

		internal const ushort RangeNumberLimitForEfficientScan = 16;

		internal const ushort SliceNumberLimitForEfficientScan = 256;

		#endregion
	}
}