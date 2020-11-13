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
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Zeiss.IMT.PiWeb.Volume.Interop;

    #endregion

    internal sealed class VolumeSliceRangeCollector : IDisposable
    {
        #region members

        private readonly Direction _Direction;
        private readonly IProgress<VolumeSliceDefinition> _ProgressNotifier;
        private readonly CancellationToken _Ct;

        private readonly Dictionary<ushort, SliceInfo> _SlicesX = new Dictionary<ushort, SliceInfo>();
        private readonly Dictionary<ushort, SliceInfo> _SlicesY = new Dictionary<ushort, SliceInfo>();
        private readonly Dictionary<ushort, SliceInfo> _SlicesZ = new Dictionary<ushort, SliceInfo>();

        private byte[] _Buffer;
        private readonly ushort _SizeX;
        private readonly ushort _SizeY;
        private readonly ushort _SizeZ;

        #endregion

        #region constructors

        internal VolumeSliceRangeCollector( VolumeMetadata metaData, Direction direction, IReadOnlyCollection<VolumeSliceRangeDefinition> ranges, IProgress<VolumeSliceDefinition> progressNotifier = null, CancellationToken ct = default )
        {
            if( direction != Direction.Z && ranges.Any( r => r.Direction != direction ) )
                throw new ArgumentException( "Collecting slices ranges for different directions than input is only allowed for z-directed input" );

            _SizeX = metaData.SizeX;
            _SizeY = metaData.SizeY;
            _SizeZ = metaData.SizeZ;

            _Direction = direction;
            _ProgressNotifier = progressNotifier;
            _Ct = ct;

            var maxLength = 0;
            foreach( var range in ranges )
            {
                var slices = GetSlices( range.Direction );
                for( var i = range.First; i <= range.Last; i++ )
                {
                    metaData.GetSliceSize( range.Direction, out var width, out var height );
                    var length = width * height;
                    var buffer = ArrayPool<byte>.Shared.Rent( length );

                    slices[ i ] = new SliceInfo( i, length, buffer );

                    maxLength = Math.Max( maxLength, length );
                }
            }

            _Buffer = new byte[maxLength];
            
            Interop = new InteropSliceWriter
            {
                WriteSlice = WriteSlice
            };
        }

        #endregion

        #region properties

        internal InteropSliceWriter Interop { get; }

        #endregion

        #region methods

        internal VolumeSliceCollection GetSliceRangeCollection()
        {
            return new VolumeSliceCollection(
                _SlicesX.ToDictionary( k => k.Key, v => v.Value.ToVolumeSlice( Direction.X ) ),
                _SlicesY.ToDictionary( k => k.Key, v => v.Value.ToVolumeSlice( Direction.Y ) ),
                _SlicesZ.ToDictionary( k => k.Key, v => v.Value.ToVolumeSlice( Direction.Z ) )
            );
        }

        internal VolumeSliceRange GetSliceRange( VolumeSliceRangeDefinition definition )
        {
            var slices = new List<VolumeSlice>( definition.Length );

            var set = GetSlices( definition.Direction );
            for( var index = definition.First; index <= definition.Last; index++ )
            {
                if( set.TryGetValue( index, out var data ) )
                    slices.Add( data.ToVolumeSlice( definition.Direction ) );
                else
                    throw new ArgumentOutOfRangeException( nameof(definition) );
            }

            return new VolumeSliceRange( definition, slices );
        }

        internal VolumeSlice GetSlice( Direction direction, ushort index )
        {
            var set = GetSlices( direction );
            if( set.TryGetValue( index, out var slice ) )
                return slice.ToVolumeSlice( direction );

            throw new ArgumentOutOfRangeException( nameof(index) );
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        private Dictionary<ushort, SliceInfo> GetSlices( Direction direction )
        {
            return direction switch
            {
                Direction.X => _SlicesX,
                Direction.Y => _SlicesY,
                Direction.Z => _SlicesZ,
                _ => throw new ArgumentOutOfRangeException( nameof(direction), direction, null )
            };
        }

        internal void WriteSlice( IntPtr slice, ushort width, ushort height, ushort index )
        {
            _Ct.ThrowIfCancellationRequested();
            _ProgressNotifier?.Report( new VolumeSliceDefinition( _Direction, index ) );

            switch( _Direction )
            {
                case Direction.X:
                    WriteXSlice( slice, width, height, index );
                    break;
                case Direction.Y:
                    WriteYSlice( slice, width, height, index );
                    break;
                case Direction.Z:
                    WriteZSlice( slice, width, height, index );
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal void WriteXSlice( IntPtr slice, ushort width, ushort height, ushort index )
        {
            if( index >= _SizeX )
                return;

            if( !_SlicesX.ContainsKey( index ) )
                return;

            for( var z = 0; z < _SizeZ; z++ )
                Marshal.Copy( slice + z * width, _SlicesX[ index ].Buffer, z * _SizeY, _SizeY );
        }

        internal void WriteYSlice( IntPtr slice, ushort width, ushort height, ushort index )
        {
            if( index >= _SizeY )
                return;

            if( !_SlicesY.ContainsKey( index ) )
                return;

            Parallel.For( 0, _SizeZ, z => Marshal.Copy( slice + z * width, _SlicesY[ index ].Buffer, z * _SizeX, _SizeX ) );
        }

        private void WriteZSlice( IntPtr slice, ushort width, ushort height, ushort index )
        {
            if( index >= _SizeZ )
                return;

            if( !_SlicesZ.ContainsKey( index ) && _SlicesX.Count == 0 && _SlicesY.Count == 0 )
                return;

            var length = width * height;

            if( _Buffer.Length < length )
                _Buffer = new byte[length];

            Marshal.Copy( slice, _Buffer, 0, length );
           
            if( _SlicesZ.TryGetValue( index, out var sliceInfo ) )
            {
                Parallel.For( 0, _SizeY, y => Array.Copy( _Buffer, y * width, sliceInfo.Buffer, y * _SizeX, _SizeX ) );
            }

            foreach( var sliceY in _SlicesY )
            {
                var y = sliceY.Key;
                Array.Copy( _Buffer, y * width, sliceY.Value.Buffer, index * _SizeX, _SizeX );
            }

            foreach( var sliceX in _SlicesX )
            {
                var x = sliceX.Key;
                for( var y = 0; y < _SizeY; y++ ) 
                    sliceX.Value.Buffer[ y + index * _SizeY ] = _Buffer[ x + width * y ];
            }
        }

        private void ReleaseUnmanagedResources()
        {
            foreach( var sliceInfo in _SlicesX.Values )
            {
                ArrayPool<byte>.Shared.Return( sliceInfo.Buffer );
            }

            foreach( var sliceInfo in _SlicesY.Values )
            {
                ArrayPool<byte>.Shared.Return( sliceInfo.Buffer );
            }

            foreach( var sliceInfo in _SlicesZ.Values )
            {
                ArrayPool<byte>.Shared.Return( sliceInfo.Buffer );
            }
        }

        #endregion

        /// <inheritdoc />
        ~VolumeSliceRangeCollector()
        {
            ReleaseUnmanagedResources();
        }

        #region interface IDisposable

        /// <inheritdoc />
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize( this );
        }

        #endregion

        #region class SliceInfo

        private readonly struct SliceInfo
        {
            public readonly byte[] Buffer;
            
            private readonly ushort _Index;
            private readonly int _Length;

            public SliceInfo( ushort index, int length, byte[] buffer )
            {
                _Index = index;
                _Length = length;
                Buffer = buffer;
            }

            public VolumeSlice ToVolumeSlice( Direction direction )
            {
                return new VolumeSlice( direction, _Index, Buffer, _Length );
            }
        }

        #endregion
    }
}