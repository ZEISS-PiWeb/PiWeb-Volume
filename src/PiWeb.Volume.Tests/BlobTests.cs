#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2026                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Tests;

using System;
using System.IO;
using NUnit.Framework;
using Zeiss.PiWeb.Volume;

[TestFixture]
public class BlobTests
{
	[Test]
	public void FromStream_ReadsAllBytes()
	{
		var source = CreatePattern( 10_000 );
		using var ms = new MemoryStream( source, writable: false );

		var blob = Blob.FromStream( ms, chunkSize: 1024 );

		Assert.That( blob.Length, Is.EqualTo( source.Length ) );
		using var stream = blob.ToStream();
		var target = new byte[ source.Length ];
		var read = stream.Read( target, 0, target.Length );

		Assert.That( read, Is.EqualTo( source.Length ) );
		Assert.That( source, Is.EquivalentTo( target ) );
	}

	[Test]
	public void ToStream_ReadsAcrossSegmentBoundaries()
	{
		var a = CreateRange( 0, 7 );
		var b = CreateRange( 7, 5 );
		var c = CreateRange( 12, 4 );
		var expected = Concat( a, b, c );

		var blob = new Blob( [a, b, c] );
		using var stream = blob.ToStream();

		var buffer = new byte[ 16 ];
		var read = stream.Read( buffer, 0, buffer.Length );

		Assert.That( read, Is.EqualTo( 16 ) );
		Assert.That( expected, Is.EquivalentTo( buffer ) );
	}

	[Test]
	public void Read_PartialThenContinue_MaintainsPosition()
	{
		var blob = new Blob( [
			CreateRange( 0, 4 ),
			CreateRange( 4, 4 ),
			CreateRange( 8, 4 )
		] );
		using var stream = blob.ToStream();

		var first = new byte[ 5 ];
		var second = new byte[ 7 ];
		var r1 = stream.Read( first, 0, first.Length );
		var r2 = stream.Read( second, 0, second.Length );

		Assert.That( r1, Is.EqualTo( 5 ) );
		Assert.That( r2, Is.EqualTo( 7 ) );
		Assert.That( new byte[] { 0, 1, 2, 3, 4 }, Is.EquivalentTo( first ) );
		Assert.That( new byte[] { 5, 6, 7, 8, 9, 10, 11 }, Is.EquivalentTo( second ) );
		Assert.That( stream.Position, Is.EqualTo( blob.Length ) );
	}

	[Test]
	public void Seek_BeginCurrentEnd_Works()
	{
		var blob = new Blob( [
			CreateRange( 0, 10 ),
			CreateRange( 10, 10 )
		] );
		using var stream = blob.ToStream();
		var one = new byte[ 1 ];

		stream.Seek( 12, SeekOrigin.Begin );
		Assert.That( stream.Position, Is.EqualTo( 12 ) );
		Assert.That( stream.Read( one, 0, 1 ), Is.EqualTo( 1 ) );
		Assert.That( one[ 0 ], Is.EqualTo( 12 ) );

		stream.Seek( -2, SeekOrigin.Current );
		Assert.That( stream.Position, Is.EqualTo( 11 ) );
		Assert.That( stream.Read( one, 0, 1 ), Is.EqualTo( 1 ) );
		Assert.That( one[ 0 ], Is.EqualTo( 11 ) );

		stream.Seek( -1, SeekOrigin.End );
		Assert.That( stream.Position, Is.EqualTo( 19 ) );
		Assert.That( stream.Read( one, 0, 1 ), Is.EqualTo( 1 ) );
		Assert.That( one[ 0 ], Is.EqualTo( 19 ) );
		Assert.That( stream.Position, Is.EqualTo( blob.Length ) );
	}

	[Test]
	public void Seek_OutsideBounds_ThrowsIOException()
	{
		var blob = new Blob( [CreateRange( 0, 10 )] );
		using var stream = blob.ToStream();

		Assert.Throws<IOException>( () => stream.Seek( -1, SeekOrigin.Begin ) );
		Assert.Throws<IOException>( () => stream.Seek( 11, SeekOrigin.Begin ) );
		Assert.Throws<IOException>( () => stream.Seek( 1, SeekOrigin.End ) );
	}

	[Test]
	public void Read_AtEnd_ReturnsZero()
	{
		var blob = new Blob( [CreateRange( 0, 3 )] );
		using var stream = blob.ToStream();
		stream.Seek( 0, SeekOrigin.End );

		var buffer = new byte[ 8 ];
		var read = stream.Read( buffer, 0, buffer.Length );

		Assert.That( read, Is.EqualTo( 0 ) );
	}

	[Test]
	public void Read_WithInvalidArguments_Throws()
	{
		var blob = new Blob( [CreateRange( 0, 10 )] );
		using var stream = blob.ToStream();
		var buffer = new byte[ 10 ];

		Assert.Throws<ArgumentNullException>( () => stream.ReadExactly( null!, 0, 1 ) );
		Assert.Throws<ArgumentOutOfRangeException>( () => stream.ReadExactly( buffer, -1, 1 ) );
		Assert.Throws<ArgumentOutOfRangeException>( () => stream.ReadExactly( buffer, 0, -1 ) );
		Assert.Throws<ArgumentOutOfRangeException>( () => stream.ReadExactly( buffer, 8, 3 ) );
	}

	[Test]
	public void Stream_IsReadOnly()
	{
		var blob = new Blob( [CreateRange( 0, 10 )] );
		using var stream = blob.ToStream();

		Assert.That( stream.CanRead, Is.True );
		Assert.That( stream.CanSeek, Is.True );
		Assert.That( stream.CanWrite, Is.False );
		Assert.Throws<NotSupportedException>( () => stream.Write( new byte[ 1 ], 0, 1 ) );
		Assert.Throws<NotSupportedException>( () => stream.SetLength( 1 ) );
	}

	[Test]
	[Explicit( "Allocates very large memory. Run manually only." )]
	public void LargeLogicalBlob_UsesLongLengthAndCanSeekPastIntMax()
	{
		const int segmentSize = 1_000_000;
		const int segmentCount = 3_000; // 3,000,000,000 bytes
		var data = new byte[ segmentCount ][];

		for( var i = 0; i < segmentCount; i++ )
		{
			data[ i ] = new byte[ segmentSize ];
			data[ i ][ 0 ] = (byte)( i % 251 );
		}

		var blob = new Blob( data );
		using var stream = blob.ToStream();

		var target = 2_500_000_000L;
		stream.Seek( target, SeekOrigin.Begin );

		Assert.That( blob.Length, Is.EqualTo( 3_000_000_000L ) );
		Assert.That( stream.Position, Is.EqualTo( target ) );
	}

	private static byte[] CreatePattern( int length )
	{
		var data = new byte[ length ];
		for( var i = 0; i < data.Length; i++ )
			data[ i ] = (byte)( i % 251 );
		return data;
	}

	private static byte[] CreateRange( int start, int count )
	{
		var data = new byte[ count ];
		for( var i = 0; i < count; i++ )
			data[ i ] = (byte)( start + i );
		return data;
	}

	private static byte[] Concat( params byte[][] arrays )
	{
		var total = 0;
		foreach( var t in arrays )
			total += t.Length;

		var result = new byte[ total ];
		var offset = 0;
		foreach( var t in arrays )
		{
			Buffer.BlockCopy( t, 0, result, offset, t.Length );
			offset += t.Length;
		}

		return result;
	}
}