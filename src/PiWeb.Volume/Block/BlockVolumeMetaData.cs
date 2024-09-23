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

#endregion

/// <summary>
/// Holds information that are needed to encode and decode a block volume.
/// </summary>
public readonly record struct BlockVolumeMetaData( uint Version, ushort SizeX, ushort SizeY, ushort SizeZ, double[] Quantization )
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
	/// Reads the metadata from the specified <paramref name="reader"/>
	/// </summary>
	public static BlockVolumeMetaData Read( BinaryReader reader )
	{
		var header = reader.ReadUInt32();
		if( header != BlockVolume.FileHeader )
			throw new FormatException( $"Encountered unexpected file header 0x{header:x8}, expected 0x{BlockVolume.FileHeader:x8}" );

		var version = reader.ReadUInt32();
		if( version != BlockVolume.Version )
			throw new FormatException( $"Encountered unexpected file header '{version}', expected {BlockVolume.Version}" );

		var sizeX = reader.ReadUInt16();
		var sizeY = reader.ReadUInt16();
		var sizeZ = reader.ReadUInt16();

		var quantization = Block.Quantization.Read( reader, true );

		return new BlockVolumeMetaData( version, sizeX, sizeY, sizeZ, quantization );
	}

	#endregion
}