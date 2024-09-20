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

	internal class Int16Stream : Stream
	{
		#region members

		private readonly Stream _BaseStream;
		private readonly float _MinValue;
		private readonly double _Factor;
		private readonly long _Offset;

		private byte[]? _ByteBuffer;

		#endregion

		#region constructors

		/// <summary>
		/// Stream that reads int16 values and converts them to byte values while reading.
		/// </summary>
		public Int16Stream( Stream baseStream, short minValue = short.MinValue, short maxValue = short.MaxValue )
		{
			_Offset = baseStream.Position;
			_BaseStream = baseStream;
			_MinValue = minValue;
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
			if( _ByteBuffer is null || _ByteBuffer.Length != count * sizeof( short ) )
				_ByteBuffer = new byte[ count * sizeof( short ) ];

			var read = _BaseStream.Read( _ByteBuffer, 0, count * sizeof( short ) );
			for( var i = 0; i < read / sizeof( short ); i++ )
			{
				var value = BitConverter.ToInt16( _ByteBuffer, i * sizeof(short) );
				buffer[ i + offset ] = (byte)Math.Min( byte.MaxValue, Math.Max( byte.MinValue, ( value - _MinValue ) * _Factor * byte.MaxValue ) );
			}

			return read / sizeof( short );
		}

		public override long Seek( long offset, SeekOrigin origin )
		{
			if( origin == SeekOrigin.Begin )
				return _BaseStream.Seek( _Offset + offset * sizeof( short ), origin );
			return _BaseStream.Seek( offset * sizeof( short ), origin );
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