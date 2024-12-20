﻿#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume
{
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
		#region fields

		private ushort _SizeX;
		private ushort _SizeY;
		private ushort _SizeZ;

		private double _ResolutionX;
		private double _ResolutionY;
		private double _ResolutionZ;

		private ushort _PositionX;
		private ushort _PositionY;
		private ushort _PositionZ;

		private ColorScale? _ColorScale;

		#endregion

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
		public VolumeMetadata(
			ushort sizeX,
			ushort sizeY,
			ushort sizeZ,
			double resolutionX,
			double resolutionY,
			double resolutionZ,
			ushort positionX = 0,
			ushort positionY = 0,
			ushort positionZ = 0 )
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
		public ushort PositionX { get => _PositionX; init => _PositionX = value; }

		/// <summary>
		/// The number of Voxels in Y-Dimension.
		/// </summary>
		public ushort PositionY { get => _PositionY; init => _PositionY = value; }

		/// <summary>
		/// The number of Voxels in Z-Dimension.
		/// </summary>
		public ushort PositionZ { get => _PositionZ; init => _PositionZ = value; }

		/// <summary>
		/// The number of Voxels in X-Dimension.
		/// </summary>
		public ushort SizeX { get => _SizeX; init => _SizeX = value; }

		/// <summary>
		/// The number of Voxels in Y-Dimension.
		/// </summary>
		public ushort SizeY { get => _SizeY; init => _SizeY = value; }

		/// <summary>
		/// The number of Voxels in Z-Dimension.
		/// </summary>
		public ushort SizeZ { get => _SizeZ; init => _SizeZ = value; }

		/// <summary>
		/// The size of a Voxel in X-Dimension (mm).
		/// </summary>
		public double ResolutionX { get => _ResolutionX; init => _ResolutionX = value; }

		/// <summary>
		/// The size of a Voxel in Y-Dimension (mm).
		/// </summary>
		public double ResolutionY { get => _ResolutionY; init => _ResolutionY = value; }

		/// <summary>
		/// The size of a Voxel in Z-Dimension (mm).
		/// </summary>
		public double ResolutionZ { get => _ResolutionZ; init => _ResolutionZ = value; }

		/// <summary>
		/// Gets or sets the metadata.
		/// </summary>
		public ICollection<Property> Properties { get; } = new List<Property>();

		/// <summary>
		/// The color scale that should be used to colorize the grayscale values of the volume.
		/// </summary>
		public ColorScale? ColorScale { get => _ColorScale; init => _ColorScale = value; }

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
			switch( direction )
			{
				case Direction.X: return SizeY * SizeZ;
				case Direction.Y: return SizeX * SizeZ;
				case Direction.Z: return SizeX * SizeY;
				default:
					throw new ArgumentOutOfRangeException( nameof( direction ), direction, null );
			}
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

			using( var writer = XmlWriter.Create( stream, settings ) )
			{
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

			var result = new VolumeMetadata();
			reader.MoveToElement();

			while( reader.Read() )
			{
				switch( reader.Name )
				{
					case "FileVersion":
						result.FileVersion = new Version( reader.ReadString() );
						break;
					case "SizeX":
						result._SizeX = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
						break;
					case "SizeY":
						result._SizeY = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
						break;
					case "SizeZ":
						result._SizeZ = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
						break;
					case "ResolutionX":
						result._ResolutionX = double.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
						break;
					case "ResolutionY":
						result._ResolutionY = double.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
						break;
					case "ResolutionZ":
						result._ResolutionZ = double.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
						break;
					case "Property":
						var property = Property.Deserialize( reader );
						if( property is not null )
							result.Properties.Add( property );
						break;
					case "PositionX":
						result._PositionX = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
						break;
					case "PositionY":
						result._PositionY = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
						break;
					case "PositionZ":
						result._PositionZ = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
						break;
					case "ColorScale":
						result._ColorScale = ColorScale.Read( reader );
						break;
				}
			}

			return result;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"Size {SizeX}x{SizeY}x{SizeZ}, Resolution {ResolutionX}x{ResolutionY}x{ResolutionZ}";
		}

		#endregion
	}
}