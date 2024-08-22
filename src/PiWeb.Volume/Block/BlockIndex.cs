#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Block
{
	/// <summary>
	/// Stores the index of a data block.
	/// </summary>
	internal readonly record struct BlockIndex
	{
		#region constructors

		internal BlockIndex( ushort x, ushort y, ushort z )
		{
			X = x;
			Y = y;
			Z = z;
		}

		#endregion

		#region properties

		public ushort X { get; }
		public ushort Y { get; }
		public ushort Z { get; }

		#endregion
	}
}