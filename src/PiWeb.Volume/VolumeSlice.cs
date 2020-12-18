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
	using System.Buffers;
	using System.IO;
	using System.Threading.Tasks;

	#endregion

	/// <summary>
	/// A single layer of a discrete volume.
	/// </summary>
	public sealed class VolumeSlice
	{
		#region constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeSlice"/> class.
		/// </summary>
		/// <param name="direction">The direction.</param>
		/// <param name="index">The index.</param>
		/// <param name="data">The data.</param>
		internal VolumeSlice( Direction direction, ushort index, byte[] data )
		{
			Direction = direction;
			Index = index;
			Data = data;
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

		/// <summary>
		/// Extracts the specified direction.
		/// </summary>
		/// <param name="direction">The direction.</param>
		/// <param name="index">The index.</param>
		/// <param name="volumeMetadata">The volume metadata.</param>
		/// <param name="data">The data.</param>
		/// <returns></returns>
		/// <exception cref="System.ArgumentOutOfRangeException">direction - null</exception>
		internal static VolumeSlice Extract( Direction direction, ushort index, VolumeMetadata volumeMetadata, byte[][] data )
		{
			return direction switch
			{
				Direction.X => ReadSliceX( direction, index, data, volumeMetadata ),
				Direction.Y => ReadSliceY( direction, index, data, volumeMetadata ),
				Direction.Z => ReadSliceZ( direction, index, data, volumeMetadata ),
				_ => throw new ArgumentOutOfRangeException( nameof(direction), direction, null )
			};
		}

		/// <summary>
		/// Extracts the specified direction.
		/// </summary>
		/// <param name="direction">The direction.</param>
		/// <param name="index">The index.</param>
		/// <param name="volumeMetadata">The volume metadata.</param>
		/// <param name="stream">The data stream</param>
		/// <returns></returns>
		/// <exception cref="System.ArgumentOutOfRangeException">direction - null</exception>
		internal static VolumeSlice Extract( Direction direction, ushort index, VolumeMetadata volumeMetadata, Stream stream )
		{
			return direction switch
			{
				Direction.X => ReadSliceX( direction, index, stream, volumeMetadata ),
				Direction.Y => ReadSliceY( direction, index, stream, volumeMetadata ),
				Direction.Z => ReadSliceZ( direction, index, stream, volumeMetadata ),
				_ => throw new ArgumentOutOfRangeException( nameof(direction), direction, null )
			};
		}

		private static VolumeSlice ReadSliceX( Direction direction, ushort index, Stream stream, VolumeMetadata volumeMetadata )
		{
			var sx = volumeMetadata.SizeX;
			var sy = volumeMetadata.SizeY;
			var sz = volumeMetadata.SizeZ;

			var bufferSize = sx * sy;
			var buffer = ArrayPool<byte>.Shared.Rent( bufferSize );
			var result = new byte[sy * sz];

			for( var z = 0; z < sz; z++ )
			{
				stream.Read( buffer, 0, bufferSize );

				for( var y = 0; y < sy; y++ )
				{
					result[ z * sy + y ] = buffer[ y * sx + index ];
				}
			}

			ArrayPool<byte>.Shared.Return( buffer );

			return new VolumeSlice( direction, index, result );
		}

		private static VolumeSlice ReadSliceX( Direction direction, ushort index, byte[][] data, VolumeMetadata volumeMetadata )
		{
			var sx = volumeMetadata.SizeX;
			var sy = volumeMetadata.SizeY;
			var sz = volumeMetadata.SizeZ;

			var bufferSize = sy * sz;
			var result = new byte[bufferSize];

			Parallel.For( 0, sz, z =>
			{
				for( var y = 0; y < sy; y++ )
				{
					result[ z * sy + y ] = data[ z ][ y * sx + index ];
				}
			} );

			return new VolumeSlice( direction, index, result );
		}

		private static VolumeSlice ReadSliceY( Direction direction, ushort index, Stream stream, VolumeMetadata volumeMetadata )
		{
			var sx = volumeMetadata.SizeX;
			var sy = volumeMetadata.SizeY;
			var sz = volumeMetadata.SizeZ;

			var result = new byte[sx * sz];
			var bufferSize = sx * sy;
			var buffer = ArrayPool<byte>.Shared.Rent( bufferSize );

			for( var z = 0; z < sz; z++ )
			{
				stream.Read( buffer, 0, bufferSize );
				Array.Copy( buffer, index * sx, result, z * sx, sx );
			}

			ArrayPool<byte>.Shared.Return( buffer );

			return new VolumeSlice( direction, index, result );
		}

		private static VolumeSlice ReadSliceY( Direction direction, ushort index, byte[][] data, VolumeMetadata volumeMetadata )
		{
			var sx = volumeMetadata.SizeX;
			var sz = volumeMetadata.SizeZ;

			var bufferSize = sx * sz;
			var result = new byte[bufferSize];

			Parallel.For( 0, sz, z => { Array.Copy( data[ z ], index * sx, result, z * sx, sx ); } );

			return new VolumeSlice( direction, index, result );
		}

		private static VolumeSlice ReadSliceZ( Direction direction, ushort index, Stream stream, VolumeMetadata volumeMetadata )
		{
			var sx = volumeMetadata.SizeX;
			var sy = volumeMetadata.SizeY;

			var bufferSize = sx * sy;
			var result = new byte[bufferSize];

			stream.Seek( ( long ) index * sx * sy, SeekOrigin.Begin );
			stream.Read( result, 0, bufferSize );

			return new VolumeSlice( direction, index, result );
		}

		private static VolumeSlice ReadSliceZ( Direction direction, ushort index, byte[][] data, VolumeMetadata volumeMetadata )
		{
			var sx = volumeMetadata.SizeX;
			var sy = volumeMetadata.SizeY;

			var bufferSize = sx * sy;

			var result = new byte[bufferSize];
			Array.Copy( data[ index ], 0, result, 0, bufferSize );

			return new VolumeSlice( direction, index, result );
		}

		#endregion
	}
}