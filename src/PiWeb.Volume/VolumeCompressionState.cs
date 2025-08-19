#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion


namespace Zeiss.PiWeb.Volume;

/// <summary>
/// Describes the compression state of a the volume in a specific direction
/// </summary>
public enum VolumeCompressionState
{
	/// <summary>
	/// The volume is currently decompressed, which allows instant access to slices
	/// </summary>
	Decompressed,

	/// <summary>
	/// The volume is compressed in the requested direction, which allows a relatively fast access to slices
	/// </summary>
	CompressedInDirection,

	/// <summary>
	/// The volume is compressed in a different direction, which means that a slice access will perform a full scan
	/// </summary>
	Compressed,
}