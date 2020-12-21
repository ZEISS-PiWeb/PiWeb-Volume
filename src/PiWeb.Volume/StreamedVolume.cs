﻿#region copyright

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
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using Zeiss.IMT.PiWeb.Volume.Block;

	#endregion

	/// <summary>
	/// This volume class lazily loads the volume data from an
	/// underlying stream. 
	/// </summary>
	public sealed class StreamedVolume : Volume, IDisposable
	{
		#region members

		private readonly bool _LeaveOpen;
		private readonly Stream _Stream;

		#endregion

		#region constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="UncompressedVolume" /> class.
		/// </summary>
		/// <param name="metadata">The metadata.</param>
		/// <param name="stream">The grayscale slice data stream.</param>
		/// <param name="leaveOpen">Determines whether this volume closes the stream.</param>
		public StreamedVolume( VolumeMetadata metadata, Stream stream, bool leaveOpen = false ) : base( metadata )
		{
			_LeaveOpen = leaveOpen;
			_Stream = stream ?? throw new ArgumentNullException( nameof(stream) );
			if( !stream.CanSeek )
				throw new ArgumentException( "The stream must be seekable." );
		}

		#endregion

		#region methods

		/// <summary>
		/// Compresses the volume with the specified compression options.
		/// </summary>
		/// <exception cref="VolumeException">Error during encoding</exception>
		public CompressedVolume Compress( VolumeCompressionOptions options, IProgress<VolumeSliceDefinition> progress = null, ILogger logger = null, CancellationToken ct = default )
		{
			var sw = Stopwatch.StartNew();
			try
			{
				if( options.Encoder == BlockVolume.EncoderID )
				{
					_Stream.Seek( 0, SeekOrigin.Begin );
					return new BlockVolume( _Stream, Metadata, options, progress );
				}

				var directionMap = new DirectionMap { [ Direction.Z ] = CompressDirection( options, progress, ct ) };

				return new CompressedVolume( Metadata, options, directionMap );
			}
			finally
			{
				logger?.Log( LogLevel.Info, $"Compressed volume with encoder '{options.Encoder}' in {sw.ElapsedMilliseconds} ms." );
			}
		}

		private byte[] CompressDirection( VolumeCompressionOptions options, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			_Stream.Seek( 0, SeekOrigin.Begin );

			using var outputStream = new MemoryStream();

			GetEncodedSliceSize( Metadata, Direction.Z, out var encodingSizeX, out var encodingSizeY );

			var inputStreamWrapper = new SliceStreamReader( Metadata, _Stream, progress, ct );
			var outputStreamWrapper = new StreamWrapper( outputStream );

			var error = NativeMethods.CompressVolume( inputStreamWrapper.Interop, outputStreamWrapper.Interop, encodingSizeX, encodingSizeY, options.Encoder, options.PixelFormat, options.GetOptionsString(), options.Bitrate );

			if( error != VolumeError.Success )
				throw new VolumeException( error, Resources.FormatResource<Volume>( "Compression_ErrorText", error ) );

			return outputStream.ToArray();
		}

		/// <inheritdoc />
		public override VolumeCompressionState GetCompressionState( Direction direction )
		{
			return VolumeCompressionState.Decompressed;
		}

		/// <inheritdoc />
		public override UncompressedVolume CreatePreview( ushort minification, IProgress<VolumeSliceDefinition> progress = null, ILogger logger = null, CancellationToken ct = default )
		{
			var sw = Stopwatch.StartNew();
			
			_Stream.Seek( 0, SeekOrigin.Begin );
			
			var result = PreviewCreator.CreatePreview( _Stream, Metadata, minification, progress );
			logger?.Log( LogLevel.Info, $"Created a preview with minification factor {minification} in {sw.ElapsedMilliseconds} ms." );

			return result;
		}

		/// <inheritdoc />
		public override VolumeSliceCollection GetSliceRanges( IReadOnlyCollection<VolumeSliceRangeDefinition> ranges, IProgress<VolumeSliceDefinition> progress = null, ILogger logger = null, CancellationToken ct = default )
		{
			var sw = Stopwatch.StartNew();

			var result = new VolumeSliceCollection( ranges.Select( range => GetSliceRange( range, progress, logger, ct ) ) );
			logger?.Log( LogLevel.Info, $"Extracted '{ranges.Count}' slice ranges in {sw.ElapsedMilliseconds} ms." );

			return result;
		}

		/// <inheritdoc />
		public override VolumeSliceRange GetSliceRange( VolumeSliceRangeDefinition range, IProgress<VolumeSliceDefinition> progress = null, ILogger logger = null, CancellationToken ct = default )
		{
			throw new NotImplementedException();
			//var sw = Stopwatch.StartNew();

			//Stream.Seek( 0, SeekOrigin.Begin );
			//var result = VolumeSliceRange.Extract( range, Metadata, Data );
			//logger?.Log( LogLevel.Info, $"Extracted '{range}' in {sw.ElapsedMilliseconds} ms." );

			//return result;
		}

		/// <inheritdoc />
		public override VolumeSlice GetSlice( VolumeSliceDefinition slice, IProgress<VolumeSliceDefinition> progress = null, ILogger logger = null, CancellationToken ct = default )
		{
			var sw = Stopwatch.StartNew();
			try
			{
				return slice.Direction switch
				{
					Direction.X => ReadSliceX( slice.Index ),
					Direction.Y => ReadSliceY( slice.Index ),
					Direction.Z => ReadSliceZ( slice.Index ),
					_ => throw new ArgumentOutOfRangeException( nameof(slice.Direction), slice.Direction, null )
				};
			}
			finally
			{
				logger?.Log( LogLevel.Info, $"Extracted '{slice}' in {sw.ElapsedMilliseconds} ms." );
			}
		}

		private VolumeSlice ReadSliceX( ushort index )
		{
			var sx = Metadata.SizeX;
			var sy = Metadata.SizeY;
			var sz = Metadata.SizeZ;

			var bufferSize = sx * sy;
			
			// TODO: Warum lesen wir hier das Ergebnis nicht gleich in den Zielbuffer? 
			var buffer = ArrayPool<byte>.Shared.Rent( bufferSize );
			
			var result = new byte[sy * sz];

			_Stream.Seek( 0, SeekOrigin.Begin );
			for( var z = 0; z < sz; z++ )
			{
				_Stream.Read( buffer, 0, bufferSize );

				for( var y = 0; y < sy; y++ )
				{
					result[ z * sy + y ] = buffer[ y * sx + index ];
				}
			}

			ArrayPool<byte>.Shared.Return( buffer );

			return new VolumeSlice( Direction.X, index, result );
		}

		private VolumeSlice ReadSliceY( ushort index )
		{
			var sx = Metadata.SizeX;
			var sy = Metadata.SizeY;
			var sz = Metadata.SizeZ;

			var result = new byte[sx * sz];
			var bufferSize = sx * sy;
			
			// TODO: Warum lesen wir hier das Ergebnis nicht gleich in den Zielbuffer? 
			var buffer = ArrayPool<byte>.Shared.Rent( bufferSize );

			_Stream.Seek( 0, SeekOrigin.Begin );
			for( var z = 0; z < sz; z++ )
			{
				_Stream.Read( buffer, 0, bufferSize );
				Array.Copy( buffer, index * sx, result, z * sx, sx );
			}

			ArrayPool<byte>.Shared.Return( buffer );

			return new VolumeSlice( Direction.Y, index, result );
		}

		private VolumeSlice ReadSliceZ( ushort index )
		{
			var sx = Metadata.SizeX;
			var sy = Metadata.SizeY;

			var bufferSize = sx * sy;
			var result = new byte[bufferSize];

			_Stream.Seek( ( long ) index * sx * sy, SeekOrigin.Begin );
			_Stream.Read( result, 0, bufferSize );

			return new VolumeSlice( Direction.Z, index, result );
		}

		/// <summary>
		/// Compresses and saves the volume in the specified stream.
		/// </summary>
		public void Save( Stream stream, VolumeCompressionOptions options, IProgress<VolumeSliceDefinition> progress = null, ILogger logger = null, CancellationToken ct = default )
		{
			var sw = Stopwatch.StartNew();
			try
			{
				var compressed = Compress( options, progress, logger, ct );
				compressed.Save( stream );
			}
			finally
			{
				logger?.Log( LogLevel.Info, $"Saved volume to stream in {sw.ElapsedMilliseconds} ms." );
			}
		}

		#endregion

		#region interface IDisposable

		/// <inheritdoc />
		public void Dispose()
		{
			if( !_LeaveOpen )
				_Stream?.Dispose();
		}

		#endregion
	}
}