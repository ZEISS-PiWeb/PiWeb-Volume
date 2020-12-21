#region copyright

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
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;
	using Zeiss.IMT.PiWeb.Volume.Block;

	#endregion

	/// <summary>
	/// TODO: add summary.
	/// </summary>
	public sealed class StreamedVolume : Volume, IDisposable
	{
		#region members

		private readonly bool _LeaveOpen;

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
			Stream = stream;
			if( !stream.CanSeek )
				throw new ArgumentException( "The stream must be seekable." );
		}

		#endregion

		#region properties

		/// <summary>
		/// The uncompressed voxel data
		/// </summary>
		public Stream Stream { get; }

		#endregion

		#region methods

		/// <summary>
		/// Compresses the volume with the specified compression options.
		/// </summary>
		/// <param name="options"></param>
		/// <param name="progress">A progress indicator, which reports the current slice number.</param>
		/// <param name="ct"></param>
		/// <exception cref="VolumeException">Error during encoding</exception>
		public CompressedVolume Compress( VolumeCompressionOptions options, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			if( options.Encoder == BlockVolume.EncoderID )
			{
				Stream.Seek( 0, SeekOrigin.Begin );
				return BlockVolume.Create( Stream, Metadata, options, progress, ct );
			}

			var directionMap = new DirectionMap { [ Direction.Z ] = CompressDirection( options, progress, ct ) };

			return new CompressedVolume( Metadata, options, directionMap );
		}

		/// <summary>
		/// Compresses the volume with the specified compression options.
		/// </summary>
		/// <param name="progress">A progress indicator, which reports the current slice number.</param>
		/// <param name="ct"></param>
		/// <param name="options">Codec settings</param>
		/// <exception cref="VolumeException">Error during encoding</exception>
		/// <exception cref="NotSupportedException">The volume has no decompressed data</exception>
		private byte[] CompressDirection( VolumeCompressionOptions options, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			Stream.Seek( 0, SeekOrigin.Begin );

			using var outputStream = new MemoryStream();

			GetEncodedSliceSize( Metadata, Direction.Z, out var encodingSizeX, out var encodingSizeY );

			var inputStreamWrapper = new SliceStreamReader( Metadata, Stream, progress, ct );
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
		public override UncompressedVolume CreatePreview( ushort minification, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			Stream.Seek( 0, SeekOrigin.Begin );
			return PreviewCreator.CreatePreview( Stream, Metadata, minification, progress );
		}

		/// <inheritdoc />
		public override VolumeSliceCollection GetSliceRanges( IReadOnlyCollection<VolumeSliceRangeDefinition> ranges, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			throw new NotImplementedException();

			/*if( ranges == null )
			    throw new ArgumentNullException( nameof(ranges) );

			if( ranges.Count == 0 )
			    return new VolumeSliceCollection();

			Stream.Seek( 0, SeekOrigin.Begin );
			return new VolumeSliceCollection( ranges.Select( range => VolumeSliceRange.Extract( range, Metadata, Data ) ) );*/
		}

		/// <inheritdoc />
		public override VolumeSliceRange GetSliceRange( VolumeSliceRangeDefinition range, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			throw new NotImplementedException();

			//Stream.Seek( 0, SeekOrigin.Begin );
			//return VolumeSliceRange.Extract( range, Metadata, Data );
		}

		/// <inheritdoc />
		public override VolumeSlice GetSlice( VolumeSliceDefinition slice, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			Stream.Seek( 0, SeekOrigin.Begin );
			return VolumeSlice.Extract( slice.Direction, slice.Index, Metadata, Stream );
		}

		/// <summary>
		/// Compresses and saves the volume in the specified stream.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="options">The options.</param>
		/// <param name="progress">The progress.</param>
		/// <param name="ct">The ct.</param>
		public void Save( Stream stream, VolumeCompressionOptions options, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			var compressed = Compress( options, progress, ct );
			compressed.Save( stream );
		}

		#endregion

		#region interface IDisposable

		/// <inheritdoc />
		public void Dispose()
		{
			if( !_LeaveOpen )
				Stream?.Dispose();
		}

		#endregion
	}
}