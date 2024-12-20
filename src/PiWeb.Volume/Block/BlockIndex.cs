#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Block;

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

	public readonly ushort X;
	public readonly ushort Y;
	public readonly ushort Z;

	#endregion
}