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
	using System.IO;

	#endregion

	internal class Uint16Stream : Stream
	{
		#region members

		private readonly Stream _BaseStream;
		private readonly ushort _MinValue;
		private readonly bool _Extrapolate;
		private readonly double _Factor;
		private readonly long _Offset;

		private byte[]? _ByteBuffer;

		#endregion

		#region constructors

		public Uint16Stream( Stream baseStream )
		{
			_Offset = baseStream.Position;
			_BaseStream = baseStream;
		}

		public Uint16Stream( Stream baseStream, ushort minValue, ushort maxValue )
		{
			_Offset = baseStream.Position;
			_BaseStream = baseStream;
			_MinValue = minValue;
			_Extrapolate = true;
			_Factor = Math.Min( 1.0, 1.0 / ( maxValue - minValue ) );
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
			if( _ByteBuffer is null || _ByteBuffer.Length != count * sizeof(ushort) )
				_ByteBuffer = new byte[ count * sizeof( ushort ) ];

			var read = _BaseStream.Read( _ByteBuffer, 0, count * sizeof(ushort) );

			if( _Extrapolate )
			{
				for( var i = 0; i < read / 2; i++ )
				{
					var value = BitConverter.ToUInt16( _ByteBuffer, i * sizeof(ushort) );
					buffer[ i + offset ] = 	(byte)Math.Min( byte.MaxValue, Math.Max( byte.MinValue, ( value - _MinValue ) * _Factor * byte.MaxValue ) );
				}
			}
			else
			{
				for( var i = 0; i < read / 2; i++ )
				{
					buffer[ i + offset ] = _ByteBuffer[ i * 2 + 1 ];
				}
			}

			return read / 2;
		}

		public override long Seek( long offset, SeekOrigin origin )
		{
			if( origin == SeekOrigin.Begin )
				return _BaseStream.Seek( _Offset + offset * sizeof( ushort ), origin );
			return _BaseStream.Seek( offset * sizeof( ushort ), origin );
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
}