﻿#region Copyright

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
	using System.Buffers;
	using System.IO;
	using System.IO.Compression;

	#endregion

	internal static class ReaderWriterExtensions
	{
		#region methods

		/// <summary>
		/// Creates an empty zip entry with the specified <paramref name="entryName"/> and the specified <paramref name="compressionLevel"/>.
		/// The <code>LastWriteTime</code> is set to 01-01-1980 instead of the current time. By doing so, two zip archives with the same binary content
		/// become binary identical which leads to the same hash, which makes change detection easier.
		/// </summary>
		internal static ZipArchiveEntry CreateNormalizedEntry( this ZipArchive zipArchive, string entryName, CompressionLevel compressionLevel )
		{
			var entry = zipArchive.CreateEntry( entryName, compressionLevel );
			entry.LastWriteTime = new DateTime( 1980, 1, 1 );

			return entry;
		}

		/// <summary>
		/// Reads all data from the specified stream and returns it as a byte array.
		/// </summary>
		internal static byte[] StreamToArray( this Stream stream )
		{
			using var memStream = new MemoryStream();

			const int bufferSize = 64 * 1024;

			int count;
			var buffer = ArrayPool<byte>.Shared.Rent( bufferSize );
			while( ( count = stream.Read( buffer, 0, bufferSize ) ) > 0 )
			{
				memStream.Write( buffer, 0, count );
			}
			ArrayPool<byte>.Shared.Return( buffer );
			return memStream.ToArray();
		}

		/// <summary>
		/// Reads all data from the specified stream and returns it as a byte array.
		/// </summary>
		internal static byte[] StreamToArray( this Stream stream, int size )
		{
			var result = new byte[ size ];
			const int bufferSize = 64 * 1024;

			var buffer = ArrayPool<byte>.Shared.Rent( bufferSize );
			try
			{
				int count;
				var totalRead = 0;
				while( ( count = stream.Read( buffer, 0, bufferSize ) ) > 0 )
				{
					buffer.AsSpan( 0, count ).CopyTo( result.AsSpan( totalRead ) );
					totalRead += count;
				}
			}
			finally
			{
				ArrayPool<byte>.Shared.Return( buffer );
			}

			return result;
		}

		#endregion
	}
}