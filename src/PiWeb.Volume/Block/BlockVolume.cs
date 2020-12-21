#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.Block
{
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

		internal const int N = 8;
		internal const int N2 = N * N;
		internal const int N3 = N * N * N;

		internal const string EncoderID = "zeiss.block";

		#endregion

		#region constructors

		internal BlockVolume( VolumeMetadata metadata, VolumeCompressionOptions options, DirectionMap compressedData )
			: base( metadata, options, compressedData )
		{ }

		internal BlockVolume( Stream input, VolumeMetadata metadata, VolumeCompressionOptions options, IProgress<VolumeSliceDefinition> progress )
			: base( metadata, options, CreateDirectionMap( input, metadata, options, progress ) )
		{ }

		internal BlockVolume( IReadOnlyList<VolumeSlice> slices, VolumeMetadata metadata, VolumeCompressionOptions options, IProgress<VolumeSliceDefinition> progress )
			: base( metadata, options, CreateDirectionMap( slices, metadata, options, progress ) )
		{ }

		private static DirectionMap CreateDirectionMap( IReadOnlyList<VolumeSlice> slices, VolumeMetadata metadata, VolumeCompressionOptions options, IProgress<VolumeSliceDefinition> progress )
		{
			var encoder = new BlockVolumeEncoder( options );
			var output = new MemoryStream();

			encoder.Encode( slices, output, metadata, progress );

			return new DirectionMap { [ Direction.Z ] = output.ToArray() };
		}

		private static DirectionMap CreateDirectionMap( Stream input, VolumeMetadata metadata, VolumeCompressionOptions options, IProgress<VolumeSliceDefinition> progress )
		{
			var encoder = new BlockVolumeEncoder( options );
			var output = new MemoryStream();

			encoder.Encode( input, output, metadata, progress );
			
			return new DirectionMap { [ Direction.Z ] = output.ToArray() };
		}

		#endregion

		#region methods

		/// <inheritdoc />
		public override UncompressedVolume Decompress( IProgress<VolumeSliceDefinition> progress = null, ILogger logger = null, CancellationToken ct = default )
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

		internal static (ushort X, ushort Y, ushort Z) GetBlockCount( VolumeMetadata metadata )
		{
			var bcz = metadata.SizeZ / N;
			if( bcz * N < metadata.SizeZ )
				bcz++;

			var bcy = metadata.SizeY / N;
			if( bcy * N < metadata.SizeY )
				bcy++;

			var bcx = metadata.SizeX / N;
			if( bcx * N < metadata.SizeX )
				bcx++;

			return ( ( ushort ) bcx, ( ushort ) bcy, ( ushort ) bcz );
		}

		/// <inheritdoc />
		public override VolumeCompressionState GetCompressionState( Direction direction )
		{
			if( CompressedData[ Direction.Z ] == null )
				throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

			return VolumeCompressionState.CompressedInDirection;
		}

		/// <inheritdoc />
		public override UncompressedVolume CreatePreview( ushort minification, IProgress<VolumeSliceDefinition> progress = null, ILogger logger = null, CancellationToken ct = default )
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
		public override VolumeSlice GetSlice( VolumeSliceDefinition slice, IProgress<VolumeSliceDefinition> progress = null, ILogger logger = null, CancellationToken ct = default )
		{
			var sw = Stopwatch.StartNew();
			
			var sliceRangeCollector = new BlockVolumeSliceRangeCollector( this, new[] { new VolumeSliceRangeDefinition( slice.Direction, slice.Index, slice.Index ) } );
			var data = sliceRangeCollector.CollectSliceRanges( progress, ct );

			var result = data.GetSlice( slice.Direction, slice.Index );
			logger?.Log( LogLevel.Info, $"Extracted '{slice}' in {sw.ElapsedMilliseconds} ms." );

			return result;
		}

		/// <inheritdoc />
		public override VolumeSliceRange GetSliceRange( VolumeSliceRangeDefinition range, IProgress<VolumeSliceDefinition> progress = null, ILogger logger = null, CancellationToken ct = default )
		{
			var sw = Stopwatch.StartNew();

			var sliceRangeCollector = new BlockVolumeSliceRangeCollector( this, new[] { range } );
			var data = sliceRangeCollector.CollectSliceRanges( progress, ct );

			var result = data.GetSliceRange( range );
			logger?.Log( LogLevel.Info, $"Extracted '{range}' in {sw.ElapsedMilliseconds} ms." );

			return result;
		}

		/// <inheritdoc />
		public override VolumeSliceCollection GetSliceRanges( IReadOnlyCollection<VolumeSliceRangeDefinition> ranges, IProgress<VolumeSliceDefinition> progress = null, ILogger logger = null, CancellationToken ct = default )
		{
			var sw = Stopwatch.StartNew();

			var sliceRangeCollector = new BlockVolumeSliceRangeCollector( this, ranges );
			var result = sliceRangeCollector.CollectSliceRanges( progress, ct );
			logger?.Log( LogLevel.Info, $"Extracted '{ranges.Count}' slice ranges in {sw.ElapsedMilliseconds} ms." );

			return result;
		}

		#endregion
	}
}