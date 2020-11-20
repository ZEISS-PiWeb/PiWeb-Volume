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
	using System.Buffers;
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
		internal void Encode( byte[][] input, Stream output, VolumeMetadata metadata, IProgress<VolumeSliceDefinition> progress )
		{
			var z = 0;
			var buffer = new byte[BlockVolume.N][];

			Encode( () =>
			{
				for( var bz = 0; bz < BlockVolume.N && z < metadata.SizeZ; bz++, z++ )
					buffer[ bz ] = input[ z ];

				return buffer;
			}, output, metadata, progress );
		}

		/// <summary>
		/// Encodes the input data that is coming from the specified input stream.
		/// </summary>
		internal void Encode( Stream input, Stream output, VolumeMetadata metadata, IProgress<VolumeSliceDefinition> progress )
		{
			var z = 0;

			var layerBuffer = new byte[BlockVolume.N][];
			for( var i = 0; i < BlockVolume.N; i++ )
				layerBuffer[ i ] = new byte[metadata.SizeX * metadata.SizeY];

			Encode( () =>
			{
				for( var bz = 0; bz < BlockVolume.N && z < metadata.SizeZ; bz++, z++ )
					input.Read( layerBuffer[ bz ], 0, metadata.SizeX * metadata.SizeY );

				return layerBuffer;
			}, output, metadata, progress );
		}

		private void Encode( Func<byte[][]> getLayerAction, Stream output, VolumeMetadata metadata, IProgress<VolumeSliceDefinition> progress )
		{
			var (bcx, bcy, _) = BlockVolume.GetBlockCount( metadata );
			
			var blockCount = bcx * bcy;
			var inputBlocks = new double[blockCount][];
			var resultBlocks = new short[blockCount][];
			var resultLengths = new ushort[blockCount];
			
			for( var i = 0; i < blockCount; i++ )
			{
				inputBlocks[ i ] = new double[BlockVolume.N3];
				resultBlocks[ i ] = new short[BlockVolume.N3];
			}

			using var writer = new BinaryWriter( output );

			var quantization = Quantization.Calculate( _Options, false );
			var zigzag = ZigZag.Calculate();

			for( ushort layerZ = 0, blockIndexZ = 0; layerZ < metadata.SizeZ; layerZ += BlockVolume.N, blockIndexZ++ )
			{
				CreateLayer( getLayerAction(), inputBlocks, blockIndexZ, metadata );
				EncodeLayer( inputBlocks, resultBlocks, resultLengths, quantization, zigzag );

				for (var blockIndex = 0; blockIndex < blockCount; blockIndex++)
				{
					var length = resultLengths[ blockIndex ];
					var block = resultBlocks[ blockIndex ];
					
					writer.Write( length );
					
					for( var i = 0; i < length; i++ )
						writer.Write( block[ i ] );
				}

				progress.Report( new VolumeSliceDefinition( Direction.Z, ( ushort ) ( blockIndexZ * BlockVolume.N ) ) );
			}
		}
		
		private static void EncodeLayer( double[][] inputBlocks, short[][] resultBlocks, ushort[] resultLengths, double[] quantization, int[] zigzag )
		{
			Parallel.For( 0, inputBlocks.Length,
				() => ArrayPool<double>.Shared.Rent( BlockVolume.N3 ),
				( blockIndex, state, buffer ) =>
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
						resultBlock[ i ] = ( short ) Math.Max( short.MinValue, Math.Min( short.MaxValue, Math.Round( inputBlock[ i ] ) ) );

					var count = 0;

					for( var i = 0; i < BlockVolume.N3; i++ )
					{
						if( resultBlock[ i ] != 0 )
							count = i + 1;
					}

					resultLengths[blockIndex] = ( ushort ) count;

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

					block[ bz * BlockVolume.N2 + by * BlockVolume.N + bx ] = gz >= sz || gy >= sy || gx >= sx ? 0.0 : layer[ bz ][ gy * sx + gx ] - 128.0;
				}
			} );
		}

		#endregion
	}
}