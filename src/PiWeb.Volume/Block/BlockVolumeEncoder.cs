#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Block
{
	#region usings

	using System;
	using System.Buffers;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading.Tasks;

	#endregion

	internal class BlockVolumeEncoder
	{
		#region members

		private readonly VolumeCompressionOptions _Options;

		#endregion

		#region constructors

		internal BlockVolumeEncoder( VolumeCompressionOptions options )
		{
			_Options = options;
		}

		#endregion

		#region methods

		/// <summary>
		/// Encodes the specified input data which is a complete volume in z-slices.
		/// </summary>
		internal void Encode( IReadOnlyList<VolumeSlice> slices, Stream output, VolumeMetadata metadata, IProgress<VolumeSliceDefinition>? progress )
		{
			var quantization = Quantization.Calculate( _Options );

			Encode( EnumerateBlockLayersFromSlices( slices, metadata ), output, metadata, quantization, progress );
		}

		/// <summary>
		/// Encodes the input data that is coming from the specified input stream.
		/// </summary>
		internal void Encode( Stream input, Stream output, VolumeMetadata metadata, IProgress<VolumeSliceDefinition>? progress )
		{
			var quantization = Quantization.Calculate( _Options );
			Encode( EnumerateBlockLayersFromStream( input, metadata ), output, metadata, quantization, progress );
		}

		private static IEnumerable<byte[][]> EnumerateBlockLayersFromStream( Stream input, VolumeMetadata metadata )
		{
			var z = 0;
			var (_, _, layerCount) = BlockVolume.GetBlockCount( metadata );
			var layerBuffer = new byte[ BlockVolume.N ][];

			for( var i = 0; i < BlockVolume.N; i++ )
				layerBuffer[ i ] = new byte[ metadata.SizeX * metadata.SizeY ];

			for( var layerIndex = 0; layerIndex < layerCount; layerIndex++ )
			{
				for( var bz = 0; bz < BlockVolume.N && z < metadata.SizeZ; bz++, z++ )
					input.Read( layerBuffer[ bz ], 0, metadata.SizeX * metadata.SizeY );

				yield return layerBuffer;
			}
		}

		private static IEnumerable<byte[][]> EnumerateBlockLayersFromSlices( IReadOnlyList<VolumeSlice> slices, VolumeMetadata metadata )
		{
			var z = 0;
			var buffer = new byte[ BlockVolume.N ][];
			var (_, _, layerCount) = BlockVolume.GetBlockCount( metadata );

			for( var layerIndex = 0; layerIndex < layerCount; layerIndex++ )
			{
				for( var bz = 0; bz < BlockVolume.N && z < metadata.SizeZ; bz++, z++ )
					buffer[ bz ] = slices[ z ].Data;

				yield return buffer;
			}
		}

		private void Encode( IEnumerable<byte[][]> blockLayers, Stream output, VolumeMetadata metadata, double[] quantization, IProgress<VolumeSliceDefinition>? progress )
		{
			var (bcx, bcy, _) = BlockVolume.GetBlockCount( metadata );

			var blockCount = bcx * bcy;
			var inputBlocks = new double[ blockCount ][];
			var resultBlocks = new short[ blockCount ][];
			var resultLengths = new ushort[ blockCount ];

			for( var i = 0; i < blockCount; i++ )
			{
				inputBlocks[ i ] = new double[ BlockVolume.N3 ];
				resultBlocks[ i ] = new short[ BlockVolume.N3 ];
			}

			using var writer = new BinaryWriter( output );

			WriteMetadata( writer, quantization );
			var zigzag = ZigZag.Calculate();

			var blockIndexZ = (ushort)0;

			foreach( var blockLayer in blockLayers )
			{
				CreateLayer( blockLayer, inputBlocks, blockIndexZ, metadata );
				EncodeLayer( inputBlocks, resultBlocks, resultLengths, quantization, zigzag );

				for( var blockIndex = 0; blockIndex < blockCount; blockIndex++ )
				{
					var resultLength = resultLengths[ blockIndex ];
					var block = resultBlocks[ blockIndex ];

					var length = resultLength & 0x0FFF;
					var firstLength = ( resultLength & 0b0011000000000000 ) >> 12;
					var otherLength = ( resultLength & 0b1100000000000000 ) >> 14;

					writer.Write( resultLength );

					if( length > 0 )
						if( firstLength == 2 )
							writer.Write( block[ 0 ] );
						else
							writer.Write( (sbyte)block[ 0 ] );

					for( var i = 1; i < length; i++ )
						if( otherLength == 2 )
							writer.Write( block[ i ] );
						else
							writer.Write( (sbyte)block[ i ] );
				}

				progress?.Report( new VolumeSliceDefinition( Direction.Z, (ushort)( blockIndexZ * BlockVolume.N ) ) );

				blockIndexZ++;
			}
		}

		private static void WriteMetadata( BinaryWriter writer, double[] quantization )
		{
			//File header, read as 'JSVF'
			writer.Write( BlockVolume.FileHeader );

			//Version
			writer.Write( BlockVolume.Version );

			//Quantization
			Quantization.Write( writer, quantization );
		}

		private static void EncodeLayer(
			double[][] inputBlocks,
			short[][] resultBlocks,
			ushort[] resultLengths,
			double[] quantization,
			int[] zigzag
		)
		{
			Parallel.For( 0, inputBlocks.Length,
				() => ArrayPool<double>.Shared.Rent( BlockVolume.N3 ),
				( blockIndex, _, buffer ) =>
				{
					var inputBlock = inputBlocks[ blockIndex ];
					var resultBlock = resultBlocks[ blockIndex ];

					//1. Cosine transform
					DiscreteCosineTransform.Transform( inputBlock, buffer );

					//2. Quantization
					for( var i = 0; i < BlockVolume.N3; i++ )
						buffer[ i ] *= quantization[ i ];

					//3. ZigZag
					for( var i = 0; i < BlockVolume.N3; i++ )
						inputBlock[ i ] = buffer[ zigzag[ i ] ];

					//4. Discretization
					for( var i = 0; i < BlockVolume.N3; i++ )
						resultBlock[ i ] = (short)Math.Max( short.MinValue, Math.Min( short.MaxValue, Math.Round( inputBlock[ i ] ) ) );

					var count = 0;
					var isFirstValueShort = resultBlock[ 0 ] is > sbyte.MaxValue or < sbyte.MinValue;
					var areOtherValuesShort = false;

					for( var i = 0; i < BlockVolume.N3; i++ )
					{
						var value = resultBlock[ i ];
						if( value != 0 )
							count = i + 1;

						if( i > 0 && ( value is > sbyte.MaxValue or < sbyte.MinValue ) )
							areOtherValuesShort = true;
					}

					//resultLength has 16 bits:
					//Bit  0 - 11: index of last value that is greater than 0
					//Bit 12 - 13: number of bytes of the first value of the block
					//Bit 14 - 15: number of bytes of the other values of the block
					resultLengths[ blockIndex ] = (ushort)( count & 0x0FFF );
					resultLengths[ blockIndex ] = (ushort)( resultLengths[ blockIndex ] | ( isFirstValueShort ? 2 << 12 : 1 << 12 ) );
					resultLengths[ blockIndex ] = (ushort)( resultLengths[ blockIndex ] | ( areOtherValuesShort ? 2 << 14 : 1 << 14 ) );

					return buffer;
				}, buffer => ArrayPool<double>.Shared.Return( buffer ) );
		}

		private static void CreateLayer( byte[][] layer, double[][] blocks, ushort blockIndexZ, VolumeMetadata metadata )
		{
			var sx = metadata.SizeX;
			var sy = metadata.SizeY;
			var sz = metadata.SizeZ;

			var (bcx, bcy, _) = BlockVolume.GetBlockCount( metadata );

			Parallel.For( 0, bcx * bcy, blockIndex =>
			{
				var blockIndexX = blockIndex % bcx;
				var blockIndexY = blockIndex / bcx;
				var block = blocks[ blockIndex ];

				for( var bz = 0; bz < BlockVolume.N; bz++ )
				for( var by = 0; by < BlockVolume.N; by++ )
				for( var bx = 0; bx < BlockVolume.N; bx++ )
				{
					var gz = blockIndexZ * BlockVolume.N + bz;
					var gy = blockIndexY * BlockVolume.N + by;
					var gx = blockIndexX * BlockVolume.N + bx;

					block[ bz * BlockVolume.N2 + by * BlockVolume.N + bx ] = gz >= sz || gy >= sy || gx >= sx ? 0.0 : layer[ bz ][ gy * sx + gx ];
				}
			} );
		}

		#endregion
	}
}