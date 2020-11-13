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
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Zeiss.IMT.PiWeb.Volume.Interop;

    #endregion

    internal class FullVolumeWriter
    {
        #region members

        private readonly IProgress<ushort> _ProgressNotifier;
        private readonly CancellationToken _Ct;

        private readonly List<Task<VolumeSlice>> _Data;

        private readonly ushort _SizeX;
        private readonly ushort _SizeY;
        private readonly ushort _SizeZ;
        private readonly byte[] _Buffer;

        #endregion

        #region constructors

        internal FullVolumeWriter( VolumeMetadata metadata, Direction direction, IProgress<ushort> progressNotifier = null, CancellationToken ct = default )
        {
            if( metadata == null )
                throw new ArgumentNullException( nameof(metadata) );
            _ProgressNotifier = progressNotifier;
            _Ct = ct;

            metadata.GetSliceSize( direction, out _SizeX, out _SizeY );

            _SizeZ = metadata.GetSize( direction );
            _Data = new List<Task<VolumeSlice>>( _SizeZ );
            _Buffer = new byte[ _SizeX * _SizeY ];

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

        internal IReadOnlyList<VolumeSlice> GetData()
        {
            // ReSharper disable once CoVariantArrayConversion
            Task.WaitAll( _Data.ToArray(), _Ct );
            return _Data.Select( t => t.Result ).ToArray();
        }

        private void WriteSlice( IntPtr slice, ushort width, ushort height, ushort z )
        {
            _Ct.ThrowIfCancellationRequested();

            if( z >= _SizeZ )
                return;

            _ProgressNotifier?.Report( z );

            for( var y = 0; y < _SizeY; y++ )
                Marshal.Copy( slice + y * width, _Buffer, y * _SizeX, _SizeX );

            _Data.Add( Task.Run( () => new VolumeSlice( Direction.Z, z, _Buffer, _Buffer.Length ), _Ct ) );
        }

        #endregion
    }
}