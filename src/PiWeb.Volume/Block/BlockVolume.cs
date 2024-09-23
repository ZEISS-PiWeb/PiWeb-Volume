#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Block;

#region usings

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

#endregion

/// <summary>
/// A Volume that is not compressed in slices, but in blocks. This is optimal for performance/memory tradeoff.
/// </summary>
internal class BlockVolume : CompressedVolume
{
	#region constants

	/// <summary>
	/// Side length of a block.
	/// </summary>
	internal const int N = 8;

	internal const int N2 = N * N;
	internal const int N3 = N * N * N;

	/// <summary>
	/// The id of the block volume encoder.
	/// </summary>
	internal const string EncoderID = "zeiss.block";

	/// <summary>
	///  The pixel format of the block volume encoder.
	/// </summary>
	internal const string PixelFormat = "gray8";

	/// <summary>
	/// File header to identify block volumes. Reads as JSVF.
	/// </summary>
	public const uint FileHeader = 0x4A535646;

	/// <summary>
	/// The current file version.
	/// </summary>
	public const uint Version = 0x00000001;

	#endregion

	#region constructors

	internal BlockVolume( VolumeMetadata metadata, VolumeCompressionOptions options, DirectionMap compressedData )
		: base( metadata, options, compressedData )
	{ }

	internal BlockVolume( Stream input, VolumeMetadata metadata, VolumeCompressionOptions options, IProgress<VolumeSliceDefinition>? progress )
		: base( metadata, options, CreateDirectionMap( input, metadata, options, progress ) )
	{ }

	internal BlockVolume( IReadOnlyList<VolumeSlice> slices, VolumeMetadata metadata, VolumeCompressionOptions options, IProgress<VolumeSliceDefinition>? progress )
		: base( metadata, options, CreateDirectionMap( slices, metadata, options, progress ) )
	{ }

	#endregion

	#region methods

	private static DirectionMap CreateDirectionMap(
		IReadOnlyList<VolumeSlice> slices,
		VolumeMetadata metadata,
		VolumeCompressionOptions options,
		IProgress<VolumeSliceDefinition>? progress )
	{
		var output = new MemoryStream();

		BlockVolumeEncoder.Encode( slices, output, metadata, options, progress );

		return new DirectionMap { [ Direction.Z ] = output.ToArray() };
	}

	private static DirectionMap CreateDirectionMap( Stream input, VolumeMetadata metadata, VolumeCompressionOptions options, IProgress<VolumeSliceDefinition>? progress )
	{
		var estimate = ( (long)metadata.SizeX * metadata.SizeY * metadata.SizeZ ) / N2;
		var output = new MemoryStream( (int)estimate );

		BlockVolumeEncoder.Encode( input, output, metadata, options, progress );

		return new DirectionMap { [ Direction.Z ] = output.ToArray() };
	}

	/// <inheritdoc />
	public override UncompressedVolume Decompress( IProgress<VolumeSliceDefinition>? progress = null, ILogger? logger = null, CancellationToken ct = default )
	{
		var sw = Stopwatch.StartNew();
		try
		{
			var decompressor = new BlockVolumeDecompressor( this );
			var data = decompressor.Decompress( progress, ct );

			return new UncompressedVolume( Metadata, data );
		}
		finally
		{
			logger?.Log( LogLevel.Info, $"Decompressed block volume in {sw.ElapsedMilliseconds} ms." );
		}
	}

	internal static (ushort X, ushort Y, ushort Z) GetBlockCount( BlockVolumeMetaData metaData )
	{
		return GetBlockCount( metaData.SizeX, metaData.SizeY, metaData.SizeZ );
	}

	internal static (ushort X, ushort Y, ushort Z) GetBlockCount( ushort sizeX, ushort sizeY, ushort sizeZ )
	{
		var bcz = sizeZ / N;
		if( bcz * N < sizeZ )
			bcz++;

		var bcy = sizeY / N;
		if( bcy * N < sizeY )
			bcy++;

		var bcx = sizeX / N;
		if( bcx * N < sizeX )
			bcx++;

		return ( (ushort)bcx, (ushort)bcy, (ushort)bcz );
	}

	/// <inheritdoc />
	public override VolumeCompressionState GetCompressionState( Direction direction )
	{
		if( CompressedData[ Direction.Z ] == null )
			throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

		return VolumeCompressionState.CompressedInDirection;
	}

	/// <inheritdoc />
	public override UncompressedVolume CreatePreview( ushort minification, IProgress<VolumeSliceDefinition>? progress = null, ILogger? logger = null, CancellationToken ct = default )
	{
		if( CompressedData[ Direction.Z ] == null )
			throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

		var sw = Stopwatch.StartNew();

		var previewCreator = new BlockVolumePreviewCreator( this, minification );
		var result = previewCreator.CreatePreview( progress, ct );
		logger?.Log( LogLevel.Info, $"Created a preview with minification factor {minification} in {sw.ElapsedMilliseconds} ms." );

		return result;
	}

	/// <inheritdoc />
	public override void GetSlice(
		VolumeSliceDefinition slice,
		byte[] buffer,
		IProgress<VolumeSliceDefinition>? progress = null,
		ILogger? logger = null,
		CancellationToken ct = default )
	{
		var sw = Stopwatch.StartNew();
		var sliceRangeCollector = new BlockVolumeSliceRangeCollector( this, slice, buffer );
		sliceRangeCollector.CollectSliceRanges( progress, ct );

		logger?.Log( LogLevel.Info, $"Extracted '{slice}' in {sw.ElapsedMilliseconds} ms." );
	}

	/// <inheritdoc />
	public override VolumeSliceRange GetSliceRange( VolumeSliceRangeDefinition range, IProgress<VolumeSliceDefinition>? progress = null, ILogger? logger = null, CancellationToken ct = default )
	{
		var sw = Stopwatch.StartNew();

		var sliceRangeCollector = new BlockVolumeSliceRangeCollector( this, new[] { range } );
		var data = sliceRangeCollector.CollectSliceRanges( progress, ct );

		var result = data.GetSliceRange( range );
		logger?.Log( LogLevel.Info, $"Extracted '{range}' in {sw.ElapsedMilliseconds} ms." );

		return result;
	}

	/// <inheritdoc />
	public override VolumeSliceCollection GetSliceRanges( IReadOnlyCollection<VolumeSliceRangeDefinition> ranges, IProgress<VolumeSliceDefinition>? progress = null, ILogger? logger = null, CancellationToken ct = default )
	{
		var sw = Stopwatch.StartNew();

		var sliceRangeCollector = new BlockVolumeSliceRangeCollector( this, ranges );
		var result = sliceRangeCollector.CollectSliceRanges( progress, ct );
		logger?.Log( LogLevel.Info, $"Extracted '{ranges.Count}' slice ranges in {sw.ElapsedMilliseconds} ms." );

		return result;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"Block volume {Metadata} [{CompressedData}]";
	}

	#endregion
}