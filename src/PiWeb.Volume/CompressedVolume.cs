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
    using System.IO.Compression;
    using System.Linq;
    using System.Threading;

    #endregion

    /// <summary>
    /// A compressed volume.
    /// </summary>
    public sealed class CompressedVolume : Volume
    {
        #region members

        private DirectionMap _CompressedData;

        #endregion

        #region constructors

        internal CompressedVolume( VolumeMetadata metadata, VolumeCompressionOptions options, DirectionMap compressedData ) : base( metadata )
        {
            if( compressedData[ Direction.Z ] == null )
                throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

            _CompressedData = compressedData;
            CompressionOptions = options;
        }

        #endregion

        #region properties

        /// <summary>
        /// Information about how the volume has been compressed. Returns <c>null</c> when the volume has not been compressed yet.
        /// </summary>
        public VolumeCompressionOptions CompressionOptions { get; }

        #endregion

        #region methods

        /// <inheritdoc />
        public override VolumeCompressionState GetCompressionState( Direction direction )
        {
            if( _CompressedData[ direction ] != null )
                return VolumeCompressionState.CompressedInDirection;

            if( _CompressedData[ Direction.Z ] == null )
                throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

            return VolumeCompressionState.Compressed;
        }


        /// <inheritdoc />
        public override UncompressedVolume CreatePreview( ushort minification, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default( CancellationToken ) )
        {
            if( _CompressedData[ Direction.Z ] == null )
                throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

            using( var input = new MemoryStream( _CompressedData[ Direction.Z ], false ) )
            {
                var inputWrapper = new StreamWrapper( input );
                var previewCreator = new PreviewCreator( Metadata, minification, progress, ct );

                var error = ( VolumeError ) DecompressVolume( inputWrapper.Interop, previewCreator.Interop );
                if( error != VolumeError.Success )
                    throw new VolumeException( error, Resources.FormatResource<Volume>( "Decompression_ErrorText", error ) );

                return previewCreator.GetPreview();
            }
        }

        /// <summary>
        /// Decompresses the volume
        /// </summary>
        /// <param name="progress">Progress indicator, which reports the current slice number.</param>
        /// <param name="ct">Cancellation token</param>
        /// <exception cref="VolumeException">Error during decoding</exception>
        /// <exception cref="NotSupportedException">The volume has no compressed data</exception>
        public UncompressedVolume Decompress( IProgress<ushort> progress = null, CancellationToken ct = default( CancellationToken ) )
        {
            if( _CompressedData[ Direction.Z ] == null )
                throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

            using( var input = new MemoryStream( _CompressedData[ Direction.Z ] ) )
            {
                var inputWrapper = new StreamWrapper( input );
                var outputWrapper = new FullVolumeWriter( Metadata, Direction.Z, progress, ct );

                var error = ( VolumeError ) DecompressVolume( inputWrapper.Interop, outputWrapper.Interop );
                if( error != VolumeError.Success )
                    throw new VolumeException( error, Resources.FormatResource<Volume>( "Decompression_ErrorText", error ) );

                return CreateUncompressed( Metadata, outputWrapper.GetData() );
            }
        }

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">The volume has no compressed data</exception>
        public override VolumeSliceCollection GetSliceRanges( IReadOnlyCollection<VolumeSliceRangeDefinition> ranges, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default( CancellationToken ) )
        {
            if( ranges == null )
                throw new ArgumentNullException( nameof(ranges) );

            if( ranges.Count == 0 )
                return new VolumeSliceCollection();

            var combinedRanges = ranges.Merge().ToArray();

            //Partial scan in directed compressed volumes
            if( combinedRanges.All( r => _CompressedData[ r.Direction ] != null ) &&
                combinedRanges.Length <= Constants.RangeNumberLimitForEfficientScan &&
                combinedRanges.Sum( r => r.Last - r.First + 1 ) < Constants.SliceNumberLimitForEfficientScan )
            {
                return new VolumeSliceCollection( ranges.Select( range => GetSliceRange( range ) ) );
            }

            var direction = combinedRanges.First().Direction;

            //Full scan in directed volume in case all ranges have this direction. Scan performance is the same, but copy is more efficient
            if( _CompressedData[ direction ] == null || combinedRanges.Any( r => r.Direction != direction ) )
                direction = Direction.Z;

            if( _CompressedData[ direction ] == null )
                throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

            using( var input = new MemoryStream( _CompressedData[ direction ], false ) )
            {
                var inputWrapper = new StreamWrapper( input );
                var rangeReader = new VolumeSliceRangeCollector( Metadata, direction, combinedRanges, progress, ct );

                var error = ( VolumeError ) DecompressVolume( inputWrapper.Interop, rangeReader.Interop );
                if( error != VolumeError.Success )
                    throw new VolumeException( error, Resources.FormatResource<Volume>( "Decompression_ErrorText", error ) );

                return rangeReader.GetSliceRangeCollection();
            }
        }

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">The volume has no compressed data</exception>
        public override VolumeSliceRange GetSliceRange( VolumeSliceRangeDefinition range, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default( CancellationToken ) )
        {
            if( _CompressedData[ range.Direction ] != null )
            {
                using( var input = new MemoryStream( _CompressedData[ range.Direction ], false ) )
                {
                    var inputWrapper = new StreamWrapper( input );
                    var rangeReader = new VolumeSliceRangeCollector( Metadata, range.Direction, new[] { range }, progress, ct );

                    var error = ( VolumeError ) DecompressSlices( inputWrapper.Interop, rangeReader.Interop, range.First, ( ushort ) ( range.Last - range.First + 1 ) );
                    if( error != VolumeError.Success )
                        throw new VolumeException( error, Resources.FormatResource<Volume>( "Decompression_ErrorText", error ) );

                    return rangeReader.GetSliceRange( range );
                }
            }

            if( _CompressedData[ Direction.Z ] == null )
                throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

            using( var input = new MemoryStream( _CompressedData[ Direction.Z ], false ) )
            {
                var inputWrapper = new StreamWrapper( input );
                var rangeReader = new VolumeSliceRangeCollector( Metadata, Direction.Z, new[] { range }, progress, ct );

                var error = ( VolumeError ) DecompressVolume( inputWrapper.Interop, rangeReader.Interop );
                if( error != VolumeError.Success )
                    throw new VolumeException( error, Resources.FormatResource<Volume>( "Decompression_ErrorText", error ) );

                return rangeReader.GetSliceRange( range );
            }
        }

        /// <inheritdoc />
        /// <exception cref="NotSupportedException">The volume has no compressed data</exception>
        public override VolumeSlice GetSlice( VolumeSliceDefinition slice, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default( CancellationToken ) )
        {
            //Videos with a very small number of frames appearantly have issues with av_seek, so we do a full scan instead
            if( _CompressedData[ slice.Direction ] != null )
            {
                using( var input = new MemoryStream( _CompressedData[ slice.Direction ] ) )
                {
                    var inputWrapper = new StreamWrapper( input );
                    var outputWrapper = new VolumeSliceRangeCollector( Metadata, slice.Direction, new[] { new VolumeSliceRangeDefinition( slice.Direction, slice.Index, slice.Index ) }, progress, ct );

                    var error = ( VolumeError ) DecompressSlices( inputWrapper.Interop, outputWrapper.Interop, slice.Index, 1 );
                    if( error != VolumeError.Success )
                        throw new VolumeException( error, Resources.FormatResource<Volume>( "Decompression_ErrorText", error ) );

                    return outputWrapper.GetSlice( slice.Direction, slice.Index );
                }
            }

            if( _CompressedData[ Direction.Z ] == null )
                throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

            using( var input = new MemoryStream( _CompressedData[ Direction.Z ] ) )
            {
                var inputWrapper = new StreamWrapper( input );

                var rangeReader = new VolumeSliceRangeCollector( Metadata, Direction.Z, new[] { new VolumeSliceRangeDefinition( slice.Direction, slice.Index, slice.Index ) }, progress, ct );
                var error = ( VolumeError ) DecompressVolume( inputWrapper.Interop, rangeReader.Interop );
                if( error != VolumeError.Success )
                    throw new VolumeException( error, Resources.FormatResource<Volume>( "Decompression_ErrorText", error ) );

                return rangeReader.GetSlice( slice.Direction, slice.Index );
            }
        }

        /// <summary>
        /// Saves the volume into the specified stream
        /// </summary>
        /// <param name="stream"></param>
        public void Save( Stream stream )
        {
            if( stream == null )
                throw new ArgumentNullException( nameof(stream) );

            using( var zipOutput = new ZipArchive( stream, ZipArchiveMode.Create ) )
            {
                var entry = zipOutput.CreateNormalizedEntry( "Metadata.xml", CompressionLevel.Optimal );
                using( var entryStream = entry.Open() )
                {
                    Metadata.Serialize( entryStream );
                }

                if( CompressionOptions != null )
                {
                    entry = zipOutput.CreateNormalizedEntry( "CompressionOptions.xml", CompressionLevel.Optimal );
                    using( var entryStream = entry.Open() )
                    {
                        CompressionOptions.Serialize( entryStream );
                    }
                }

                if( _CompressedData[ Direction.Z ] == null )
                    throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

                entry = zipOutput.CreateNormalizedEntry( "VoxelsZ.dat", CompressionLevel.NoCompression );
                using( var entryStream = entry.Open() )
                {
                    entryStream.Write( _CompressedData[ Direction.Z ], 0, _CompressedData[ Direction.Z ].Length );
                }

                if( _CompressedData[ Direction.X ] != null )
                {
                    entry = zipOutput.CreateNormalizedEntry( "VoxelsX.dat", CompressionLevel.NoCompression );
                    using( var entryStream = entry.Open() )
                    {
                        entryStream.Write( _CompressedData[ Direction.X ], 0, _CompressedData[ Direction.X ].Length );
                    }
                }

                if( _CompressedData[ Direction.Y ] != null )
                {
                    entry = zipOutput.CreateNormalizedEntry( "VoxelsY.dat", CompressionLevel.NoCompression );
                    using( var entryStream = entry.Open() )
                    {
                        entryStream.Write( _CompressedData[ Direction.Y ], 0, _CompressedData[ Direction.Y ].Length );
                    }
                }
            }
        }

        #endregion
    }
}