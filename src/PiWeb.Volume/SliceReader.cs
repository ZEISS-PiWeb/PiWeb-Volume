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
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Zeiss.IMT.PiWeb.Volume.Interop;

    #endregion

    internal class SliceReader
    {
        #region members

        private readonly byte[][] _Data;
        private readonly Direction _ReadDirection;
        private readonly IProgress<VolumeSliceDefinition> _ProgressNotifier;
        private readonly CancellationToken _Ct;

        private readonly ushort _SizeX;
        private readonly ushort _SizeY;
        private readonly ushort _SizeZ;
        private ushort _CurrentSlice;
        private byte[] _Buffer;

        #endregion

        #region constructors

        internal SliceReader( VolumeMetadata metadata, byte[][] data, Direction readDirection = Direction.Z, IProgress<VolumeSliceDefinition> progressNotifier = null, CancellationToken ct = default( CancellationToken ) )
        {
            _Data = data;
            _ReadDirection = readDirection;
            _ProgressNotifier = progressNotifier;
            _Ct = ct;
            _CurrentSlice = 0;

            _SizeX = metadata.SizeX;
            _SizeY = metadata.SizeY;
            _SizeZ = metadata.SizeZ;

            _Buffer = new byte[0];

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

            switch( _ReadDirection )
            {
                case Direction.X:
                    return ReadInXDirection( pv, width, height );
                case Direction.Y:
                    return ReadInYDirection( pv, width, height );
                case Direction.Z:
                    return ReadInZDirection( pv, width, height );
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool ReadInXDirection( IntPtr pv, ushort width, ushort height )
        {
            if( _CurrentSlice >= _SizeX )
                return false;

            _ProgressNotifier?.Report( new VolumeSliceDefinition( _ReadDirection, _CurrentSlice ) );

            if( _Buffer.Length < width * height )
                _Buffer = new byte[width * height];

            var sx = _SizeX;

            Parallel.For( 0, Math.Min( _SizeY, height ), z =>
            {
                var input = _Data[ z ];
                var output = _Buffer;
                long inputIndex = _CurrentSlice;
                long outputIndex = z * width;

                for( var y = 0; y < _SizeY; y++ )
                {
                    output[ outputIndex ] = input[ inputIndex ];
                    outputIndex++;
                    inputIndex += sx;
                }
            } );

            Marshal.Copy( _Buffer, 0, pv, width * height );
            _CurrentSlice++;

            return true;
        }


        private bool ReadInYDirection( IntPtr pv, ushort width, ushort height )
        {
            if( _CurrentSlice >= _SizeY )
                return false;

            _ProgressNotifier?.Report( new VolumeSliceDefinition( _ReadDirection, _CurrentSlice ) );

            var data = _Data[ _CurrentSlice ];

            Parallel.For( 0, Math.Min( _SizeZ, height ), z => Marshal.Copy( data, _CurrentSlice * _SizeX, pv + z * width, _SizeX ) );

            _CurrentSlice++;

            return true;
        }


        private bool ReadInZDirection( IntPtr pv, ushort width, ushort height )
        {
            if( _CurrentSlice >= _SizeZ )
                return false;

            _ProgressNotifier?.Report( new VolumeSliceDefinition( _ReadDirection, _CurrentSlice ) );

            var data = _Data[ _CurrentSlice ];

            Parallel.For( 0, Math.Min( _SizeY, height ), y => Marshal.Copy( data, y * _SizeX, pv + y * width, _SizeX ) );

            _CurrentSlice++;

            return true;
        }

        #endregion
    }
}