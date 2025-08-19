#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Convert;

#region usings

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#endregion

public class Scv
{
	#region constructors

	private Scv()
	{ }

	#endregion

	#region properties

	public double PositionX { get; private set; }

	public double PositionY { get; private set; }

	public double PositionZ { get; private set; }

	public float BinningMode { get; private set; }

	public float Amplification { get; private set; }

	public double VoxelReductionFactor { get; private set; }

	public double NoiseReductionFilter { get; private set; }

	public string PreFilter { get; private set; } = string.Empty;

	public uint Projections { get; private set; }

	public int DetectorImageHeight { get; private set; }

	public int DetectorImageWidth { get; private set; }

	public int DetectorHeight { get; private set; }

	public int DetectorWidth { get; private set; }

	public int DetectorBitDepth { get; private set; }

	public int VolumeSizeZ { get; private set; }

	public double ResolutionX { get; private set; }

	public double ResolutionY { get; private set; }

	public double ResolutionZ { get; private set; }

	public double MinBitDepth { get; private set; }

	public double MaxBitDepth { get; private set; }

	public double ScannerPositionX { get; private set; }

	public double ScannerPositionY { get; private set; }

	public double ScannerPositionZ { get; private set; }

	public int ScannerCurrent { get; private set; }

	public int ScannerVoltage { get; private set; }

	public double RtPositionX { get; private set; }

	public double RtPositionY { get; private set; }

	public double RtPositionZ { get; private set; }

	public double DetectorTime { get; private set; }

	public double DetectorGain { get; private set; }

	public double DetectorPositionX { get; private set; }

	public double DetectorPositionY { get; private set; }

	public double DetectorPositionZ { get; private set; }

	public float DetectorVoxelSizeX { get; private set; }

	public float DetectorVoxelSizeY { get; private set; }

	public int VolumeSizeY { get; private set; }

	public int VolumeSizeX { get; private set; }

	public int BitDepth { get; private set; }

	public int RoiX { get; private set; }

	public int RoiY { get; private set; }

	public int RoiW { get; private set; }

	public int RoiH { get; private set; }

	public float MaxReko { get; private set; }

	public float MinReko { get; private set; }

	public float Angle { get; private set; }

	public byte Merge { get; private set; }

	public int MirrorZ { get; private set; }

	public int HeaderLength { get; private set; }

	#endregion

	#region methods

	public static implicit operator VolumeMetadata( Scv scv )
	{
		return scv.ToMetadata();
	}

	public VolumeMetadata ToMetadata()
	{
		var properties = new List<Property>
		{
			Property.Create( "HeaderSize", HeaderLength ),
			Property.Create( "MirrorZ", MirrorZ ),
			Property.Create( "BitDepth", BitDepth ),
			Property.Create( "MinBitDepth", MinBitDepth ),
			Property.Create( "MaxBitDepth", MaxBitDepth ),
			Property.Create( "ScannerPositionX", ScannerPositionX ),
			Property.Create( "ScannerPositionY", ScannerPositionY ),
			Property.Create( "ScannerPositionZ", ScannerPositionZ ),
			Property.Create( "ScannerCurrent", ScannerCurrent ),
			Property.Create( "ScannerVoltage", ScannerVoltage ),
			Property.Create( "RtPositionX", RtPositionX ),
			Property.Create( "RtPositionY", RtPositionY ),
			Property.Create( "RtPositionZ", RtPositionZ ),
			Property.Create( "DetectorTime", DetectorTime ),
			Property.Create( "DetectorGain", DetectorGain ),
			Property.Create( "DetectorPositionX", DetectorPositionX ),
			Property.Create( "DetectorPositionY", DetectorPositionY ),
			Property.Create( "DetectorPositionZ", DetectorPositionZ ),
			Property.Create( "DetectorVoxelSizeX", DetectorVoxelSizeX ),
			Property.Create( "DetectorVoxelSizeY", DetectorVoxelSizeY ),
			Property.Create( "DetectorBitDepth", DetectorBitDepth ),
			Property.Create( "DetectorWidth", DetectorWidth ),
			Property.Create( "DetectorHeight", DetectorHeight ),
			Property.Create( "DetectorImageWidth", DetectorImageWidth ),
			Property.Create( "DetectorImageHeight", DetectorImageHeight ),
			Property.Create( "Projections", Projections ),
			Property.Create( "RoiX", RoiX ),
			Property.Create( "RoiY", RoiY ),
			Property.Create( "RoiWidth", RoiW ),
			Property.Create( "RoiHeight", RoiH ),
			Property.Create( "NoiseReductionFilter", NoiseReductionFilter ),
			Property.Create( "VoxelReductionFactor", VoxelReductionFactor ),
			Property.Create( "Amplification", Amplification ),
			Property.Create( "BinningMode", BinningMode ),
			Property.Create( "PreFilter", PreFilter ),
			Property.Create( "Position.X", PositionX ),
			Property.Create( "Position.Y", PositionY ),
			Property.Create( "Position.Z", PositionZ ),
			Property.Create( "MinReko", MinReko ),
			Property.Create( "MaxReko", MaxReko ),
			Property.Create( "Angle", Angle ),
			Property.Create( "Merge", Merge )
		};

		return new VolumeMetadata(
			(ushort)VolumeSizeX,
			(ushort)VolumeSizeY,
			(ushort)VolumeSizeZ,

			ResolutionX,
			ResolutionY,
			ResolutionZ,

			properties: properties );
	}

	public static Scv FromMetaData( VolumeMetadata metadata, int bitDepth )
	{
		var scv = new Scv
		{
			VolumeSizeX = metadata.SizeX,
			VolumeSizeY = metadata.SizeY,
			VolumeSizeZ = metadata.SizeZ,
			ResolutionX = metadata.ResolutionX,
			ResolutionY = metadata.ResolutionY,
			ResolutionZ = metadata.ResolutionZ,
			HeaderLength = 1024,
			BitDepth = bitDepth
		};

		return scv;
	}

	public static Scv Parse( Stream stream, int bitDepthFromExtension )
	{
		var result = new Scv();

		using var reader = new BinaryReader( stream, Encoding.UTF8, true );

		result.HeaderLength = reader.ReadInt32();
		result.MirrorZ = reader.ReadInt32();
		result.BitDepth = reader.ReadInt32();
		result.VolumeSizeX = reader.ReadInt32();
		result.VolumeSizeY = reader.ReadInt32();
		result.VolumeSizeZ = reader.ReadInt32();
		result.ResolutionX = reader.ReadDouble();
		result.ResolutionY = reader.ReadDouble();
		result.ResolutionZ = reader.ReadDouble();

		result.MinBitDepth = reader.ReadDouble();
		result.MaxBitDepth = reader.ReadDouble();

		result.ScannerPositionX = reader.ReadDouble();
		result.ScannerPositionY = reader.ReadDouble();
		result.ScannerPositionZ = reader.ReadDouble();
		result.ScannerCurrent = reader.ReadInt32();
		result.ScannerVoltage = reader.ReadInt32();

		result.RtPositionX = reader.ReadDouble();
		result.RtPositionY = reader.ReadDouble();
		result.RtPositionZ = reader.ReadDouble();
		result.DetectorTime = reader.ReadDouble();
		result.DetectorGain = reader.ReadDouble();
		result.DetectorPositionX = reader.ReadDouble();
		result.DetectorPositionY = reader.ReadDouble();
		result.DetectorPositionZ = reader.ReadDouble();
		result.DetectorVoxelSizeX = reader.ReadSingle();
		result.DetectorVoxelSizeY = reader.ReadSingle();

		result.DetectorBitDepth = reader.ReadInt32();
		result.DetectorWidth = reader.ReadInt32();
		result.DetectorHeight = reader.ReadInt32();
		result.DetectorImageWidth = reader.ReadInt32();
		result.DetectorImageHeight = reader.ReadInt32();

		result.Projections = reader.ReadUInt32();

		result.RoiX = reader.ReadInt32();
		result.RoiY = reader.ReadInt32();
		result.RoiW = reader.ReadInt32();
		result.RoiH = reader.ReadInt32();

		result.NoiseReductionFilter = reader.ReadDouble();
		result.VoxelReductionFactor = reader.ReadDouble();
		result.Amplification = reader.ReadSingle();
		result.BinningMode = reader.ReadSingle();
		result.PreFilter = Encoding.UTF8.GetString( reader.ReadBytes( 128 ) ).TrimEnd( '\0' );
		result.PositionX = reader.ReadDouble();
		result.PositionY = reader.ReadDouble();
		result.PositionZ = reader.ReadDouble();
		result.MinReko = reader.ReadSingle();
		result.MaxReko = reader.ReadSingle();
		result.Angle = reader.ReadSingle();
		result.Merge = reader.ReadByte();

		if( result.BitDepth == 0 )
			result.BitDepth = bitDepthFromExtension;

		return result;
	}

	public void Write( Stream stream )
	{
		using var writer = new BinaryWriter( stream, Encoding.UTF8, true );

		writer.Write( HeaderLength );
		writer.Write( MirrorZ );
		writer.Write( BitDepth );
		writer.Write( VolumeSizeX );
		writer.Write( VolumeSizeY );
		writer.Write( VolumeSizeZ );
		writer.Write( ResolutionX );
		writer.Write( ResolutionY );
		writer.Write( ResolutionZ );

		writer.Write( MinBitDepth );
		writer.Write( MaxBitDepth );

		writer.Write( ScannerPositionX );
		writer.Write( ScannerPositionY );
		writer.Write( ScannerPositionZ );
		writer.Write( ScannerCurrent );
		writer.Write( ScannerVoltage );

		writer.Write( RtPositionX );
		writer.Write( RtPositionY );
		writer.Write( RtPositionZ );
		writer.Write( DetectorTime );
		writer.Write( DetectorGain );
		writer.Write( DetectorPositionX );
		writer.Write( DetectorPositionY );
		writer.Write( DetectorPositionZ );
		writer.Write( DetectorVoxelSizeX );
		writer.Write( DetectorVoxelSizeY );

		writer.Write( DetectorBitDepth );
		writer.Write( DetectorWidth );
		writer.Write( DetectorHeight );
		writer.Write( DetectorImageWidth );
		writer.Write( DetectorImageHeight );

		writer.Write( Projections );

		writer.Write( RoiX );
		writer.Write( RoiY );
		writer.Write( RoiW );
		writer.Write( RoiH );

		writer.Write( NoiseReductionFilter );
		writer.Write( VoxelReductionFactor );
		writer.Write( Amplification );
		writer.Write( BinningMode );
		var buffer = Encoding.UTF8.GetBytes( PreFilter );
		Array.Resize( ref buffer, 128 );
		writer.Write( buffer );
		writer.Write( PositionX );
		writer.Write( PositionY );
		writer.Write( PositionZ );
		writer.Write( MinReko );
		writer.Write( MaxReko );
		writer.Write( Angle );
		writer.Write( Merge );
	}

	#endregion
}