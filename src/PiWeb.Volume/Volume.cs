#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume
{
	#region usings

	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.IO.Compression;
	using System.Threading;
	using Zeiss.PiWeb.Volume.Block;

	#endregion

	#region usings

	#endregion

	/// <summary>
	/// This is the base class for all volume implementations.
	/// </summary>
	/// <seealso cref="CompressedVolume"/>
	/// <seealso cref="UncompressedVolume"/>
	/// <seealso cref="StreamedVolume"/>
	/// <seealso cref="BlockVolume"/>
	public abstract class Volume
	{
		#region members

		/// <summary>
		/// The current volume library version. This version is stored in the volume file when it's saved.
		/// </summary>
		public static readonly Version FileVersion = new Version( 1, 0, 0, 0 );

		#endregion

		#region constructors

		internal Volume( VolumeMetadata metadata )
		{
			Metadata = metadata;
		}

		#endregion

		#region properties

		/// <summary>
		/// Describes the volumes size and resolution
		/// </summary>
		public VolumeMetadata Metadata { get; }

		#endregion

		#region methods

		/// <summary>
		/// Creates a new, decompressed volume from the specified data
		/// </summary>
		/// <param name="metadata">Describes the volumes size and resolution</param>
		/// <param name="slices">The decompressed volume as 8-Bit grayscale values. The array dimensions must match the specified <paramref name="metadata"/> (byte[z][x*y]).</param>
		/// <param name="logger">The logger to use for log messages</param>
		/// <exception cref="IndexOutOfRangeException">The specified data did not match the dimensions of the specified <paramref name="metadata"/>.</exception>
		public static UncompressedVolume CreateUncompressed( VolumeMetadata metadata, IReadOnlyList<VolumeSlice> slices, ILogger? logger = null )
		{
			var sw = Stopwatch.StartNew();

			var result = new UncompressedVolume( metadata, slices );
			logger?.Log( LogLevel.Info, $"Created uncompressed volume {sw.ElapsedMilliseconds} ms." );

			return result;
		}

		/// <summary>
		/// Creates a new, decompressed volume from the specified data
		/// </summary>
		/// <param name="metadata">Describes the volumes size and resolution</param>
		/// <param name="slices">The decompressed volume as 8-Bit grayscale values. The array dimensions must match the specified <paramref name="metadata"/> (byte[z][x*y]).</param>
		/// <param name="multiDirection"></param>
		/// <param name="progress">A progress indicator, which reports the current slice number.</param>
		/// <param name="options">Codec settings</param>
		/// <param name="logger">The logger to use for log messages</param>
		/// <param name="ct">The cancellation token to cancel the operation</param>
		/// <exception cref="IndexOutOfRangeException">The specified data did not match the dimensions of the specified <paramref name="metadata"/>.</exception>
		/// <exception cref="VolumeException">Error during encoding</exception>
		public static CompressedVolume CreateCompressed(
			VolumeMetadata metadata,
			IReadOnlyList<VolumeSlice> slices,
			VolumeCompressionOptions options,
			bool multiDirection = false,
			IProgress<VolumeSliceDefinition>? progress = null,
			ILogger? logger = null,
			CancellationToken ct = default )
		{
			var volume = new UncompressedVolume( metadata, slices );
			return volume.Compress( options, multiDirection, progress, logger, ct );
		}

		/// <summary>
		/// Creates a new, decompressed volume from the specified data
		/// </summary>
		/// <param name="metadata">Holds additional information about a volume</param>
		/// <param name="stream">A stream to the slice data</param>
		/// <param name="options">Codec settings</param>
		/// <param name="progress">A progress indicator, which reports the current slice number.</param>
		/// <param name="logger">The logger to use for log messages</param>
		/// <exception cref="VolumeException">Error during encoding</exception>
		public static Volume CreateCompressed(
			VolumeMetadata metadata,
			Stream stream,
			VolumeCompressionOptions options,
			IProgress<VolumeSliceDefinition>? progress = null,
			ILogger? logger = null )
		{
			var sw = Stopwatch.StartNew();
			try
			{
				if( options.Encoder == BlockVolume.EncoderID )
					return new BlockVolume( stream, metadata, options, progress );

				return new CompressedVolume( stream, metadata, options, progress );
			}
			finally
			{
				logger?.Log( LogLevel.Info, $"Compressed volume from stream in {sw.ElapsedMilliseconds} ms." );
			}
		}

		/// <summary>
		/// Returns the volumes accessibility in the specified direction.
		/// </summary>
		/// <param name="direction">The direction</param>
		/// <returns></returns>
		public abstract VolumeCompressionState GetCompressionState( Direction direction );

		/// <summary>
		/// Extracts the specified slice ranges. This is usually a lot faster than extracting single slices or ranges and consumes less memory than a full decompression.
		/// </summary>
		/// <param name="ranges">The required slice ranges.</param>
		/// <param name="progress">A progress indicator, which reports the current slice number.</param>
		/// <param name="logger">The logger to use for log messages</param>
		/// <param name="ct">The cancellation token to cancel the operation</param>
		/// <returns>An enumeration of slice ranges or an empty enumeration.</returns>
		/// <exception cref="VolumeException">Error during decoding</exception>
		public abstract VolumeSliceCollection GetSliceRanges(
			IReadOnlyCollection<VolumeSliceRangeDefinition> ranges,
			IProgress<VolumeSliceDefinition>? progress = null,
			ILogger? logger = null,
			CancellationToken ct = default );

		/// <summary>
		/// Gets the specified slice range. This is usually a lot faster than extracting single slices and consumes less memory than a full decompression.
		/// </summary>
		/// <param name="range">The requested slice range.</param>
		/// <param name="progress">A progress indicator, which reports the current slice number.</param>
		/// <param name="logger">The logger to use for log messages</param>
		/// <param name="ct">The cancellation token to cancel the operation</param>
		/// <returns></returns>
		/// <exception cref="VolumeException">Error during decoding</exception>
		public abstract VolumeSliceRange GetSliceRange(
			VolumeSliceRangeDefinition range,
			IProgress<VolumeSliceDefinition>? progress = null,
			ILogger? logger = null,
			CancellationToken ct = default );

		/// <summary>
		/// Gets the specified slice. This is the most memory friendly and usually the fastest approach to get a single slice.
		/// </summary>
		/// <param name="slice">The requested slice.</param>
		/// <param name="sliceBuffer">The slice buffer that will be filled with the slice data.</param>
		/// <param name="progress">A progress indicator, which reports the current slice number.</param>
		/// <param name="logger">The logger to use for log messages</param>
		/// <param name="ct">The cancellation token to cancel the operation</param>
		/// <exception cref="VolumeException">Error during decoding</exception>
		public abstract void GetSlice(
			VolumeSliceDefinition slice,
			byte[] sliceBuffer,
			IProgress<VolumeSliceDefinition>? progress = null,
			ILogger? logger = null,
			CancellationToken ct = default );

		/// <summary>
		/// Gets the specified slice.
		/// </summary>
		/// <param name="slice">The requested slice.</param>
		/// <param name="progress">A progress indicator, which reports the current slice number.</param>
		/// <param name="logger">The logger to use for log messages</param>
		/// <param name="ct">The cancellation token to cancel the operation</param>
		/// <exception cref="VolumeException">Error during decoding</exception>
		public VolumeSlice GetSlice(
			VolumeSliceDefinition slice,
			IProgress<VolumeSliceDefinition>? progress = null,
			ILogger? logger = null,
			CancellationToken ct = default )
		{
			var sliceBuffer = new byte[ Metadata.GetSliceLength( slice.Direction ) ];

			GetSlice( slice, sliceBuffer, progress, logger, ct );

			return new VolumeSlice( slice, sliceBuffer );
		}

		/// <summary>
		/// Creates a smaller volume from the current volume without performing a full decompression.
		/// </summary>
		/// <param name="minification">The stride factor in X, Y and Z direction. The size of the preview will be the CompleteSize divided by <paramref name="minification"/> ^ 3</param>
		/// <param name="progress">A progress indicator, which reports the current slice number.</param>
		/// <param name="logger">The logger to use for log messages</param>
		/// <param name="ct">The cancellation token to cancel the operation</param>
		/// <exception cref="VolumeException">Error during decoding</exception>
		public abstract UncompressedVolume CreatePreview(
			ushort minification,
			IProgress<VolumeSliceDefinition>? progress = null,
			ILogger? logger = null,
			CancellationToken ct = default );

		internal static void GetEncodedSliceSize( VolumeMetadata metadata, Direction direction, out ushort x, out ushort y )
		{
			switch( direction )
			{
				case Direction.X:
					x = (ushort)Math.Max( ( metadata.SizeY + Constants.EncodingBlockSize - 1 ) / Constants.EncodingBlockSize * Constants.EncodingBlockSize, Constants.MinimumEncodingSize );
					y = (ushort)Math.Max( ( metadata.SizeZ + Constants.EncodingBlockSize - 1 ) / Constants.EncodingBlockSize * Constants.EncodingBlockSize, Constants.MinimumEncodingSize );
					break;
				case Direction.Y:
					x = (ushort)Math.Max( ( metadata.SizeX + Constants.EncodingBlockSize - 1 ) / Constants.EncodingBlockSize * Constants.EncodingBlockSize, Constants.MinimumEncodingSize );
					y = (ushort)Math.Max( ( metadata.SizeZ + Constants.EncodingBlockSize - 1 ) / Constants.EncodingBlockSize * Constants.EncodingBlockSize, Constants.MinimumEncodingSize );
					break;
				case Direction.Z:
					x = (ushort)Math.Max( ( metadata.SizeX + Constants.EncodingBlockSize - 1 ) / Constants.EncodingBlockSize * Constants.EncodingBlockSize, Constants.MinimumEncodingSize );
					y = (ushort)Math.Max( ( metadata.SizeY + Constants.EncodingBlockSize - 1 ) / Constants.EncodingBlockSize * Constants.EncodingBlockSize, Constants.MinimumEncodingSize );
					break;
				default:
					throw new ArgumentOutOfRangeException( nameof( direction ), direction, null );
			}
		}

		/// <summary>
		/// Loads volume data from the specified data stream.
		/// </summary>
		/// <exception cref="VolumeException">Error during decoding</exception>
		/// <exception cref="NotSupportedException">The volume has no compressed data</exception>
		/// <exception cref="InvalidOperationException">One or more entries are missing in the archive.</exception>
		public static CompressedVolume Load( Stream stream, ILogger? logger = null )
		{
			var sw = Stopwatch.StartNew();
			try
			{
				if( !stream.CanSeek )
					stream = new MemoryStream( stream.StreamToArray() );

				using var archive = new ZipArchive( stream );

				var metaData = ReadVolumeMetadata( archive );
				var compressionOptions = ReadVolumeCompressionOptions( archive );
				var directionMap = ReadVolumeVoxels( archive );

				if( compressionOptions.Encoder == BlockVolume.EncoderID )
					return new BlockVolume( metaData, compressionOptions, directionMap );

				return new CompressedVolume( metaData, compressionOptions, directionMap );
			}
			finally
			{
				logger?.Log( LogLevel.Info, $"Loaded volume from stream in {sw.ElapsedMilliseconds} ms." );
			}
		}

		private static DirectionMap ReadVolumeVoxels( ZipArchive archive )
		{
			var directionMap = new DirectionMap();

			if( ReadVoxelData( archive.GetEntry( "Voxels.dat" ), Direction.Z, directionMap ) )
				return directionMap;

			if( !ReadVoxelData( archive.GetEntry( "VoxelsZ.dat" ), Direction.Z, directionMap ) )
				throw new InvalidOperationException( Resources.FormatResource<Volume>( "InvalidFormatMissingFile_ErrorText", "Voxels.dat" ) );

			ReadVoxelData( archive.GetEntry( "VoxelsY.dat" ), Direction.Y, directionMap );
			ReadVoxelData( archive.GetEntry( "VoxelsX.dat" ), Direction.X, directionMap );

			return directionMap;
		}

		private static bool ReadVoxelData( ZipArchiveEntry? dataEntry, Direction direction, DirectionMap directionMap )
		{
			if( dataEntry == null )
				return false;

			using var entryStream = dataEntry.Open();
			directionMap[ direction ] = entryStream.StreamToArray( (int)dataEntry.Length );

			return true;
		}

		private static VolumeMetadata ReadVolumeMetadata( ZipArchive archive )
		{
			var metaDataEntry = archive.GetEntry( "Metadata.xml" );
			if( metaDataEntry == null )
				throw new InvalidOperationException( Resources.FormatResource<Volume>( "InvalidFormatMissingFile_ErrorText", "Metadata.xml" ) );

			using var entryStream = metaDataEntry.Open();
			var metaData = VolumeMetadata.Deserialize( entryStream );

			if( metaData.FileVersion > FileVersion )
				throw new InvalidOperationException( Resources.FormatResource<Volume>( "FileVersionError_Text", metaData.FileVersion, FileVersion ) );

			return metaData;
		}

		private static VolumeCompressionOptions ReadVolumeCompressionOptions( ZipArchive archive )
		{
			var compressionOptionsEntry = archive.GetEntry( "CompressionOptions.xml" );

			if( compressionOptionsEntry == null )
				throw new InvalidOperationException( Resources.FormatResource<Volume>( "InvalidFormatMissingFile_ErrorText", "CompressionOptions.xml" ) );

			using var entryStream = compressionOptionsEntry.Open();
			return VolumeCompressionOptions.Deserialize( entryStream );
		}

		#endregion
	}
}