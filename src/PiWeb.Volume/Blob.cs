#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2026                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume;

#region usings

using System;
using System.Collections.Generic;
using System.IO;

#endregion

/// <summary>
/// Handles binary data that exceeds the builtin 2GB limit with a stream interface.
/// </summary>
internal class Blob
{
	#region members

	private readonly byte[][] _Data;
	private readonly long[] _PrefixLengths; // Start offset of each segment.

	#endregion

	#region constructors

	internal Blob( byte[][] data )
	{
		_Data = data;
		_PrefixLengths = new long[ _Data.Length ];

		long running = 0;
		for( var i = 0; i < _Data.Length; i++ )
		{
			var segment = _Data[ i ];
			_PrefixLengths[ i ] = running;
			running += segment.Length;
		}

		Length = running;
	}

	#endregion

	#region properties

	/// <summary>
	/// Overall length of the blob.
	/// </summary>
	public long Length { get; }

	#endregion

	#region methods

	/// <summary>
	///
	/// </summary>
	/// <param name="stream"></param>
	/// <param name="chunkSize"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public static Blob FromStream( Stream stream, int chunkSize = 100 * 1024 * 1024 )
	{
		if( stream == null )
			throw new ArgumentNullException( nameof( stream ) );
		if( chunkSize <= 0 )
			throw new ArgumentOutOfRangeException( nameof( chunkSize ) );

		var chunks = new List<byte[]>();
		var buffer = new byte[ chunkSize ];

		while( true )
		{
			var read = stream.Read( buffer, 0, buffer.Length );
			if( read <= 0 )
				break;

			var chunk = new byte[ read ];
			Buffer.BlockCopy( buffer, 0, chunk, 0, read );
			chunks.Add( chunk );
		}

		return new Blob( chunks.ToArray() );
	}

	public Stream ToStream()
	{
		return new ReadStream( this );
	}

	private int FindSegmentIndex( long position )
	{
		// Position can be == Length (EOF). In that case caller should handle before calling.
		// Binary search for last prefix <= position.
		var lo = 0;
		var hi = _PrefixLengths.Length - 1;

		while( lo <= hi )
		{
			var mid = lo + ( ( hi - lo ) / 2 );
			var start = _PrefixLengths[ mid ];
			var end = start + _Data[ mid ].Length;

			if( position < start )
				hi = mid - 1;
			else if( position >= end )
				lo = mid + 1;
			else
				return mid;
		}

		// For empty blobs this can happen.
		return -1;
	}

	public int CopyTo( long sourceOffset, byte[] destination, int destinationOffset, int count )
	{
		if( destination == null )
			throw new ArgumentNullException( nameof( destination ) );
		if( destinationOffset < 0 || count < 0 || destinationOffset + count > destination.Length )
			throw new ArgumentOutOfRangeException();

		if( count == 0 || sourceOffset >= Length )
			return 0;

		var remainingTotal = Length - sourceOffset;
		var toRead = (int)Math.Min( count, remainingTotal );
		var readTotal = 0;
		var segmentIndex = FindSegmentIndex( sourceOffset );
		if( segmentIndex < 0 )
			return readTotal; // Empty blob guard.

		while( readTotal < toRead )
		{
			if (segmentIndex >= _Data.Length)
				break;

			var segment = _Data[ segmentIndex ];
			var segmentStart = _PrefixLengths[ segmentIndex ];
			var segmentOffset = (int)( sourceOffset - segmentStart );
			var availableInSegment = segment.Length - segmentOffset;
			var needed = toRead - readTotal;
			var copy = Math.Min( availableInSegment, needed );

			Buffer.BlockCopy( segment, segmentOffset, destination, destinationOffset + readTotal, copy );

			readTotal += copy;
			sourceOffset += copy;
			segmentIndex++;
		}

		return readTotal;
	}

	#endregion

	#region class ReadStream

	private sealed class ReadStream : Stream
	{
		#region members

		private readonly Blob _Blob;
		private long _Position;

		#endregion

		#region constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ReadStream"/> class.
		/// </summary>
		public ReadStream( Blob blob )
		{
			_Blob = blob ?? throw new ArgumentNullException( nameof( blob ) );
			_Position = 0;
		}

		#endregion

		#region properties

		/// <inheritdoc />
		public override bool CanRead => true;

		/// <inheritdoc />
		public override bool CanSeek => true;

		/// <inheritdoc />
		public override bool CanWrite => false;

		/// <inheritdoc />
		public override long Length => _Blob.Length;

		/// <inheritdoc />
		public override long Position
		{
			get => _Position;
			set
			{
				if( value < 0 || value > Length )
					throw new ArgumentOutOfRangeException( nameof( value ) );
				_Position = value;
			}
		}

		#endregion

		#region methods

		/// <inheritdoc />
		public override void Flush()
		{
			// No-op for read-only in-memory stream.
		}

		/// <inheritdoc />
		public override int Read( byte[] buffer, int offset, int count )
		{
			if( buffer == null )
				throw new ArgumentNullException( nameof( buffer ) );
			if( offset < 0 || count < 0 || offset + count > buffer.Length )
				throw new ArgumentOutOfRangeException();

			if( count == 0 || _Position >= Length )
				return 0;

			var remainingTotal = Length - _Position;
			var toRead = (int)Math.Min( count, remainingTotal );
			var readTotal = 0;

			while( readTotal < toRead )
			{
				var segmentIndex = _Blob.FindSegmentIndex( _Position );
				if( segmentIndex < 0 )
					break; // Empty blob guard.

				var segment = _Blob._Data[ segmentIndex ];
				var segmentStart = _Blob._PrefixLengths[ segmentIndex ];
				var segmentOffset = (int)( _Position - segmentStart );
				var availableInSegment = segment.Length - segmentOffset;
				var needed = toRead - readTotal;
				var copy = Math.Min( availableInSegment, needed );

				Buffer.BlockCopy( segment, segmentOffset, buffer, offset + readTotal, copy );

				readTotal += copy;
				_Position += copy;
			}

			return readTotal;
		}

		/// <inheritdoc />
		public override long Seek( long offset, SeekOrigin origin )
		{
			long target;
			switch( origin )
			{
				case SeekOrigin.Begin:
					target = offset;
					break;
				case SeekOrigin.Current:
					target = _Position + offset;
					break;
				case SeekOrigin.End:
					target = Length + offset;
					break;
				default:
					throw new ArgumentOutOfRangeException( nameof( origin ) );
			}

			if( target < 0 || target > Length )
				throw new IOException( "Attempted to seek outside blob bounds." );

			_Position = target;
			return _Position;
		}

		/// <inheritdoc />
		public override void SetLength( long value )
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		public override void Write( byte[] buffer, int offset, int count )
		{
			throw new NotSupportedException();
		}

		#endregion
	}

	#endregion

	#region class WriteStream

	internal sealed class WriteStream : Stream
	{
		#region members

		private readonly int _ChunkSize;
		private readonly List<byte[]> _Chunks = [];
		private byte[]? _CurrentChunk;
		private int _CurrentChunkPos;
		private long _Length;
		private bool _Disposed;

		#endregion

		#region constructors

		public WriteStream( int chunkSize = 1024 * 1024 )
		{
			if( chunkSize <= 0 )
				throw new ArgumentOutOfRangeException( nameof( chunkSize ) );

			_ChunkSize = chunkSize;
		}

		#endregion

		#region properties

		/// <inheritdoc />
		public override bool CanRead => false;

		/// <inheritdoc />
		public override bool CanSeek => false;

		/// <inheritdoc />
		public override bool CanWrite => !_Disposed;

		/// <inheritdoc />
		public override long Length => _Length;

		/// <inheritdoc />
		public override long Position
		{
			get => _Length;
			set => throw new NotSupportedException( "BlobWriteStream is non-seekable." );
		}

		#endregion

		#region methods

		/// <summary>
		/// Returns a <see cref="Blob"/> that contains the data that was written to this stream.
		/// </summary>
		public Blob ToBlob()
		{
			ThrowIfDisposed();

			// Freeze snapshot of currently written data.
			var result = new byte[ _Chunks.Count + ( _CurrentChunkPos > 0 ? 1 : 0 ) ][];

			for( var i = 0; i < _Chunks.Count; i++ )
				result[ i ] = _Chunks[ i ];

			if( _CurrentChunkPos > 0 )
			{
				var tail = new byte[ _CurrentChunkPos ];
				Buffer.BlockCopy( _CurrentChunk!, 0, tail, 0, _CurrentChunkPos );
				result[ ^1 ] = tail;
			}

			return new Blob( result );
		}

		/// <inheritdoc />
		public override void Write( byte[] buffer, int offset, int count )
		{
			ThrowIfDisposed();

			if( buffer == null )
				throw new ArgumentNullException( nameof( buffer ) );
			if( offset < 0 || count < 0 || offset + count > buffer.Length )
				throw new ArgumentOutOfRangeException();

			var remaining = count;
			var srcOffset = offset;

			while( remaining > 0 )
			{
				EnsureWritableChunk();

				var free = _ChunkSize - _CurrentChunkPos;
				var toCopy = Math.Min( free, remaining );

				Buffer.BlockCopy( buffer, srcOffset, _CurrentChunk!, _CurrentChunkPos, toCopy );

				_CurrentChunkPos += toCopy;
				srcOffset += toCopy;
				remaining -= toCopy;
				_Length += toCopy;

				if( _CurrentChunkPos == _ChunkSize )
					CommitCurrentChunk();
			}
		}

		/// <inheritdoc />
		public override void WriteByte( byte value )
		{
			ThrowIfDisposed();

			EnsureWritableChunk();
			_CurrentChunk![ _CurrentChunkPos++ ] = value;
			_Length++;

			if( _CurrentChunkPos == _ChunkSize )
				CommitCurrentChunk();
		}

		/// <inheritdoc />
		public override void Flush()
		{
			ThrowIfDisposed();
		}

		/// <inheritdoc />
		public override int Read( byte[] buffer, int offset, int count )
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		public override long Seek( long offset, SeekOrigin origin )
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		public override void SetLength( long value )
		{
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		protected override void Dispose( bool disposing )
		{
			_Disposed = true;
			base.Dispose( disposing );
		}

		private void EnsureWritableChunk()
		{
			if( _CurrentChunk == null )
			{
				_CurrentChunk = new byte[ _ChunkSize ];
				_CurrentChunkPos = 0;
			}
		}

		private void CommitCurrentChunk()
		{
			if( _CurrentChunkPos == 0 || _CurrentChunk == null )
				return;

			// Full chunk is stored as-is (no copy).
			_Chunks.Add( _CurrentChunk );
			_CurrentChunk = null;
			_CurrentChunkPos = 0;
		}

		private void ThrowIfDisposed()
		{
			if( !_Disposed ) return;
			throw new ObjectDisposedException( nameof( WriteStream ) );
		}

		#endregion
	}

	#endregion
}