#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion


namespace Zeiss.PiWeb.Volume
{
	#region usings

	#region usings

	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Xml;

	#endregion

	#endregion

	/// <summary>
	/// Encapsulates the options for volume compression.
	/// </summary>
	public sealed class VolumeCompressionOptions
	{
		#region constructors

		private VolumeCompressionOptions()
		{ }

		/// <summary>
		///     Initializes a new instance of the <see cref="VolumeCompressionOptions" /> class.
		/// </summary>
		/// <param name="encoder">The encoder.</param>
		/// <param name="encoderOptions">The encoder options.</param>
		/// <param name="pixelFormat">The pixelformat.</param>
		/// <param name="bitrate">The bitrate.</param>
		public VolumeCompressionOptions( string encoder = "libopenh264", string pixelFormat = "yuv420p", Dictionary<string, string>? encoderOptions = null, int bitrate = -1 )
		{
			Encoder = encoder;
			EncoderOptions = encoderOptions ?? new Dictionary<string, string>();
			PixelFormat = pixelFormat;
			Bitrate = bitrate;
		}

		#endregion

		#region properties

		/// <summary>
		/// Gets the codec.
		/// </summary>
		/// <value>
		/// The codec.
		/// </value>
		public string Encoder { get; private set; } = string.Empty;

		/// <summary>
		///     Gets the codec options.
		/// </summary>
		/// <value>
		///     The codec options.
		/// </value>
		public IReadOnlyDictionary<string, string> EncoderOptions { get; private set; } = new Dictionary<string, string>();

		/// <summary>
		/// Gets the pixel format
		/// </summary>
		public string PixelFormat { get; private set; } = string.Empty;

		/// <summary>
		/// Gets the bitrate.
		/// </summary>
		/// <value>
		/// The bitrate.
		/// </value>
		public int Bitrate { get; private set; } = -1;

		#endregion

		#region methods

		internal void Serialize( Stream stream )
		{
			var settings = new XmlWriterSettings
			{
				Indent = false,
				Encoding = Encoding.UTF8,
				CloseOutput = false
			};

			using var writer = XmlWriter.Create( stream, settings );

			writer.WriteStartDocument( true );
			writer.WriteStartElement( "VolumeCompressionOptions" );

			writer.WriteElementString( "Encoder", Encoder );
			writer.WriteElementString( "EncoderOptions", GetOptionsString() );
			writer.WriteElementString( "PixelFormat", PixelFormat );
			writer.WriteElementString( "Bitrate", Bitrate.ToString( CultureInfo.InvariantCulture ) );

			writer.WriteEndElement();
			writer.WriteEndDocument();
		}

		internal static VolumeCompressionOptions Deserialize( Stream stream )
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

			var result = new VolumeCompressionOptions();
			reader.MoveToElement();

			while( reader.Read() )
			{
				switch( reader.Name )
				{
					case "Encoder":
						result.Encoder = reader.ReadString();
						break;
					case "EncoderOptions":
						result.EncoderOptions = OptionsFromString( reader.ReadString() );
						break;
					case "PixelFormat":
						result.PixelFormat = reader.ReadString();
						break;
					case "Bitrate":
						result.Bitrate = int.Parse( reader.ReadString(), NumberStyles.Integer, CultureInfo.InvariantCulture );
						break;
				}
			}

			return result;
		}

		internal string GetOptionsString()
		{
			if( EncoderOptions.Count == 0 )
				return string.Empty;

			return string.Join( ";", EncoderOptions.Select( o => $"{o.Key}={o.Value}" ) );
		}

		private static Dictionary<string, string> OptionsFromString( string optionsString )
		{
			var result = new Dictionary<string, string>();
			if( string.IsNullOrWhiteSpace( optionsString ) )
				return result;

			var options = optionsString.Split( ';' );
			foreach( var option in options )
			{
				var kv = option.Split( '=' );
				if( kv.Length != 2 )
					throw new ArgumentException( "Encoder options must be specified in the scheme of 'name=value'" );

				result[ kv[ 0 ] ] = kv[ 1 ];
			}

			return result;
		}

		#endregion
	}
}