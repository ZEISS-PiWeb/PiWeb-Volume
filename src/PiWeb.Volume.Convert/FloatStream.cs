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

#endregion

internal class FloatStream : Stream
{
	#region members

	private readonly Stream _BaseStream;
	private readonly float _MinValue;
	private readonly float _Factor;
	private readonly long _Offset;

	private byte[]? _ByteBuffer;

	#endregion

	#region constructors

	public FloatStream( Stream baseStream, float minValue = 0.0f, float maxValue = 1.0f )
	{
		_Offset = baseStream.Position;
		_BaseStream = baseStream;
		_MinValue = minValue;
		_Factor = Math.Max( 1.0f, 1.0f / ( maxValue - minValue ) );
	}

	#endregion

	#region properties

	public override bool CanRead => true;
	public override bool CanSeek => true;
	public override bool CanWrite => false;
	public override long Length => ( _BaseStream.Length - _Offset ) / sizeof( ushort );

	public override long Position
	{
		get => ( _BaseStream.Position - _Offset ) / sizeof( ushort );
		set => _BaseStream.Position = _Offset + value * sizeof( ushort );
	}

	#endregion

	#region methods

	public override void Flush()
	{
		_BaseStream.Flush();
	}

	public override int Read( byte[] buffer, int offset, int count )
	{
		if( _ByteBuffer is null || _ByteBuffer.Length != count * sizeof( float ) )
			_ByteBuffer = new byte[ count * sizeof( float ) ];

		var read = _BaseStream.Read( _ByteBuffer, 0, count * sizeof( float ) );
		for( var i = 0; i < read / sizeof( float ); i++ )
		{
			var value = BitConverter.ToSingle( _ByteBuffer, i * sizeof(float) );
			buffer[ i + offset ] = (byte)( Math.Min( 1.0, Math.Max( 0.0, ( value - _MinValue ) * _Factor ) ) * byte.MaxValue );
		}

		return read / sizeof( float );
	}

	public override long Seek( long offset, SeekOrigin origin )
	{
		if( origin == SeekOrigin.Begin )
			return _BaseStream.Seek( _Offset + offset * sizeof( float ), origin );
		return _BaseStream.Seek( offset * sizeof( float ), origin );
	}

	public override void SetLength( long value )
	{
		throw new NotImplementedException();
	}

	public override void Write( byte[] buffer, int offset, int count )
	{
		throw new NotImplementedException();
	}

	#endregion
}