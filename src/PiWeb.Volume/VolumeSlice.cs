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
		#region members

		private readonly CompressedBytes _Compressed;

		#endregion

		#region constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeSlice"/> class.
		/// </summary>
		/// <param name="direction">The direction.</param>
		/// <param name="index">The index.</param>
		/// <param name="dataLength">The length of the actual data in the data array.</param>
		/// <param name="data">The data. Must not be null.</param>
		internal VolumeSlice( Direction direction, ushort index, int dataLength, byte[] data )
		{
			if( data == null ) throw new ArgumentNullException( nameof(data) );

			Direction = direction;
			Index = index;
			Length = dataLength;
			_Compressed = CompressedBytes.Create( new ArraySegment<byte>( data, 0, dataLength ) );
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeSlice"/> class.
		/// </summary>
		/// <param name="definition">The definition.</param>
		/// <param name="dataLength">The length of the actual data in the data array.</param>
		/// <param name="data">The data. Must not be null.</param>
		internal VolumeSlice( VolumeSliceDefinition definition, int dataLength, byte[] data )
		{
			if( data == null ) throw new ArgumentNullException( nameof(data) );

			Direction = definition.Direction;
			Index = definition.Index;
			Length = dataLength;
			_Compressed = CompressedBytes.Create( new ArraySegment<byte>( data, 0, dataLength ) );
		}

		#endregion

		#region properties

		/// <summary>
		/// Gets the direction.
		/// </summary>
		public Direction Direction { get; }

		/// <summary>
		/// Gets the index.
		/// </summary>
		public ushort Index { get; }

		/// <summary>
		/// Gets the length of the slice data in bytes.
		/// </summary>
		public int Length { get; }
		
		/// <summary>
		/// Gets the length of the compressed slice data in bytes.
		/// </summary>
		public int CompressedLength => _Compressed.CompressedLength;

		/// <summary>
		/// Returns the byte value at the specified index.
		/// </summary>
		public byte this[ int index ] => _Compressed[ index ];

		#endregion

		#region methods

		/// <summary>
		/// Copies the slice data to the specified target array.
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void CopyDataTo( byte[] destination )
		{
			if( destination.Length < Length )
				throw new ArgumentOutOfRangeException( $"Invalid buffer size. The buffer has to be at least {Length} bytes." );

			_Compressed.DecompressTo( destination );
		}

		/// <summary>
		/// Copies a range of the slice data to the specified target array.
		/// </summary>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void CopyDataTo( byte[] destination, int destinationIndex, int startIndex, int count )
		{
			if( destination.Length < destinationIndex + count )
				throw new ArgumentOutOfRangeException( $"Invalid buffer size. The buffer has to be at least {Length} bytes." );

			_Compressed.DecompressTo( destination, destinationIndex, startIndex, count );
		}
		
		// TODO: Aufrufer dieser Methode sollten die untere Überladung nutzen, wenn möglich

		/// <summary>
		/// Decompresses the stored data into a temporary buffer that must be disposed after usage.
		/// Please note that the buffer is invalid after disposing the <see cref="VolumeSliceData"/>
		/// and might be reused.
		/// </summary>
		public VolumeSliceData Decompress()
		{
			var buffer = VolumeArrayPool.Shared.Rent( Length );
			_Compressed.DecompressTo( buffer );

			return new VolumeSliceData( buffer, Length, true );
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"Slice {Index}, direction {Direction}, {Length} data bytes, {_Compressed.CompressedLength} compressed bytes";
		}

		#endregion
	}
}