namespace Zeiss.PiWeb.Volume.Convert;

#region usings

using System;
using System.Globalization;
using System.IO;
using System.Xml;

#endregion

public static class Gom
{
	#region constants

	public const string MaximumValue = "MaximumValue";
	public const string MinimumValue = "MinimumValue";
	public const string RawDataName = "RawDataName";

	#endregion

	#region methods

	public static VolumeMetadata ParseMetadata( Stream stream, out double minValue, out double maxValue, out string rawFileName, out DataType dataType )
	{
		var document = new XmlDocument();
		document.Load( stream );

		var sx = ReadUshort( document, "//volume/volume_size/x" );
		var sy = ReadUshort( document, "//volume/volume_size/y" );
		var sz = ReadUshort( document, "//volume/volume_size/z" );

		var rx = ReadDouble( document, "//volume/voxel_size/x" );
		var ry = ReadDouble( document, "//volume/voxel_size/y" );
		var rz = ReadDouble( document, "//volume/voxel_size/z" );

		dataType = ReadDataType( document, "//volume/data_type" );

		minValue = ReadDouble( document, "//volume/gray_value_range/min" );
		maxValue = ReadDouble( document, "//volume/gray_value_range/max" );

		rawFileName = ReadValue( document, "//volume/raw_data/raw_data_block" );

		var result = new VolumeMetadata( sx, sy, sz, rx, ry, rz );

		result.Properties.Add( Property.Create( MaximumValue, minValue ) );
		result.Properties.Add( Property.Create( MinimumValue, maxValue ) );
		result.Properties.Add( Property.Create( RawDataName, rawFileName ) );

		return result;
	}

	private static DataType ReadDataType( XmlDocument document, string path )
	{
		var node = ReadValue( document, path );
		switch( node )
		{
			case "uint16": return DataType.UInt16;
			case "int16": return DataType.Int16;
			case "float32": return DataType.Single;
			default:
				throw new FormatException( $"Unsupported data type {node}" );
		}
	}

	private static ushort ReadUshort( XmlDocument document, string path )
	{
		var node = ReadValue( document, path );

		if( !ushort.TryParse( node, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result ) )
			throw new FormatException( $"Expected node {path} to have a value of type unsigned short" );

		return result;
	}

	private static double ReadDouble( XmlDocument document, string path )
	{
		var node = ReadValue( document, path );

		if( !double.TryParse( node, NumberStyles.Float, CultureInfo.InvariantCulture, out var result ) )
			throw new FormatException( $"Expected node {path} to have a floating point value in invariant culture" );

		return result;
	}

	private static string ReadValue( XmlDocument document, string path )
	{
		var node = document.SelectSingleNode( path );
		if( node is null )
			throw new FormatException( $"Unable to find node {path} in document" );

		if( node.FirstChild is not XmlText text || string.IsNullOrEmpty( text.Value ) )
			throw new FormatException( $"Encountered empty node {path} in document" );

		return text.Value;
	}

	#endregion

	public enum DataType
	{
		Int16,
		UInt16,
		Single
	}
}