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
	using System.IO;
	using System.Threading;

	#endregion

	/// <summary>
	/// A Volume that is not compressed in slices, but in blocks.
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
		{
		}

		#endregion

		#region methods

		/// <inheritdoc />
		public override UncompressedVolume Decompress( IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			var decompressor = new BlockVolumeDecompressor( this );
			var data = decompressor.Decompress( progress, ct );

			return new UncompressedVolume( Metadata, data );
		}

		internal static BlockVolume Create( Stream input, VolumeMetadata metadata, VolumeCompressionOptions options, IProgress<VolumeSliceDefinition> progress, CancellationToken ct )
		{
			var encoder = new BlockVolumeEncoder( options );
			var output = new MemoryStream();

			encoder.Encode( input, output, metadata, progress );

			var directionMap = new DirectionMap
			{
				[ Direction.Z ] = output.ToArray()
			};

			return new BlockVolume( metadata, options, directionMap );
		}

		internal static BlockVolume Create( byte[][] input, VolumeMetadata metadata, VolumeCompressionOptions options, IProgress<VolumeSliceDefinition> progress, CancellationToken ct )
		{
			var encoder = new BlockVolumeEncoder( options );
			var output = new MemoryStream();

			encoder.Encode( input, output, metadata, progress );

			var directionMap = new DirectionMap
			{
				[ Direction.Z ] = output.ToArray()
			};

			return new BlockVolume( metadata, options, directionMap );
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
		public override UncompressedVolume CreatePreview( ushort minification, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			if( CompressedData[ Direction.Z ] == null )
				throw new NotSupportedException( Resources.GetResource<Volume>( "CompressedDataMissing_ErrorText" ) );

			var previewCreator = new BlockVolumePreviewCreator( this, minification );
			return previewCreator.CreatePreview( progress, ct );
		}

		/// <inheritdoc />
		public override VolumeSlice GetSlice( VolumeSliceDefinition slice, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			var sliceRangeCollector = new BlockVolumeSliceRangeCollector( this, new[] { new VolumeSliceRangeDefinition( slice.Direction, slice.Index, slice.Index ) } );
			var data = sliceRangeCollector.CollectSliceRanges( progress, ct );

			return data.GetSlice( slice.Direction, slice.Index );
		}

		/// <inheritdoc />
		public override VolumeSliceRange GetSliceRange( VolumeSliceRangeDefinition range, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			var sliceRangeCollector = new BlockVolumeSliceRangeCollector( this, new[] { range } );
			var data = sliceRangeCollector.CollectSliceRanges( progress, ct );

			return data.GetSliceRange( range );
		}

		/// <inheritdoc />
		public override VolumeSliceCollection GetSliceRanges( IReadOnlyCollection<VolumeSliceRangeDefinition> ranges, IProgress<VolumeSliceDefinition> progress = null, CancellationToken ct = default )
		{
			var sliceRangeCollector = new BlockVolumeSliceRangeCollector( this, ranges );
			var data = sliceRangeCollector.CollectSliceRanges( progress, ct );

			return data;
		}

		#endregion
	}
}