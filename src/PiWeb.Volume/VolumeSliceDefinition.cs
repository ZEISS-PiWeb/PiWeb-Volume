#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume
{
	/// <summary>
	/// Holds the properties that define a volume slice.
	/// </summary>
	public readonly record struct VolumeSliceDefinition
	{
		#region constructors

		/// <summary>
		/// Creates a decription of a volume slice.
		/// </summary>
		/// <param name="direction">The projection direction of the slice.</param>
		/// <param name="index">The index of the slice in the volume.</param>
		/// <param name="regionOfInterest">The region of interest inside the slice.</param>
		public VolumeSliceDefinition( Direction direction, ushort index, VolumeRegion? regionOfInterest = null )
		{
			Direction = direction;
			Index = index;
			RegionOfInterest = regionOfInterest;
		}


		#endregion

		#region properties

		/// <summary>
		/// Direction.
		/// </summary>
		public Direction Direction { get; }

		/// <summary>
		/// Slice index.
		/// </summary>
		public ushort Index { get; }

		/// <summary>
		/// The region of interest in this slice.
		/// </summary>
		public VolumeRegion? RegionOfInterest { get; }

		#endregion

		#region methods

		/// <inheritdoc />
		public override string ToString()
		{
			return $"Slice {Index}, direction {Direction}";
		}

		#endregion
	}
}