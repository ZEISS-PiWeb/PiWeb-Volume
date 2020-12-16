#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

// ReSharper disable SuggestBaseTypeForParameter
namespace Zeiss.IMT.PiWeb.Volume.Block
{
	#region usings

	using System;
	using System.Buffers;
	using System.IO;
	using System.Threading;
	using System.Threading.Tasks;

	#endregion

	internal class BlockVolumeDecoder
	{
		#region members

		private readonly VolumeCompressionOptions _Options;
		
		internal delegate void BlockAction( byte[] data, BlockIndex index );
		internal delegate bool BlockPredicate( BlockIndex index );
		internal delegate bool LayerPredicate( ushort index );

		#endregion

		#region constructors

		internal BlockVolumeDecoder( VolumeCompressionOptions options )
		{
			_Options = options;
		}

		#endregion

		#region methods

		internal void Decode( Stream input,
			VolumeMetadata metadata,
			BlockAction blockAction,
			LayerPredicate layerPredicate = null,
			BlockPredicate blockPredicate = null,
			IProgress<VolumeSliceDefinition> progress = null,
			CancellationToken ct = default )
		{
			var sz = metadata.SizeZ;
			var sy = metadata.SizeY;
			var sx = metadata.SizeX;

			var quantization = Quantization.Calculate( _Options, true );
			var zigzag = ZigZag.Calculate();

			var result = new byte[sz][];

			for( var z = 0; z < sz; z++ )
				result[ z ] = new byte[sx * sy];

			var (bcx, bcy, bcz) = BlockVolume.GetBlockCount( metadata );
			var blockCount = bcx * bcy;
			var encodedBlockLengths = new ushort[blockCount];
			var encodedBlocks = new short[blockCount][];
			var decodedBlocks = new double[blockCount][];

			for( var i = 0; i < blockCount; i++ )
			{
				encodedBlocks[ i ] = new short[BlockVolume.N3];
				decodedBlocks[ i ] = new double[BlockVolume.N3];
			}

			using var reader = new BinaryReader( input );

			for( ushort biz = 0; biz < bcz; biz++ )
			{
				ct.ThrowIfCancellationRequested();
				if( layerPredicate?.Invoke( biz ) == false )
				{
					SkipLayer( reader, blockCount );
				}
				else
				{
					ReadLayer( reader, encodedBlocks, encodedBlockLengths );
					DecodeLayer( encodedBlocks, encodedBlockLengths, bcx, biz, quantization, zigzag, blockPredicate, blockAction );
				}

				progress?.Report( new VolumeSliceDefinition( Direction.Z, ( ushort ) ( biz * BlockVolume.N ) ) );
			}
		}

		private void SkipLayer( BinaryReader reader, int blockCount )
		{
			for( var i = 0; i < blockCount; i++ )
			{
				var resultLength =  reader.ReadUInt16();
				
				var length = resultLength & 0x0FFF;
				var firstLength = ( resultLength & 0b0011000000000000 ) >> 12;
				var otherLength = ( resultLength & 0b1100000000000000 ) >> 14;

				var count = 0;
				if( length > 0 )
					count += firstLength;
				if( length > 1 )
					count += ( length - 1 ) * otherLength;
				
				reader.BaseStream.Seek( count, SeekOrigin.Current );
			}
		}

		private void ReadLayer( BinaryReader reader, short[][] encodedBlocks, ushort[] encodedBlockLengths )
		{
			for( var i = 0; i < encodedBlocks.Length; i++ )
			{
				var resultLength =  reader.ReadUInt16();
				
				var length = resultLength & 0x0FFF;
				var firstLength = ( resultLength & 0b0011000000000000 ) >> 12;
				var otherLength = ( resultLength & 0b1100000000000000 ) >> 14;
				
				encodedBlockLengths[ i ] = (ushort)length;
				if( length > 0 )
					encodedBlocks[ i ][ 0 ] = firstLength == 2 ? reader.ReadInt16() : reader.ReadSByte();
				
				for (var j = 1; j < length; j++)
					encodedBlocks[ i ][ j ] = otherLength == 2 ? reader.ReadInt16() : reader.ReadSByte();
			}
		}

		private static void DecodeLayer(
			short[][] encodedBlocks,
			ushort[] encodedBlockLengths,
			ushort blockCountX,
			ushort blockIndexZ,
			double[] quantization,
			int[] zigzag,
			BlockPredicate blockPredicate,
			BlockAction blockAction )
		{
			Parallel.For( 0, encodedBlocks.Length, () => (
					ArrayPool<double>.Shared.Rent( BlockVolume.N3 ),
					ArrayPool<double>.Shared.Rent( BlockVolume.N3 ),
					ArrayPool<byte>.Shared.Rent( BlockVolume.N3 )
				), ( index, state, buffers ) =>
				{
					var blockIndexX = index % blockCountX;
					var blockIndexY = index / blockCountX;
					var blockIndex = new BlockIndex( ( ushort ) blockIndexX, ( ushort ) blockIndexY, blockIndexZ );

					if( blockPredicate?.Invoke( blockIndex ) == false )
						return buffers;

					var encodedBlock = encodedBlocks[ index ];
					var encodedLength = encodedBlockLengths[ index ];

					var doubleBuffer1 = buffers.Item1;
					var doubleBuffer2 = buffers.Item2;
					var byteBuffer = buffers.Item3;

					//1. Dediscretization
					for( var i = 0; i < BlockVolume.N3; i++ )
						doubleBuffer1[ i ] = i >= encodedLength ? 0 : encodedBlock[ i ];

					//2. ZigZag
					for( var i = 0; i < BlockVolume.N3; i++ )
						doubleBuffer2[ zigzag[ i ] ] = doubleBuffer1[ i ];

					//3. Quantization
					for( var i = 0; i < BlockVolume.N3; i++ )
						doubleBuffer2[ i ] *= quantization[ i ];

					//4. Cosine transform
					DiscreteCosineTransform.Transform( doubleBuffer2, doubleBuffer1, true );

					//5. Discretization
					for( var i = 0; i < BlockVolume.N3; i++ )
						byteBuffer[ i ] = ( byte ) Math.Max( byte.MinValue, Math.Min( byte.MaxValue, Math.Round( doubleBuffer1[ i ] + 128.0 ) ) );

					blockAction( byteBuffer, blockIndex );

					return buffers;
				},
				buffers =>
				{
					ArrayPool<double>.Shared.Return( buffers.Item1 );
					ArrayPool<double>.Shared.Return( buffers.Item2 );
					ArrayPool<byte>.Shared.Return( buffers.Item3 );
				} );
		}

		#endregion
	}
}