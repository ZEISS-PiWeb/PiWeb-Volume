﻿#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
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
		public ushort PositionX { get; private set; }

		/// <summary>
		/// The number of Voxels in Y-Dimension.
		/// </summary>
		public ushort PositionY { get; private set; }

		/// <summary>
		/// The number of Voxels in Z-Dimension.
		/// </summary>
		public ushort PositionZ { get; private set; }

		/// <summary>
		/// The number of Voxels in X-Dimension.
		/// </summary>
		public ushort SizeX { get; private set; }

		/// <summary>
		/// The number of Voxels in Y-Dimension.
		/// </summary>
		public ushort SizeY { get; private set; }

		/// <summary>
		/// The number of Voxels in Z-Dimension.
		/// </summary>
		public ushort SizeZ { get; private set; }

		/// <summary>
		/// The size of a Voxel in X-Dimension (mm).
		/// </summary>
		public double ResolutionX { get; private set; }

		/// <summary>
		/// The size of a Voxel in Y-Dimension (mm).
		/// </summary>
		public double ResolutionY { get; private set; }

		/// <summary>
		/// The size of a Voxel in Z-Dimension (mm).
		/// </summary>
		public double ResolutionZ { get; private set; }

		/// <summary>
		/// Gets or sets the metadata.
		/// </summary>
		public ICollection<Property> Properties { get; } = new List<Property>();

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
						result.SizeX = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
						break;
					case "SizeY":
						result.SizeY = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
						break;
					case "SizeZ":
						result.SizeZ = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
						break;
					case "ResolutionX":
						result.ResolutionX = double.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
						break;
					case "ResolutionY":
						result.ResolutionY = double.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
						break;
					case "ResolutionZ":
						result.ResolutionZ = double.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
						break;
					case "Property":
						var property = Property.Deserialize( reader );
						if( property is not null )
							result.Properties.Add( property );
						break;
					case "PositionX":
						result.PositionX = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
						break;
					case "PositionY":
						result.PositionY = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
						break;
					case "PositionZ":
						result.PositionZ = ushort.Parse( reader.ReadString(), CultureInfo.InvariantCulture );
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