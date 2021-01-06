namespace Zeiss.IMT.PiWeb.Volume
{
	#region usings

	using System;
	using System.IO;
	using System.Runtime.CompilerServices;
	using K4os.Compression.LZ4;

	#endregion

	/// <summary>
	/// This class is responsible for storing a byte array very efficently optimized for memory usage.
	/// This is achieved by slicing the data into smaller blocks (for random access) and storing each
	/// slice as a LZ4 compressed byte array.  
	/// </summary>
	public readonly struct CompressedBytes
	{
		#region constants

		private const int BucketSize = 64;

		#endregion

		#region members

		[ThreadStatic] private static byte[] _Buffer;

		private readonly byte[] _Data;
		private readonly int[] _Buckets;
		private readonly bool _IsCompressed;

		#endregion

		#region constructors

		private CompressedBytes( byte[] data, int[] buckets, int length, bool isCompressed )
		{
			_Data = data;
			_Buckets = buckets;
			_IsCompressed = isCompressed;
			Length = length;
		}

		#endregion

		#region properties

		private static byte[] Buffer => _Buffer ??= new byte[2 * BucketSize];

		/// <summary>
		/// Returns the uncompressed length. 
		/// </summary>
		public int Length { get; }

		/// <summary>
		/// Returns the compressed length. 
		/// </summary>
		public int CompressedLength => _Data.Length;

		/// <summary>
		/// Returns the byte value at the specified index.
		/// </summary>
		/// <param name="index">The index into the uncompressed bytze array.</param>
		public byte this[ int index ]
		{
			get
			{
				if( _IsCompressed )
				{
					DecompressBucket( index / BucketSize, Buffer, 0, index % BucketSize, 1 );
					return Buffer[ 0 ];
				}

				return _Data[ index ];
			}
		}

		#endregion

		#region methods

		/// <summary>
		/// Creates a new compressed bytes object for the specified <paramref name="data"/> object.
		/// </summary>
		/// <param name="data">The data to compress</param>
		public static CompressedBytes Create( ArraySegment<byte> data )
		{
			var stream = new MemoryStream();

			var numberOfBuckets = ( data.Count + BucketSize ) / BucketSize;
			var buckets = new int[numberOfBuckets];

			var sourceIndex = 0;
			var destinationIndex = 0;
			var bucketIndex = 0;

			while( sourceIndex < data.Count )
			{
				buckets[ bucketIndex++ ] = destinationIndex;

				var sourceCount = Math.Min( BucketSize, data.Count - sourceIndex );

				var source = data.Slice( sourceIndex, sourceCount );
				var destination = new ArraySegment<byte>( Buffer );

				var bytesCompressed = CompressBytes( source, destination );
				stream.Write( Buffer, 0, bytesCompressed );

				sourceIndex += BucketSize;
				destinationIndex += bytesCompressed;
			}
			if( stream.Length < data.Count )
				return new CompressedBytes( stream.ToArray(), buckets, data.Count, true );

			return new CompressedBytes( data.ToArray(), Array.Empty<int>(), data.Count, false );
		}

		/// <summary>
		/// Decompresses all bytes into <paramref name="destination"/> 
		/// </summary>
		/// <param name="destination">The destination buffer.</param>
		public void DecompressTo( byte[] destination )
		{
			DecompressTo( destination, 0, 0, Length );
		}

		/// <summary>
		/// Decompresses a range of bytes into <paramref name="destination"/>
		/// </summary>
		/// <param name="destination">The destination buffer.</param>
		/// <param name="destinationIndex">The index into destination where the first byte should be written to.</param>
		/// <param name="srcIndex">The index into the source where the first byte should be read.</param>
		/// <param name="count">The number of bytes to read.</param>
		public void DecompressTo( byte[] destination, int destinationIndex, int srcIndex, int count )
		{
			if( !_IsCompressed )
				Array.Copy( _Data, srcIndex, destination, destinationIndex, count );
			else
				DecompressBucketsTo( destination, destinationIndex, srcIndex, count );
		}

		private void DecompressBucketsTo( byte[] destination, int destinationIndex, int srcIndex, int count )
		{
			var firstBucketIndex = srcIndex / BucketSize;
			var lastBucketIndex = ( srcIndex + count ) / BucketSize;

			for( var bucketIndex = firstBucketIndex; bucketIndex <= lastBucketIndex && count > 0; bucketIndex++ )
			{
				var srcIndexForBucket = srcIndex % BucketSize;
				var countForBucket = Math.Min( count, BucketSize - srcIndexForBucket );

				DecompressBucket( bucketIndex, destination, destinationIndex, srcIndexForBucket, countForBucket );

				count -= countForBucket;
				srcIndex += countForBucket;
				destinationIndex += countForBucket;
			}
		}

		private void DecompressBucket( int bucketIndex, byte[] destination, int destinationIndex, int srcIndex, int count )
		{
			var (sourceOffset, sourceLength) = GetCompressedByteCountInBucket( bucketIndex );
			LZ4Codec.Decode( _Data, sourceOffset, sourceLength, Buffer, 0, Buffer.Length );
			
			Array.Copy( Buffer, srcIndex, destination, destinationIndex, count );
		}

		private (int start, int length) GetCompressedByteCountInBucket( int bucketIndex )
		{
			var currentBucketStartIndex = _Buckets[ bucketIndex ];
			if( bucketIndex < _Buckets.Length - 1 )
				return (currentBucketStartIndex, _Buckets[ bucketIndex + 1 ] - currentBucketStartIndex);
			return (currentBucketStartIndex, CompressedLength - currentBucketStartIndex);
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static int CompressBytes( ArraySegment<byte> data, ArraySegment<byte> destination )
		{
			return LZ4Codec.Encode( data.Array, data.Offset, data.Count, destination.Array, destination.Offset, destination.Count );
		}

		#endregion
	}
}