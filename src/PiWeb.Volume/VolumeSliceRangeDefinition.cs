#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume
{
	#region usings

	using System;

	#endregion

	/// <summary>
	/// Describes a continous range of slices in a specific direction.
	/// </summary>
	public readonly struct VolumeSliceRangeDefinition
	{
		#region constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeSliceRangeDefinition"/> struct.
		/// </summary>
		/// <param name="direction"></param>
		/// <param name="first">The first.</param>
		/// <param name="last">The last.</param>
		public VolumeSliceRangeDefinition( Direction direction, ushort first, ushort last )
		{
			First = Math.Min( first, last );
			Last = Math.Max( first, last );
			Direction = direction;
		}

		#endregion

		#region properties

		/// <summary>
		/// Gets the direction.
		/// </summary>
		/// <value>
		/// The direction.
		/// </value>
		public Direction Direction { get; }

		/// <summary>
		/// Gets the inclusive first slice.
		/// </summary>
		public ushort First { get; }

		/// <summary>
		/// Gets the inclusive last slice.
		/// </summary>
		public ushort Last { get; }

		/// <summary>
		/// Returns the number of slices defined by this definition.
		/// </summary>
		public int Length => Last - First;

		#endregion
	}
}