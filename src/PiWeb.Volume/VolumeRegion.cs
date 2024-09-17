#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2024                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume;

/// <summary>
/// Defines a region of interest inside a slice. The ranges <paramref name="U"/> and <paramref name="V"/> are projected
/// to the slice direction.
/// </summary>
public readonly record struct VolumeRegion( VolumeRange U, VolumeRange V );
