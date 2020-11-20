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
    /// TODO: add summary.
    /// </summary>
    public sealed class UncompressedVolume : Volume
    {
        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UncompressedVolume" /> class.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        /// <param name="data">The grayscale slice data.</param>
        public UncompressedVolume( VolumeMetadata metadata, byte[][] data ) : base( metadata )
        {
            Data = data;

            CheckForIntegrity();
        }

        #endregion

        #region properties

        /// <summary>
        /// The uncompressed voxel data
        /// </summary>
        public byte[][] Data { get; }

        #endregion

        #region methods

        /// <summary>
        /// Checks the data for integrity. Works only on decompressed data. Will throw exceptions in case a check failed.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"></exception>
        private void CheckForIntegrity()
        {
            //Check data length to fit the dimensions
            if( Data.Length == 0 || Data.LongLength * Data[ 0 ].LongLength != ( long ) Metadata.SizeX * Metadata.SizeY * Metadata.SizeZ )
                throw new IndexOutOfRangeException( Resources.GetResource<Volume>( "DimensionMismatch_ErrorText" ) );
        }

        /// <summary>
        /// Compresses the volume with the specified compression options.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="multiDirection"></param>
        /// <param name="progress">A progress indicator, which reports the current slice number.</param>
        /// <param name="ct"></param>
        /// <exception cref="VolumeException">Error during encoding</exception>
        public CompressedVolume Compress( VolumeCompressionOptions options, bool multiDirection = false, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default( CancellationToken ) )
        {
	        if( options.Encoder == BlockVolume.EncoderID )
	        {
		        return BlockVolume.Create( Data, Metadata, options, progress, ct );
	        }
			else
			{
				var directionMap = new DirectionMap { [ Direction.Z ] = CompressDirection( Direction.Z, options, progress, ct ) };

				if( multiDirection )
				{
					directionMap[ Direction.X ] = CompressDirection( Direction.X, options, progress, ct );
					directionMap[ Direction.Y ] = CompressDirection( Direction.Y, options, progress, ct );
				}
				return new CompressedVolume( Metadata, options, directionMap );
			}
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
        private byte[] CompressDirection( Direction direction, VolumeCompressionOptions options, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default( CancellationToken ) )
        {
	        using( var outputStream = new MemoryStream() )
            {
                GetEncodedSliceSize( Metadata, direction, out var encodingSizeX, out var encodingSizeY );

                var inputStreamWrapper = new SliceReader( Metadata, Data, direction, progress, ct );
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
        public override UncompressedVolume CreatePreview( ushort minification, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default( CancellationToken ) )
        {
            return PreviewCreator.CreatePreview( Data, Metadata, minification );
        }

        /// <inheritdoc />
        public override VolumeSliceCollection GetSliceRanges( IReadOnlyCollection<VolumeSliceRangeDefinition> ranges, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default( CancellationToken ) )
        {
            if( ranges == null )
                throw new ArgumentNullException( nameof(ranges) );

            if( ranges.Count == 0 )
                return new VolumeSliceCollection();

            return new VolumeSliceCollection( ranges.Select( range => VolumeSliceRange.Extract( range, Metadata, Data ) ) );
        }

        /// <inheritdoc />
        public override VolumeSliceRange GetSliceRange( VolumeSliceRangeDefinition range, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default( CancellationToken ) )
        {
            return VolumeSliceRange.Extract( range, Metadata, Data );
        }

        /// <inheritdoc />
        public override VolumeSlice GetSlice( VolumeSliceDefinition slice, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default( CancellationToken ) )
        {
            return VolumeSlice.Extract( slice.Direction, slice.Index, Metadata, Data );
        }

        /// <summary>
        /// Compresses and saves the volume in the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="options">The options.</param>
        /// <param name="multiDirection">if set to <c>true</c> [multi direction].</param>
        /// <param name="progress">The progress.</param>
        /// <param name="ct">The ct.</param>
        public void Save( Stream stream, VolumeCompressionOptions options, bool multiDirection = false, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default( CancellationToken ) )
        {
            var compressed = Compress( options, multiDirection, progress, ct );
            compressed.Save( stream );
        }

        #endregion
    }
}