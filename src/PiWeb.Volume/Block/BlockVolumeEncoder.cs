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
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

#endregion

/// <summary>
/// Encodes a collection of slices with the block volume encoder.
/// </summary>
internal static class BlockVolumeEncoder
{
	#region methods

	/// <summary>
	/// Encodes the specified input data which is a complete volume in z-slices.
	/// </summary>
	internal static void Encode(
		IReadOnlyList<VolumeSlice> slices,
		Stream output,
		VolumeMetadata metadata,
		VolumeCompressionOptions options,
		IProgress<VolumeSliceDefinition>? progress )
	{
		var quantization = Quantization.Calculate( options );
		var blockVolumeMetaData = new BlockVolumeMetaData(
			BlockVolume.Version,
			metadata.SizeX,
			metadata.SizeY,
			metadata.SizeZ,
			quantization );

		Encode( EnumerateBlockLayersFromSlices( slices, blockVolumeMetaData ), output, blockVolumeMetaData, progress );
	}

	/// <summary>
	/// Encodes the input data that is coming from the specified input stream.
	/// </summary>
	internal static void Encode(
		Stream input,
		Stream output,
		VolumeMetadata metadata,
		VolumeCompressionOptions options,
		IProgress<VolumeSliceDefinition>? progress )
	{
		var quantization = Quantization.Calculate( options );
		var blockVolumeMetaData = new BlockVolumeMetaData(
			BlockVolume.Version,
			metadata.SizeX,
			metadata.SizeY,
			metadata.SizeZ,
			quantization );

		Encode( EnumerateBlockLayersFromStream( input, blockVolumeMetaData ), output, blockVolumeMetaData, progress );
	}

	private static IEnumerable<byte[][]> EnumerateBlockLayersFromStream( Stream input, BlockVolumeMetaData metadata )
	{
		var z = 0;
		var (_, _, layerCount) = BlockVolume.GetBlockCount( metadata );
		var layerBuffer = new byte[ BlockVolume.N ][];
		var layerLength = metadata.SizeX * metadata.SizeY;

		for( var i = 0; i < BlockVolume.N; i++ )
			layerBuffer[ i ] = new byte[ layerLength ];

		for( var layerIndex = 0; layerIndex < layerCount; layerIndex++ )
		{
			for( var bz = 0; bz < BlockVolume.N && z < metadata.SizeZ; bz++, z++ )
			{
				if( input.Read( layerBuffer[ bz ], 0, layerLength ) < layerLength )
					throw new EndOfStreamException();
			}

			yield return layerBuffer;
		}
	}

	private static IEnumerable<byte[][]> EnumerateBlockLayersFromSlices( IReadOnlyList<VolumeSlice> slices, BlockVolumeMetaData metadata )
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

	private static void Encode( IEnumerable<byte[][]> blockLayers, Stream output, BlockVolumeMetaData metadata, IProgress<VolumeSliceDefinition>? progress )
	{
		var (bcx, bcy, _) = BlockVolume.GetBlockCount( metadata );

		var blockCount = bcx * bcy;
		var inputBlocks = new double[ blockCount ][];
		var resultBlocks = new short[ blockCount ][];
		var blockInfos = new BlockInfo[ blockCount ];

		for( var i = 0; i < blockCount; i++ )
		{
			inputBlocks[ i ] = new double[ BlockVolume.N3 ];
			resultBlocks[ i ] = new short[ BlockVolume.N3 ];
		}

		using var writer = new BinaryWriter( output );
		metadata.Write( writer );

		var blockIndexZ = (ushort)0;
		var quantization = metadata.Quantization;

		foreach( var blockLayer in blockLayers )
		{
			CreateLayer( blockLayer, inputBlocks, blockIndexZ, metadata );
			EncodeLayer( inputBlocks, resultBlocks, blockInfos, quantization );
			WriteLayer( writer, resultBlocks, blockInfos );

			progress?.Report( new VolumeSliceDefinition( Direction.Z, (ushort)( blockIndexZ * BlockVolume.N ) ) );

			blockIndexZ++;
		}
	}

	private static void WriteLayer( BinaryWriter writer, short[][] blocks, BlockInfo[] blockInfos )
	{
		var layerLength = blockInfos.Sum( blockInfo => sizeof( ushort ) + blockInfo.Length );
		writer.Write( layerLength );

		for( var blockIndex = 0; blockIndex < blockInfos.Length; blockIndex++ )
		{
			var blockInfo = blockInfos[ blockIndex ];
			var block = blocks[ blockIndex ];

			WriteBlock( writer, block, blockInfo );
		}
	}

	private static void WriteBlock( BinaryWriter writer, short[] block, BlockInfo blockInfo )
	{
		blockInfo.Write( writer );

		if( blockInfo.ValueCount > 0 )
		{
			if( blockInfo.IsFirstValueShort )
				writer.Write( block[ 0 ] );
			else
				writer.Write( (sbyte)block[ 0 ] );
		}

		if( blockInfo.AreOtherValuesShort )
		{
			for( var i = 1; i < blockInfo.ValueCount; i++ )
				writer.Write( block[ i ] );
		}
		else
		{
			for( var i = 1; i < blockInfo.ValueCount; i++ )
				writer.Write( (sbyte)block[ i ] );
		}
	}

	private static void EncodeLayer(
		double[][] inputBlocks,
		short[][] resultBlocks,
		BlockInfo[] blockInfos,
		double[] quantization
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
				for( var i = 0; i < BlockVolume.N3; i += BlockVolume.N )
					Quantization.Apply( quantization, buffer, inputBlock );

				//3. ZigZag
				ZigZag.Apply( inputBlock, buffer );

				//4. Discretization
				for( var i = 0; i < BlockVolume.N3; i++ )
					resultBlock[ i ] = (short)Math.Clamp( short.MinValue, short.MaxValue, Math.Round( buffer[ i ] ) );

				blockInfos[ blockIndex ] = BlockInfo.Create( resultBlock );

				return buffer;
			}, buffer => ArrayPool<double>.Shared.Return( buffer ) );
	}

	private static void CreateLayer( byte[][] layer, double[][] blocks, ushort blockIndexZ, BlockVolumeMetaData metadata )
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