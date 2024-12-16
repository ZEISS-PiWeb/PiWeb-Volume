#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2024                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Block;

#region usings

using System;
using System.IO;
using System.Runtime.InteropServices;

#endregion

/// <summary>
/// Holds information that are needed to encode and decode a block volume.
/// </summary>
public record BlockVolumeMetaData( uint Version, ushort SizeX, ushort SizeY, ushort SizeZ, double[] Quantization )
{
	#region methods

	/// <summary>
	/// Writes the metadata to the specified <paramref name="writer"/>
	/// </summary>
	public void Write( BinaryWriter writer )
	{
		writer.Write( BlockVolume.FileHeader );
		writer.Write( Version );

		writer.Write( SizeX );
		writer.Write( SizeY );
		writer.Write( SizeZ );

		Block.Quantization.Write( writer, Quantization );
	}

	/// <summary>
	/// Reads the metadata from the specified <paramref name="data"/>
	/// </summary>
	public static BlockVolumeMetaData Create( ReadOnlySpan<byte> data )
	{
		var position = 0;
		var header = MemoryMarshal.Read<uint>( data[ position.. ] );
		position += sizeof( uint );
		if( header != BlockVolume.FileHeader )
			throw new FormatException( $"Encountered unexpected file header 0x{header:x8}, expected 0x{BlockVolume.FileHeader:x8}" );

		var version = MemoryMarshal.Read<uint>( data[ position.. ] );
		position += sizeof( uint );
		if( version != BlockVolume.Version )
			throw new FormatException( $"Encountered unexpected file header '{version}', expected {BlockVolume.Version}" );

		var sizeX = MemoryMarshal.Read<ushort>( data[ position.. ] );
		position += sizeof( ushort );
		var sizeY = MemoryMarshal.Read<ushort>( data[ position.. ] );
		position += sizeof( ushort );
		var sizeZ = MemoryMarshal.Read<ushort>( data[ position.. ] );
		position += sizeof( ushort );

		var quantization = MemoryMarshal.Cast<byte, double>( data.Slice( position, BlockVolume.N3 * sizeof( double ) ) );

		return new BlockVolumeMetaData( version, sizeX, sizeY, sizeZ, quantization.ToArray() );
	}

	#endregion

	internal (ushort, ushort, ushort) GetBlockCount()
	{
		return ( GetBlockCount( Direction.X ),
			GetBlockCount( Direction.Y ),
			GetBlockCount( Direction.Z ) );
	}

	internal ushort GetBlockCount( Direction direction )
	{
		var size = direction switch
		{
			Direction.X => SizeX,
			Direction.Y => SizeY,
			Direction.Z => SizeZ,
			_           => throw new ArgumentOutOfRangeException( nameof( direction ), direction, null )
		};

		var blocks = size / BlockVolume.N;
		if( blocks * BlockVolume.N < size )
			blocks++;

		return (ushort)blocks;
	}

	internal (ushort, ushort) GetLayerBlockCount( Direction direction )
	{
		return direction switch
		{
			Direction.X => ( GetBlockCount( Direction.Y ), GetBlockCount( Direction.Z ) ),
			Direction.Y => ( GetBlockCount( Direction.X ), GetBlockCount( Direction.Z ) ),
			Direction.Z => ( GetBlockCount( Direction.X ), GetBlockCount( Direction.Y ) ),
			_           => throw new ArgumentOutOfRangeException( nameof( direction ), direction, null )
		};
	}

	/// <summary>
	/// The number of bytes the header consists of.
	/// </summary>
	public static int HeaderLength => 2 * sizeof( uint ) + 3 * sizeof( ushort ) + BlockVolume.N3 * sizeof( double );
}