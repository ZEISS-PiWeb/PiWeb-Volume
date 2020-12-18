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
	using System.Linq;
	using System.Threading;
	using Zeiss.IMT.PiWeb.Volume.Block;

	#endregion

	/// <summary>
	/// An uncompressed volume. This volume is optimized for speed to make the access
	/// to slices as fast as possible. The tradeoff for speed is memory.  
	/// </summary>
	/// <seealso cref="CompressedVolume"/>
	public sealed class UncompressedVolume : Volume
	{
		#region constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="UncompressedVolume" /> class.
		/// </summary>
		/// <param name="metadata">The metadata.</param>
		/// <param name="slices">The grayscale slice data.</param>
		public UncompressedVolume( VolumeMetadata metadata, IReadOnlyList<VolumeSlice> slices ) 
			: base( metadata )
		{
			Slices = slices;
			CheckForIntegrity();
		}

		#endregion

		#region properties

		/// <summary>
		/// The uncompressed voxel data
		/// </summary>
		public IReadOnlyList<VolumeSlice> Slices { get; }

		#endregion

		#region methods

		/// <summary>
		/// Checks the data for integrity. Works only on decompressed data. Will throw exceptions in case a check failed.
		/// </summary>
		/// <exception cref="IndexOutOfRangeException"></exception>
		private void CheckForIntegrity()
		{
			if( Slices.Count != Metadata.SizeZ  )
				throw new VolumeIntegrityException( $"Invalid number of slices (expected: {Metadata.SizeZ}, but was {Slices.Count})." );

			var expectedSliceSize = Metadata.SizeX * Metadata.SizeY;
			var index = 0;
			foreach( var slice in Slices )
			{
				if( slice.Data.Length != expectedSliceSize )
					throw new VolumeIntegrityException( $"Invalid dimension of slice {index} (expected: {expectedSliceSize}, but was {slice.Data.Length})." );
				if( slice.Direction != Direction.Z )
					throw new VolumeIntegrityException( $"Invalid slice direction for slice {index} (expected: {Direction.Z}, but was {slice.Direction})." );
				index++;
			}
		}

		/// <summary>
		/// Compresses the volume with the specified compression options.
		/// </summary>
		/// <param name="options"></param>
		/// <param name="multiDirection"></param>
		/// <param name="progress">A progress indicator, which reports the current slice number.</param>
		/// <param name="ct"></param>
		/// <exception cref="VolumeException">Error during encoding</exception>
		public CompressedVolume Compress( VolumeCompressionOptions options, bool multiDirection = false, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			if( options.Encoder == BlockVolume.EncoderID )
			{
				return BlockVolume.Create( Slices, Metadata, options, progress, ct );
			}

			var directionMap = new DirectionMap { [ Direction.Z ] = CompressDirection( Direction.Z, options, progress, ct ) };

			if( multiDirection )
			{
				directionMap[ Direction.X ] = CompressDirection( Direction.X, options, progress, ct );
				directionMap[ Direction.Y ] = CompressDirection( Direction.Y, options, progress, ct );
			}

			return new CompressedVolume( Metadata, options, directionMap );
		}

		/// <summary>
		/// Compresses the volume with the specified compression options.
		/// </summary>
		/// <param name="progress">A progress indicator, which reports the current slice number.</param>
		/// <param name="ct"></param>
		/// <param name="direction"></param>
		/// <param name="options">Codec settings</param>
		/// <exception cref="VolumeException">Error during encoding</exception>
		/// <exception cref="NotSupportedException">The volume has no decompressed data</exception>
		private byte[] CompressDirection( Direction direction, VolumeCompressionOptions options, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			using( var outputStream = new MemoryStream() )
			{
				GetEncodedSliceSize( Metadata, direction, out var encodingSizeX, out var encodingSizeY );

				var inputStreamWrapper = new SliceReader( Metadata, Slices, direction, progress, ct );
				var outputStreamWrapper = new StreamWrapper( outputStream );

				var error = ( VolumeError ) CompressVolume( inputStreamWrapper.Interop, outputStreamWrapper.Interop, encodingSizeX, encodingSizeY, options.Encoder, options.PixelFormat, options.GetOptionsString(), options.Bitrate );

				if( error != VolumeError.Success )
					throw new VolumeException( error, Resources.FormatResource<Volume>( "Compression_ErrorText", error ) );

				return outputStream.ToArray();
			}
		}

		/// <inheritdoc />
		public override VolumeCompressionState GetCompressionState( Direction direction )
		{
			return VolumeCompressionState.Decompressed;
		}

		/// <inheritdoc />
		public override UncompressedVolume CreatePreview( ushort minification, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			return PreviewCreator.CreatePreview( Slices, Metadata, minification );
		}

		/// <inheritdoc />
		public override VolumeSliceCollection GetSliceRanges( IReadOnlyCollection<VolumeSliceRangeDefinition> ranges, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			if( ranges == null )
				throw new ArgumentNullException( nameof(ranges) );

			if( ranges.Count == 0 )
				return new VolumeSliceCollection();

			return new VolumeSliceCollection( ranges.Select( range => VolumeSliceRange.Extract( range, Metadata, Slices ) ) );
		}

		/// <inheritdoc />
		public override VolumeSliceRange GetSliceRange( VolumeSliceRangeDefinition range, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			return VolumeSliceRange.Extract( range, Metadata, Slices );
		}

		/// <inheritdoc />
		public override VolumeSlice GetSlice( VolumeSliceDefinition slice, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			return VolumeSlice.Extract( slice.Direction, slice.Index, Metadata, Slices );
		}

		/// <summary>
		/// Compresses and saves the volume in the specified stream.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="options">The options.</param>
		/// <param name="multiDirection">if set to <c>true</c> [multi direction].</param>
		/// <param name="progress">The progress.</param>
		/// <param name="ct">The ct.</param>
		public void Save( Stream stream, VolumeCompressionOptions options, bool multiDirection = false, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			var compressed = Compress( options, multiDirection, progress, ct );
			compressed.Save( stream );
		}

		#endregion
	}
}