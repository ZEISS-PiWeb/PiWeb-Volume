#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2007                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume
{
	#region usings

    using System.IO;
    using System.IO.Compression;

    #endregion

	/// <summary>
	/// Hilfsklasse fuer den Umgang mit <see cref="System.IO.Stream"/>-Objekten.
	/// </summary>
	internal static class StreamHelper
	{
		#region methods

		public static byte[] CompressBytes( byte[] data )
		{
			var stream = new MemoryStream( data.Length );

			using( var zipOutput = new DeflateStream( stream, CompressionLevel.Fastest ) )
			{
				zipOutput.Write( data, 0, data.Length );
			}

			return stream.ToArray();
		}

        public static void DecompressBytes( byte[] source, byte[] destination )
        {
            using var sourceStream = new MemoryStream( source );
            using var gzipInput = new DeflateStream( sourceStream, CompressionMode.Decompress );

            using var destinationStream = new MemoryStream( destination, true );
            gzipInput.CopyTo( destinationStream );
        }

        #endregion
	}
}