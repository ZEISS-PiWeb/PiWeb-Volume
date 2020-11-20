#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.Block
{
	/// <summary>
	/// Stores the index of a data block.
	/// </summary>
	internal readonly struct BlockIndex
	{
		public readonly ushort X;
		public readonly ushort Y;
		public readonly ushort Z;

		internal BlockIndex( ushort x, ushort y, ushort z )
		{
			X = x;
			Y = y;
			Z = z;
		}
	}
}