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
	/// A single layer of a discrete volume.
	/// </summary>
	public readonly struct VolumeSlice
	{
		#region constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeSlice"/> class.
		/// </summary>
		/// <param name="direction">The direction.</param>
		/// <param name="index">The index.</param>
		/// <param name="data">The data. Must not be null.</param>
		internal VolumeSlice( Direction direction, ushort index, byte[] data )
		{
			Direction = direction;
			Index = index;
			Data = data ?? throw new ArgumentNullException( nameof(data) );
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
		/// Gets the index.
		/// </summary>
		/// <value>
		/// The index.
		/// </value>
		public ushort Index { get; }

		/// <summary>
		/// Gets the data.
		/// </summary>
		/// <value>
		/// The data.
		/// </value>
		public byte[] Data { get; }

		#endregion

		#region methods

		/// <inheritdoc />
		public override string ToString()
		{
			return $"Slice {Index}, direction {Direction}, {Data.Length} data bytes";
		}

		#endregion
	}
}