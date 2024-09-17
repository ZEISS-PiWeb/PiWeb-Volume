#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Convert
{
	#region usings

	using System;
	using System.Globalization;
	using System.IO;

	#endregion

	internal class Vgi
	{
		#region properties

		public int SizeX { get; private set; }

		public int SizeY { get; private set; }

		public int SizeZ { get; private set; }

		public double ResX { get; private set; }

		public double ResY { get; private set; }

		public double ResZ { get; private set; }

		public int BitDepth { get; private set; }

		public int HeaderLength { get; private set; }

		#endregion

		#region methods

		public static implicit operator VolumeMetadata( Vgi vgi ) => vgi.ToMetaData();

		private VolumeMetadata ToMetaData()
		{
			return new VolumeMetadata( ( ushort ) SizeX,
				( ushort ) SizeY,
				( ushort ) SizeZ,
				ResX,
				ResY,
				ResZ );
		}

		public static Vgi Parse( Stream stream )
		{
			var result = new Vgi();

			using var reader = new StreamReader( stream );

			while( reader.Peek() >= 0 )
			{
				var line = reader.ReadLine();
				if( string.IsNullOrEmpty( line ) )
					continue;

				var parts = line.Split( new[] { '=' }, StringSplitOptions.RemoveEmptyEntries );
				if( parts.Length != 2 )
					continue;

				var declaration = parts[ 0 ].Trim().ToLower();
				var value = parts[ 1 ].Trim();

				switch( declaration )
				{
					case "size":
						result.ParseSize( value );
						break;
					case "resolution":
						result.ParseResolution( value );
						break;
					case "bitsperelement":
						result.BitDepth = int.Parse( value );
						break;
					case "skipheader":
						result.HeaderLength = int.Parse( value );
						break;
				}
			}

			return result;
		}

		private void ParseSize( string line )
		{
			var parts = line.Split( ' ' );
			if( parts.Length != 3 )
				throw new FormatException( "size must have three integer values" );

			SizeX = int.Parse( parts[ 0 ] );
			SizeY = int.Parse( parts[ 1 ] );
			SizeZ = int.Parse( parts[ 2 ] );
		}

		private void ParseResolution( string line )
		{
			var parts = line.Split( ' ' );
			if( parts.Length != 3 )
				throw new FormatException( "resolution must have three double values" );

			ResX = double.Parse( parts[ 0 ], CultureInfo.InvariantCulture );
			ResY = double.Parse( parts[ 1 ], CultureInfo.InvariantCulture );
			ResZ = double.Parse( parts[ 2 ], CultureInfo.InvariantCulture );
		}

		#endregion
	}
}