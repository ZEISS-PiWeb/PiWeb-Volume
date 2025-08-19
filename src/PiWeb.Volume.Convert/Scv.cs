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
using System.IO;
using System.Linq;
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

	public static implicit operator VolumeMetadata( Scv scv ) => scv.ToMetadata();

	public VolumeMetadata ToMetadata()
	{
		var result = new VolumeMetadata(
			(ushort)VolumeSizeX,
			(ushort)VolumeSizeY,
			(ushort)VolumeSizeZ,
			ResolutionX,
			ResolutionY,
			ResolutionZ );

		result.Properties.Add( Property.Create( "HeaderSize", HeaderLength ) );
		result.Properties.Add( Property.Create( "MirrorZ", MirrorZ ) );
		result.Properties.Add( Property.Create( "BitDepth", BitDepth ) );
		result.Properties.Add( Property.Create( "MinBitDepth", MinBitDepth ) );
		result.Properties.Add( Property.Create( "MaxBitDepth", MaxBitDepth ) );
		result.Properties.Add( Property.Create( "ScannerPositionX", ScannerPositionX ) );
		result.Properties.Add( Property.Create( "ScannerPositionY", ScannerPositionY ) );
		result.Properties.Add( Property.Create( "ScannerPositionZ", ScannerPositionZ ) );
		result.Properties.Add( Property.Create( "ScannerCurrent", ScannerCurrent ) );
		result.Properties.Add( Property.Create( "ScannerVoltage", ScannerVoltage ) );
		result.Properties.Add( Property.Create( "RtPositionX", RtPositionX ) );
		result.Properties.Add( Property.Create( "RtPositionY", RtPositionY ) );
		result.Properties.Add( Property.Create( "RtPositionZ", RtPositionZ ) );
		result.Properties.Add( Property.Create( "DetectorTime", DetectorTime ) );
		result.Properties.Add( Property.Create( "DetectorGain", DetectorGain ) );
		result.Properties.Add( Property.Create( "DetectorPositionX", DetectorPositionX ) );
		result.Properties.Add( Property.Create( "DetectorPositionY", DetectorPositionY ) );
		result.Properties.Add( Property.Create( "DetectorPositionZ", DetectorPositionZ ) );
		result.Properties.Add( Property.Create( "DetectorVoxelSizeX", DetectorVoxelSizeX ) );
		result.Properties.Add( Property.Create( "DetectorVoxelSizeY", DetectorVoxelSizeY ) );
		result.Properties.Add( Property.Create( "DetectorBitDepth", DetectorBitDepth ) );
		result.Properties.Add( Property.Create( "DetectorWidth", DetectorWidth ) );
		result.Properties.Add( Property.Create( "DetectorHeight", DetectorHeight ) );
		result.Properties.Add( Property.Create( "DetectorImageWidth", DetectorImageWidth ) );
		result.Properties.Add( Property.Create( "DetectorImageHeight", DetectorImageHeight ) );
		result.Properties.Add( Property.Create( "Projections", Projections ) );
		result.Properties.Add( Property.Create( "RoiX", RoiX ) );
		result.Properties.Add( Property.Create( "RoiY", RoiY ) );
		result.Properties.Add( Property.Create( "RoiWidth", RoiW ) );
		result.Properties.Add( Property.Create( "RoiHeight", RoiH ) );
		result.Properties.Add( Property.Create( "NoiseReductionFilter", NoiseReductionFilter ) );
		result.Properties.Add( Property.Create( "VoxelReductionFactor", VoxelReductionFactor ) );
		result.Properties.Add( Property.Create( "Amplification", Amplification ) );
		result.Properties.Add( Property.Create( "BinningMode", BinningMode ) );
		result.Properties.Add( Property.Create( "PreFilter", PreFilter ) );
		result.Properties.Add( Property.Create( "Position.X", PositionX ) );
		result.Properties.Add( Property.Create( "Position.Y", PositionY ) );
		result.Properties.Add( Property.Create( "Position.Z", PositionZ ) );
		result.Properties.Add( Property.Create( "MinReko", MinReko ) );
		result.Properties.Add( Property.Create( "MaxReko", MaxReko ) );
		result.Properties.Add( Property.Create( "Angle", Angle ) );
		result.Properties.Add( Property.Create( "Merge", Merge ) );

		return result;
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