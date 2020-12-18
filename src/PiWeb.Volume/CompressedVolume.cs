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
	using Zeiss.IMT.PiWeb.Volume.Block;

	#endregion

	/// <summary>
	/// A compressed volume. This volume is optimized for memory size. The tradeoff for memory size is speed.
	/// </summary>
	/// <seealso cref="UncompressedVolume"/>
	public class CompressedVolume : Volume
	{
		#region members

		internal DirectionMap CompressedData;

		#endregion

		#region constructors

		internal CompressedVolume( VolumeMetadata metadata, VolumeCompressionOptions options, DirectionMap compressedData ) : base( metadata )
		{
			if( compressedData[ Direction.Z ] == null )
				throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

			CompressedData = compressedData;
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
			if( CompressedData[ direction ] != null )
				return VolumeCompressionState.CompressedInDirection;

			if( CompressedData[ Direction.Z ] == null )
				throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

			return VolumeCompressionState.Compressed;
		}


		/// <inheritdoc />
		public override UncompressedVolume CreatePreview( ushort minification, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			if( CompressedData[ Direction.Z ] == null )
				throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

			using var input = new MemoryStream( CompressedData[ Direction.Z ], false );
			var inputWrapper = new StreamWrapper( input );
			var previewCreator = new PreviewCreator( Metadata, minification, progress, ct );

			var error = ( VolumeError ) DecompressVolume( inputWrapper.Interop, previewCreator.Interop );
			if( error != VolumeError.Success )
				throw new VolumeException( error, Resources.FormatResource<Volume>( "Decompression_ErrorText", error ) );

			return previewCreator.GetPreview();
		}

		internal static CompressedVolume Create( Stream stream, Direction direction, VolumeMetadata metadata, VolumeCompressionOptions options, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			var compressedData = CompressStream( stream, Direction.Z, metadata, options, progress, ct );
			var directionMap = new DirectionMap
			{
				[ Direction.Z ] = compressedData
			};

			return new CompressedVolume( metadata, options, directionMap );
		}

		private static byte[] CompressStream( Stream stream, Direction direction, VolumeMetadata metadata, VolumeCompressionOptions options, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			using var outputStream = new MemoryStream();
			GetEncodedSliceSize( metadata, direction, out var encodingSizeX, out var encodingSizeY );

			var inputStreamWrapper = new SliceStreamReader( metadata, stream, progress, ct );
			var outputStreamWrapper = new StreamWrapper( outputStream );

			var error = ( VolumeError ) CompressVolume( inputStreamWrapper.Interop, outputStreamWrapper.Interop, encodingSizeX, encodingSizeY, options.Encoder, options.PixelFormat, options.GetOptionsString(), options.Bitrate );

			if( error != VolumeError.Success )
				throw new VolumeException( error, Resources.FormatResource<Volume>( "Compression_ErrorText", error ) );

			return outputStream.ToArray();
		}

		/// <summary>
		/// Decompresses the volume
		/// </summary>
		/// <param name="progress">Progress indicator, which reports the current slice number.</param>
		/// <param name="ct">Cancellation token</param>
		/// <exception cref="VolumeException">Error during decoding</exception>
		/// <exception cref="NotSupportedException">The volume has no compressed data</exception>
		public virtual UncompressedVolume Decompress( IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			if( CompressedData[ Direction.Z ] == null )
				throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

			using var input = new MemoryStream( CompressedData[ Direction.Z ] );
			var inputWrapper = new StreamWrapper( input );
			var outputWrapper = new FullVolumeWriter( Metadata, Direction.Z, progress, ct );

			var error = ( VolumeError ) DecompressVolume( inputWrapper.Interop, outputWrapper.Interop );
			if( error != VolumeError.Success )
				throw new VolumeException( error, Resources.FormatResource<Volume>( "Decompression_ErrorText", error ) );

			return CreateUncompressed( Metadata, outputWrapper.GetData() );
		}

		/// <inheritdoc />
		/// <exception cref="NotSupportedException">The volume has no compressed data</exception>
		public override VolumeSliceCollection GetSliceRanges( IReadOnlyCollection<VolumeSliceRangeDefinition> ranges, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			if( ranges == null )
				throw new ArgumentNullException( nameof(ranges) );

			if( ranges.Count == 0 )
				return new VolumeSliceCollection();

			var combinedRanges = ranges.Merge().ToArray();

			//Partial scan in directed compressed volumes
			if( combinedRanges.All( r => CompressedData[ r.Direction ] != null ) &&
			    combinedRanges.Length <= Constants.RangeNumberLimitForEfficientScan &&
			    combinedRanges.Sum( r => r.Length ) < Constants.SliceNumberLimitForEfficientScan )
			{
				return new VolumeSliceCollection( ranges.Select( range => GetSliceRange( range ) ) );
			}

			var direction = combinedRanges.First().Direction;

			//Full scan in directed volume in case all ranges have this direction. Scan performance is the same, but copy is more efficient
			if( CompressedData[ direction ] == null || combinedRanges.Any( r => r.Direction != direction ) )
				direction = Direction.Z;

			if( CompressedData[ direction ] == null )
				throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

			using var input = new MemoryStream( CompressedData[ direction ], false );
			var inputWrapper = new StreamWrapper( input );
			var rangeReader = new VolumeSliceRangeCollector( Metadata, direction, combinedRanges, progress, ct );

			var error = ( VolumeError ) DecompressVolume( inputWrapper.Interop, rangeReader.Interop );
			if( error != VolumeError.Success )
				throw new VolumeException( error, Resources.FormatResource<Volume>( "Decompression_ErrorText", error ) );

			return rangeReader.GetSliceRangeCollection();
		}

		/// <inheritdoc />
		/// <exception cref="NotSupportedException">The volume has no compressed data</exception>
		public override VolumeSliceRange GetSliceRange( VolumeSliceRangeDefinition range, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			if( CompressedData[ range.Direction ] != null )
			{
				using var input = new MemoryStream( CompressedData[ range.Direction ], false );
				var inputWrapper = new StreamWrapper( input );
				var rangeReader = new VolumeSliceRangeCollector( Metadata, range.Direction, new[] { range }, progress, ct );

				var error = ( VolumeError ) DecompressSlices( inputWrapper.Interop, rangeReader.Interop, range.First, range.Length );
				if( error != VolumeError.Success )
					throw new VolumeException( error, Resources.FormatResource<Volume>( "Decompression_ErrorText", error ) );

				return rangeReader.GetSliceRange( range );
			}

			if( CompressedData[ Direction.Z ] == null )
				throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

			using( var input = new MemoryStream( CompressedData[ Direction.Z ], false ) )
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
		public override VolumeSlice GetSlice( VolumeSliceDefinition slice, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			//Videos with a very small number of frames appearantly have issues with av_seek, so we do a full scan instead
			if( CompressedData[ slice.Direction ] != null )
			{
				using var input = new MemoryStream( CompressedData[ slice.Direction ] );
				var inputWrapper = new StreamWrapper( input );
				var outputWrapper = new VolumeSliceRangeCollector( Metadata, slice.Direction, new[] { new VolumeSliceRangeDefinition( slice.Direction, slice.Index, slice.Index ) }, progress, ct );

				var error = ( VolumeError ) DecompressSlices( inputWrapper.Interop, outputWrapper.Interop, slice.Index, 1 );
				if( error != VolumeError.Success )
					throw new VolumeException( error, Resources.FormatResource<Volume>( "Decompression_ErrorText", error ) );

				return outputWrapper.GetSlice( slice.Direction, slice.Index );
			}

			if( CompressedData[ Direction.Z ] == null )
				throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

			using( var input = new MemoryStream( CompressedData[ Direction.Z ] ) )
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

			using var zipOutput = new ZipArchive( stream, ZipArchiveMode.Create );
			
			var metaDataEntry = zipOutput.CreateNormalizedEntry( "Metadata.xml", CompressionLevel.Optimal );
			using( var entryStream = metaDataEntry.Open() )
			{
				Metadata.Serialize( entryStream );
			}

			if( CompressionOptions != null )
			{
				var compressionOptionsEntry = zipOutput.CreateNormalizedEntry( "CompressionOptions.xml", CompressionLevel.Optimal );
				using var entryStream = compressionOptionsEntry.Open();
				
				CompressionOptions.Serialize( entryStream );
			}

			if( CompressedData[ Direction.Z ] == null )
				throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

			var zEntry = zipOutput.CreateNormalizedEntry( "VoxelsZ.dat", CompressionOptions.Encoder == BlockVolume.EncoderID ? CompressionLevel.Optimal : CompressionLevel.NoCompression );
			using( var entryStream = zEntry.Open() )
			{
				entryStream.Write( CompressedData[ Direction.Z ], 0, CompressedData[ Direction.Z ].Length );
			}

			if( CompressedData[ Direction.X ] != null )
			{
				var xEntry = zipOutput.CreateNormalizedEntry( "VoxelsX.dat", CompressionLevel.NoCompression );
				using var entryStream = xEntry.Open();
				
				entryStream.Write( CompressedData[ Direction.X ], 0, CompressedData[ Direction.X ].Length );
			}

			if( CompressedData[ Direction.Y ] != null )
			{
				var yEntry = zipOutput.CreateNormalizedEntry( "VoxelsY.dat", CompressionLevel.NoCompression );
				using var entryStream = yEntry.Open();
				
				entryStream.Write( CompressedData[ Direction.Y ], 0, CompressedData[ Direction.Y ].Length );
			}
		}

		#endregion
	}
}