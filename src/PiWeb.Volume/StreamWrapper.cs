#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume
{
    #region usings

    using System;
    using System.Buffers;
    using System.IO;
    using System.Runtime.InteropServices;
    using Zeiss.IMT.PiWeb.Volume.Interop;

    #endregion

    internal class StreamWrapper
    {
        #region members

        private readonly Stream _Stream;
        internal InteropStream Interop;

        #endregion

        #region constructors

        internal StreamWrapper( Stream stream )
        {
            _Stream = stream ?? throw new ArgumentNullException();
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
            var buffer = ArrayPool<byte>.Shared.Rent( cb );

            var result = _Stream.Read( buffer, 0, cb );
            Marshal.Copy( buffer, 0, pv, result );
            ArrayPool<byte>.Shared.Return( buffer );

            return result;
        }

        private int Write( IntPtr pv, int cb )
        {
            var buffer = ArrayPool<byte>.Shared.Rent( cb );

            Marshal.Copy( pv, buffer, 0, cb );
            _Stream.Write( buffer, 0, cb );
            ArrayPool<byte>.Shared.Return( buffer );

            return cb;
        }

        private long Seek( long dlibMove, SeekOrigin dwOrigin )
        {
            return _Stream.Seek( dlibMove, dwOrigin );
        }

        #endregion
    }
}