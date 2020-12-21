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
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Zeiss.IMT.PiWeb.Volume.Block;

	#endregion

	/// <summary>
	/// An uncompressed volume. This volume is optimized for speed to make the access
	/// to slices as fast as possible. The tradeoff for speed is memory.  
	/// </summary>
	/// <seealso cref="CompressedVolume"/>
	public sealed class UncompressedVolume : Volume
	{
		#region members

		private readonly IReadOnlyList<VolumeSlice> _Slices;

		#endregion

		#region constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="UncompressedVolume" /> class.
		/// </summary>
		/// <param name="metadata">The metadata.</param>
		/// <param name="slices">The grayscale slice data.</param>
		public UncompressedVolume( VolumeMetadata metadata, IReadOnlyList<VolumeSlice> slices ) 
			: base( metadata )
		{
			_Slices = slices;
			CheckForIntegrity();
		}

		#endregion

		#region methods

		/// <summary>
		/// Checks the data for integrity. Works only on decompressed data. Will throw exceptions in case a check failed.
		/// </summary>
		/// <exception cref="IndexOutOfRangeException"></exception>
		private void CheckForIntegrity()
		{
			if( _Slices.Count != Metadata.SizeZ  )
				throw new VolumeIntegrityException( $"Invalid number of slices (expected: {Metadata.SizeZ}, but was {_Slices.Count})." );

			var expectedSliceSize = Metadata.SizeX * Metadata.SizeY;
			var index = 0;
			foreach( var slice in _Slices )
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
		/// <exception cref="VolumeException">Error during encoding</exception>
		public CompressedVolume Compress( VolumeCompressionOptions options, bool multiDirection = false, IProgress<VolumeSliceDefinition> progress = null, ILogger logger = null, CancellationToken ct = default )
		{
			var sw = Stopwatch.StartNew();
			try
			{
				if( options.Encoder == BlockVolume.EncoderID )
					return new BlockVolume( _Slices, Metadata, options, progress );

				var directionMap = new DirectionMap { [ Direction.Z ] = CompressDirection( Direction.Z, options, progress, logger, ct ) };
				if( multiDirection )
				{
					directionMap[ Direction.X ] = CompressDirection( Direction.X, options, progress, logger, ct );
					directionMap[ Direction.Y ] = CompressDirection( Direction.Y, options, progress, logger, ct );
				}

				return new CompressedVolume( Metadata, options, directionMap );
			}
			finally
			{
				logger?.Log( LogLevel.Info, $"Compressed volume with encoder '{options.Encoder}' in {sw.ElapsedMilliseconds} ms." );
			}
		}

		/// <summary>
		/// Compresses the volume with the specified compression options.
		/// </summary>
		/// <exception cref="VolumeException">Error during encoding</exception>
		/// <exception cref="NotSupportedException">The volume has no decompressed data</exception>
		private byte[] CompressDirection( Direction direction, VolumeCompressionOptions options, IProgress<VolumeSliceDefinition> progress = null, ILogger logger = null, CancellationToken ct = default )
		{
			var sw = Stopwatch.StartNew();
			
			using var outputStream = new MemoryStream();
			GetEncodedSliceSize( Metadata, direction, out var encodingSizeX, out var encodingSizeY );

			var inputStreamWrapper = new SliceReader( Metadata, _Slices, direction, progress, ct );
			var outputStreamWrapper = new StreamWrapper( outputStream );

			var error = NativeMethods.CompressVolume( inputStreamWrapper.Interop, outputStreamWrapper.Interop, encodingSizeX, encodingSizeY, options.Encoder, options.PixelFormat, options.GetOptionsString(), options.Bitrate );
			if( error != VolumeError.Success )
				throw new VolumeException( error, Resources.FormatResource<Volume>( "Compression_ErrorText", error ) );

			var result = outputStream.ToArray();
			logger?.Log( LogLevel.Debug, $"Compressed direction {direction} with encoder {options.Encoder} in {sw.ElapsedMilliseconds} ms." );
			
			return result;
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
			
			var result = PreviewCreator.CreatePreview( _Slices, Metadata, minification );
			logger?.Log( LogLevel.Info, $"Created a preview with minification factor {minification} in {sw.ElapsedMilliseconds} ms." );

			return result;
		}

		/// <inheritdoc />
		public override VolumeSliceCollection GetSliceRanges( IReadOnlyCollection<VolumeSliceRangeDefinition> ranges, IProgress<VolumeSliceDefinition> progress = null, ILogger logger = null, CancellationToken ct = default )
		{
			if( ranges == null )
				throw new ArgumentNullException( nameof(ranges) );

			var sw = Stopwatch.StartNew();

			var result = new VolumeSliceCollection( ranges.Select( range => GetSliceRange( range, progress, logger, ct ) ) );
			logger?.Log( LogLevel.Info, $"Extracted '{ranges.Count}' slice ranges in {sw.ElapsedMilliseconds} ms." );

			return result;
		}

		/// <inheritdoc />
		public override VolumeSliceRange GetSliceRange( VolumeSliceRangeDefinition range, IProgress<VolumeSliceDefinition> progress = null, ILogger logger = null, CancellationToken ct = default )
		{
			var sw = Stopwatch.StartNew();

			var sliceRange = new List<VolumeSlice>( range.Length );
			sliceRange.AddRange( range.Select( definition => GetSlice( definition, progress, logger ) ) );

			var result = new VolumeSliceRange( range, sliceRange );
			logger?.Log( LogLevel.Info, $"Extracted '{range}' in {sw.ElapsedMilliseconds} ms." );
	
			return result;
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

			var bufferSize = sy * sz;
			var result = new byte[bufferSize];

			Parallel.For( 0, sz, z =>
			{
				for( var y = 0; y < sy; y++ )
				{
					result[ z * sy + y ] = _Slices[ z ].Data[ y * sx + index ];
				}
			} );

			return new VolumeSlice( Direction.X, index, result );
		}

		private VolumeSlice ReadSliceY( ushort index )
		{
			var sx = Metadata.SizeX;
			var sz = Metadata.SizeZ;

			var bufferSize = sx * sz;
			var result = new byte[bufferSize];

			Parallel.For( 0, sz, z => { Array.Copy( _Slices[ z ].Data, index * sx, result, z * sx, sx ); } );

			return new VolumeSlice( Direction.Y, index, result );
		}

		private VolumeSlice ReadSliceZ( ushort index )
		{
			return new VolumeSlice( Direction.Z, index, _Slices[ index ].Data );
		}

		/// <summary>
		/// Compresses and saves the volume in the specified stream.
		/// </summary>
		public void Save( Stream stream, VolumeCompressionOptions options, bool multiDirection = false, IProgress<VolumeSliceDefinition> progress = null, ILogger logger = null, CancellationToken ct = default )
		{
			var sw = Stopwatch.StartNew();
			try
			{
				var compressed = Compress( options, multiDirection, progress, logger, ct );
				compressed.Save( stream );
			}
			finally
			{
				logger?.Log( LogLevel.Info, $"Saved volume to stream in {sw.ElapsedMilliseconds} ms." );
			}
		}

		#endregion
	}
}