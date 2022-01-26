#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume
{
	/// <summary>
	/// Holds the properties that define a volume slice.
	/// </summary>
	public readonly struct VolumeSliceDefinition
	{
		#region constructors

		/// <summary>
		/// Creates a decription of a volume slice.
		/// </summary>
		/// <param name="direction"></param>
		/// <param name="index"></param>
		public VolumeSliceDefinition( Direction direction, ushort index )
		{
			Direction = direction;
			Index = index;
		}

		#endregion

		#region properties

		/// <summary>
		/// Direction
		/// </summary>
		public Direction Direction { get; }

		/// <summary>
		/// Slice index
		/// </summary>
		public ushort Index { get; }

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