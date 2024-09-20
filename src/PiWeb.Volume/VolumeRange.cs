#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2024                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume;

/// <summary>
/// Describes a range in voxel coordinates.
/// </summary>
/// <param name="Start">Inclusive start</param>
/// <param name="End">Inclusive end</param>
public readonly record struct VolumeRange( ushort Start, ushort End )
{
	#region properties

	/// <summary>
	/// The size of the range.
	/// </summary>
	public ushort Size => (ushort)( End - Start + 1 );

	#endregion

	#region methods

	/// <summary>
	/// Determines, whether this range contains <paramref name="value"/>.
	/// </summary>
	public bool Contains( ushort value )
	{
		return value >= Start && value <= End;
	}

	/// <summary>
	/// Determines, whether there's an overlap between this range and <paramref name="other"/>.
	/// </summary>
	public bool Intersects( VolumeRange other )
	{
		return Start <= other.End && other.Start <= End;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"[{Start}..{End}]";
	}

	#endregion
}