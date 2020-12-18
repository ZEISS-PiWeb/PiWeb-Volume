#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.Convert
{
	#region usings

	using System;
	using System.IO;

	#endregion

	internal class Uint8Stream : Stream
	{
		#region members

		private readonly Stream _BaseStream;
		private readonly byte _MinValue;
		private bool _Extrapolate;
		private double _Factor;
		private long _Offset;

		#endregion

		#region constructors

		public Uint8Stream( Stream baseStream )
		{
			_BaseStream = baseStream;
		}

		public Uint8Stream( Stream baseStream, byte minValue, byte maxValue )
		{
			_Offset = baseStream.Position;
			_BaseStream = baseStream;
			_MinValue = minValue;
			_Extrapolate = true;
			_Factor = ( double ) byte.MaxValue / ( maxValue - _MinValue );
		}

		#endregion

		#region properties

		public override bool CanRead => true;
		public override bool CanSeek => true;
		public override bool CanWrite => false;
		public override long Length => _BaseStream.Length - _Offset;

		public override long Position
		{
			get => _BaseStream.Position - _Offset;
			set => _BaseStream.Position = value - + _Offset;
		}

		#endregion

		#region methods

		public override void Flush()
		{
			_BaseStream.Flush();
		}

		public override int Read( byte[] buffer, int offset, int count )
		{
			var read = _BaseStream.Read( buffer, offset, count );

			if( _Extrapolate )
			{
				for( var i = 0; i < read; i++ )
					buffer[ i + offset ] = ( byte ) Math.Min( byte.MaxValue, Math.Max( byte.MinValue, buffer[ i + offset ] - _MinValue ) * _Factor );
			}

			return read;
		}

		public override long Seek( long offset, SeekOrigin origin )
		{
			if( origin == SeekOrigin.Begin )
				return _BaseStream.Seek( _Offset + offset, origin );
			else
				return _BaseStream.Seek( offset, origin );
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