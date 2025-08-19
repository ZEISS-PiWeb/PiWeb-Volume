#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume;

#region usings

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Zeiss.PiWeb.ColorScale;

#endregion

/// <summary>
/// Holds additional information about a volume
/// </summary>
public sealed class VolumeMetadata
{
	#region constructors

	private VolumeMetadata()
	{ }

	/// <summary>
	/// Initializes a new instance of the <see cref="VolumeMetadata" /> class.
	/// </summary>
	/// <param name="sizeX">The size x.</param>
	/// <param name="sizeY">The size y.</param>
	/// <param name="sizeZ">The size z.</param>
	/// <param name="resolutionX">The resolution x.</param>
	/// <param name="resolutionY">The resolution y.</param>
	/// <param name="resolutionZ">The resolution z.</param>
	/// <param name="positionX">The position x.</param>
	/// <param name="positionY">The position y.</param>
	/// <param name="positionZ">The position z.</param>
	/// <param name="properties">Optional properties for this volume.</param>
	public VolumeMetadata(
		ushort sizeX,
		ushort sizeY,
		ushort sizeZ,
		double resolutionX,
		double resolutionY,
		double resolutionZ,
		ushort positionX = 0,
		ushort positionY = 0,
		ushort positionZ = 0,
		IReadOnlyList<Property>? properties = null )
	{
		SizeX = sizeX;
		SizeY = sizeY;
		SizeZ = sizeZ;

		ResolutionX = resolutionX;
		ResolutionY = resolutionY;
		ResolutionZ = resolutionZ;

		PositionX = positionX;
		PositionY = positionY;
		PositionZ = positionZ;

		Properties = properties ?? [];
	}

	#endregion

	#region properties

	/// <summary>
	/// Gets the file version.
	/// </summary>
	public Version FileVersion { get; private set; } = Volume.FileVersion;

	/// <summary>
	/// The number of Voxels in X-Dimension.
	/// </summary>
	public ushort PositionX { get; init; }

	/// <summary>
	/// The number of Voxels in Y-Dimension.
	/// </summary>
	public ushort PositionY { get; init; }

	/// <summary>
	/// The number of Voxels in Z-Dimension.
	/// </summary>
	public ushort PositionZ { get; init; }

	/// <summary>
	/// The number of Voxels in X-Dimension.
	/// </summary>
	public ushort SizeX { get; init; }

	/// <summary>
	/// The number of Voxels in Y-Dimension.
	/// </summary>
	public ushort SizeY { get; init; }

	/// <summary>
	/// The number of Voxels in Z-Dimension.
	/// </summary>
	public ushort SizeZ { get; init; }

	/// <summary>
	/// The size of a Voxel in X-Dimension (mm).
	/// </summary>
	public double ResolutionX { get; init; }

	/// <summary>
	/// The size of a Voxel in Y-Dimension (mm).
	/// </summary>
	public double ResolutionY { get; init; }

	/// <summary>
	/// The size of a Voxel in Z-Dimension (mm).
	/// </summary>
	public double ResolutionZ { get; init; }

	/// <summary>
	/// Gets or sets the metadata.
	/// </summary>
	public IReadOnlyList<Property> Properties { get; } = [];

	/// <summary>
	/// The color scale that should be used to colorize the grayscale values of the volume.
	/// </summary>
	public ColorScale? ColorScale { get; init; }

	#endregion

	#region methods

	/// <summary>
	/// Returns the size in the specified direction.
	/// </summary>
	public ushort GetSize( Direction direction )
	{
		return direction switch
		{
			Direction.X => SizeX,
			Direction.Y => SizeY,
			Direction.Z => SizeZ,
			_           => throw new ArgumentOutOfRangeException( nameof( direction ), direction, null )
		};
	}

	/// <summary>
	/// Returns the size of a slice in the specified direction.
	/// </summary>
	/// <param name="direction"></param>
	/// <param name="width"></param>
	/// <param name="height"></param>
	public void GetSliceSize( Direction direction, out ushort width, out ushort height )
	{
		switch( direction )
		{
			case Direction.X:
				width = SizeY;
				height = SizeZ;
				break;
			case Direction.Y:
				width = SizeX;
				height = SizeZ;
				break;
			case Direction.Z:
				width = SizeX;
				height = SizeY;
				break;
			default:
				throw new ArgumentOutOfRangeException( nameof( direction ), direction, null );
		}
	}

	/// <summary>
	/// Returns the required size of the slice buffer in the specified direction.
	/// </summary>
	/// <param name="direction"></param>
	public int GetSliceLength( Direction direction )
	{
		return direction switch
		{
			Direction.X => SizeY * SizeZ,
			Direction.Y => SizeX * SizeZ,
			Direction.Z => SizeX * SizeY,
			_           => throw new ArgumentOutOfRangeException( nameof( direction ), direction, null )
		};
	}

	/// <summary>
	/// Serializes the data of this instance and writes it into the specified <paramref name="stream" />.
	/// </summary>
	/// <param name="stream">The stream.</param>
	/// <param name="upgradeVersionNumber">if set to <c>true</c>, the version number is adjusted to match the current version.</param>
	internal void Serialize( Stream stream, bool upgradeVersionNumber = true )
	{
		if( upgradeVersionNumber )
			FileVersion = Volume.FileVersion;

		var settings = new XmlWriterSettings
		{
			Indent = false,
			Encoding = Encoding.UTF8,
			CloseOutput = false,
			NewLineChars = "\r\n",
			NewLineHandling = NewLineHandling.Replace,
			NewLineOnAttributes = false
		};

		using var writer = XmlWriter.Create( stream, settings );

		writer.WriteStartDocument( true );
		writer.WriteStartElement( "VolumeMetadata" );

		writer.WriteElementString( "FileVersion", FileVersion.ToString() );

		writer.WriteElementString( "SizeX", SizeX.ToString( CultureInfo.InvariantCulture ) );
		writer.WriteElementString( "SizeY", SizeY.ToString( CultureInfo.InvariantCulture ) );
		writer.WriteElementString( "SizeZ", SizeZ.ToString( CultureInfo.InvariantCulture ) );

		writer.WriteElementString( "ResolutionX", ResolutionX.ToString( CultureInfo.InvariantCulture ) );
		writer.WriteElementString( "ResolutionY", ResolutionY.ToString( CultureInfo.InvariantCulture ) );
		writer.WriteElementString( "ResolutionZ", ResolutionZ.ToString( CultureInfo.InvariantCulture ) );

		writer.WriteElementString( "PositionX", PositionX.ToString( CultureInfo.InvariantCulture ) );
		writer.WriteElementString( "PositionY", PositionY.ToString( CultureInfo.InvariantCulture ) );
		writer.WriteElementString( "PositionZ", PositionZ.ToString( CultureInfo.InvariantCulture ) );

		if( Properties.Count > 0 )
		{
			foreach( var property in Properties )
			{
				writer.WriteStartElement( "Property" );
				property.Serialize( writer );
				writer.WriteEndElement();
			}
		}

		if( ColorScale is not null )
		{
			writer.WriteStartElement( "ColorScale" );
			ColorScale.Write( writer );
			writer.WriteEndElement();
		}

		writer.WriteEndElement();
		writer.WriteEndDocument();
	}

	internal static VolumeMetadata Deserialize( Stream stream )
	{
		var settings = new XmlReaderSettings
		{
			IgnoreComments = true,
			IgnoreWhitespace = true,
			IgnoreProcessingInstructions = true,
			CloseInput = false,
			NameTable = new NameTable()
		};

		using var reader = XmlReader.Create( stream, settings );

		reader.MoveToElement();

		var fileVersion = Volume.FileVersion;

		ColorScale? colorScale = null;

		var properties = new List<Property>();
		ushort sizeX = 0, sizeY = 0, sizeZ = 0;
		ushort positionX = 0, positionY = 0, positionZ = 0;
		double resolutionX = 0, resolutionY = 0, resolutionZ = 0;

		while( reader.Read() )
		{
			switch( reader.Name )
			{
				case "FileVersion":
					fileVersion = new Version( reader.ReadString() );
					break;
				case "SizeX":
					sizeX = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
					break;
				case "SizeY":
					sizeY = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
					break;
				case "SizeZ":
					sizeZ = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
					break;
				case "ResolutionX":
					resolutionX = double.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
					break;
				case "ResolutionY":
					resolutionY = double.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
					break;
				case "ResolutionZ":
					resolutionZ = double.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
					break;
				case "Property":
					var property = Property.Deserialize( reader );
					if( property is not null )
						properties.Add( property );
					break;
				case "PositionX":
					positionX = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
					break;
				case "PositionY":
					positionY = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
					break;
				case "PositionZ":
					positionZ = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
					break;
				case "ColorScale":
					colorScale = ColorScale.Read( reader );
					break;
			}
		}

		return new VolumeMetadata( sizeX, sizeY, sizeZ, resolutionX, resolutionY, resolutionZ, positionX, positionY, positionZ, properties )
		{
			FileVersion = fileVersion,
		};
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return $"Size {SizeX}x{SizeY}x{SizeZ}, Resolution {ResolutionX}x{ResolutionY}x{ResolutionZ}";
	}

	#endregion
}