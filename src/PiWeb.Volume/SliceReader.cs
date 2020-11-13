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
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Zeiss.IMT.PiWeb.Volume.Interop;

    #endregion

    internal class SliceReader
    {
        #region members

        private readonly IReadOnlyList<VolumeSlice> _Data;
        private readonly IProgress<VolumeSliceDefinition> _ProgressNotifier;
        private readonly CancellationToken _Ct;
        private readonly Direction _ReadDirection;

        private readonly ushort _SizeX;
        private readonly ushort _SizeY;
        private readonly ushort _SizeZ;

        private ushort _CurrentSlice;

        #endregion

        #region constructors

        internal SliceReader( VolumeMetadata metadata, IReadOnlyList<VolumeSlice> data, Direction readDirection = Direction.Z, IProgress<VolumeSliceDefinition> progressNotifier = null, CancellationToken ct = default )
        {
            _Data = data;
            _ReadDirection = readDirection;
            _ProgressNotifier = progressNotifier;
            _Ct = ct;
            _CurrentSlice = 0;

            _SizeX = metadata.SizeX;
            _SizeY = metadata.SizeY;
            _SizeZ = metadata.SizeZ;

            Interop = new InteropSliceReader
            {
                ReadSlice = ReadSlice
            };
        }

        #endregion

        #region properties

        internal InteropSliceReader Interop { get; }

        #endregion

        #region methods

        internal bool ReadSlice( IntPtr pv, ushort width, ushort height )
        {
            _Ct.ThrowIfCancellationRequested();

            return _ReadDirection switch
            {
                Direction.X => ReadInXDirection( pv, width, height ),
                Direction.Y => ReadInYDirection( pv, width, height ),
                Direction.Z => ReadInZDirection( pv, width, height ),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private bool ReadInXDirection( IntPtr pv, ushort width, ushort height )
        {
            if( _CurrentSlice >= _SizeX )
                return false;

            _ProgressNotifier?.Report( new VolumeSliceDefinition( _ReadDirection, _CurrentSlice ) );

            var bufferSize = width * height;
            var buffer = ArrayPool<byte>.Shared.Rent( bufferSize );

            var sx = _SizeX;

            Parallel.For( 0, Math.Min( _SizeZ, height ), z =>
            {
                var input = ArrayPool<byte>.Shared.Rent( _Data[ z ].Length );
                _Data[ z ].CopyDataTo( input );

                long inputIndex = _CurrentSlice;
                long outputIndex = z * width;

                for( var y = 0; y < _SizeY; y++ )
                {
                    buffer[ outputIndex ] = input[ inputIndex ];
                    outputIndex++;
                    inputIndex += sx;
                }

                ArrayPool<byte>.Shared.Return( input );
            } );

            Marshal.Copy( buffer, 0, pv, width * height );
            _CurrentSlice++;

            ArrayPool<byte>.Shared.Return( buffer );

            return true;
        }

        private bool ReadInYDirection( IntPtr pv, ushort width, ushort height )
        {
            if( _CurrentSlice >= _SizeY )
                return false;

            _ProgressNotifier?.Report( new VolumeSliceDefinition( _ReadDirection, _CurrentSlice ) );

            Parallel.For( 0, Math.Min( _SizeZ, height ), z =>
            {
                var input = ArrayPool<byte>.Shared.Rent( _Data[ z ].Length );
                _Data[ z ].CopyDataTo( input );
                Marshal.Copy( input, _CurrentSlice * _SizeX, pv + z * width, _SizeX );
                ArrayPool<byte>.Shared.Return( input );
            } );

            _CurrentSlice++;

            return true;
        }


        private bool ReadInZDirection( IntPtr pv, ushort width, ushort height )
        {
            if( _CurrentSlice >= _SizeZ )
                return false;

            _ProgressNotifier?.Report( new VolumeSliceDefinition( _ReadDirection, _CurrentSlice ) );

            var input = ArrayPool<byte>.Shared.Rent( _Data[ _CurrentSlice ].Length );
            _Data[ _CurrentSlice ].CopyDataTo( input );

            Parallel.For( 0, Math.Min( _SizeY, height ), y => Marshal.Copy( input, y * _SizeX, pv + y * width, _SizeX ) );
            _CurrentSlice++;

            ArrayPool<byte>.Shared.Return( input );

            return true;
        }

        #endregion
    }
}