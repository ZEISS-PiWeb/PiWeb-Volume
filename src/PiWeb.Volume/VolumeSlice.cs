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
	using System.Runtime.CompilerServices;

	#endregion

	/// <summary>
	/// A single layer of a discrete volume.
	/// </summary>
	public readonly struct VolumeSlice
	{
		#region constructors

		///  <summary>
		///  Initializes a new instance of the <see cref="VolumeSlice"/> class.
		///  </summary>
		///  <param name="definition">The definition.</param>
		///  <param name="data">The buffer.</param>
		internal VolumeSlice( VolumeSliceDefinition definition, byte[] data )
		{
			Direction = definition.Direction;
			Index = definition.Index;
			Data = data ?? throw new ArgumentNullException( nameof(data) );
		}

		#endregion

		#region properties

		/// <summary>
		/// The slice index.
		/// </summary>
		public ushort Index { get; }

		/// <summary>
		/// The slice direction.
		/// </summary>
		public Direction Direction { get; }

		/// <summary>
		/// The slice data.
		/// </summary>
		public byte[] Data { get; }

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