#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume
{
	#region usings

	using System;

	#endregion

	/// <summary>
	/// A buffer class for slice data. This class has the purpose to reuse the slice data array
	/// for multiple slices fetches to avoid byte array allocations.
	/// </summary>
	public class VolumeSliceBuffer
	{
		#region members

		private int _Size;

		#endregion

		#region constructors

		public VolumeSliceBuffer()
		{
		}

		public VolumeSliceBuffer( VolumeSliceDefinition definition, int size )
		{
			Initialize( definition, size );
		}

		#endregion

		#region properties

		/// <summary>
		/// The slice definition that is associated with this buffer.
		/// </summary>
		public VolumeSliceDefinition Definition { get; private set; }

		/// <summary>
		/// The buffer that holds the slice data. Please note that the length of this buffer might not
		/// match the data length inside the buffer (buffer might be larger than necessary). Please use
		/// the property <see cref="_Size"/> to query the actual size of the data buffer. 
		/// </summary>
		public byte[] Data { get; private set; } = Array.Empty<byte>();

		#endregion

		#region methods

		/// <summary>
		/// Ensures that the buffer <see cref="Data"/> has at least the size <paramref name="size"/>. 
		/// </summary>
		/// <param name="definition">The slice definition that is associated with this buffer.</param>
		/// <param name="size">The minimum number of bytes that should be storable in <see cref="Data"/>.</param>
		public void Initialize( VolumeSliceDefinition definition, int size )
		{
			_Size = size;
			Definition = definition;

			if( Data.Length < size )
				Data = new byte[size];
		}

		/// <summary>
		/// Converts this buffer to a <see cref="VolumeSlice"/>.
		/// </summary>
		public VolumeSlice ToVolumeSlice()
		{
			return new VolumeSlice( Definition, _Size, Data );
		}

		#endregion
	}
}