#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume;

#region usings

using System;
using System.IO;
using System.Runtime.InteropServices;
using Zeiss.PiWeb.Volume.Interop;

#endregion

internal class StreamWrapper
{
	#region members

	private readonly Stream _Stream;
	private byte[] _Buffer;

	internal InteropStream Interop;

	#endregion

	#region constructors

	internal StreamWrapper( Stream stream )
	{
		_Stream = stream ?? throw new ArgumentNullException();
		_Buffer = Array.Empty<byte>();
		Interop = new InteropStream
		{
			Read = Read,
			Write = Write,
			Seek = Seek
		};
	}

	#endregion

	#region methods

	private int Read( IntPtr pv, int cb )
	{
		if( cb > _Buffer.Length )
			_Buffer = new byte[ cb ];

		var result = _Stream.Read( _Buffer, 0, cb );
		Marshal.Copy( _Buffer, 0, pv, result );

		return result;
	}

	private int Write( IntPtr pv, int cb )
	{
		if( cb > _Buffer.Length )
			_Buffer = new byte[ cb ];

		Marshal.Copy( pv, _Buffer, 0, cb );
		_Stream.Write( _Buffer, 0, cb );

		return cb;
	}

	private long Seek( long dlibMove, SeekOrigin dwOrigin )
	{
		return _Stream.Seek( dlibMove, dwOrigin );
	}

	#endregion
}