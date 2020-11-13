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
    using Zeiss.IMT.PiWeb.Volume.Interop;

    #endregion

    internal class PreviewCreator
    {
        #region members

        private readonly VolumeMetadata _Metadata;
        private readonly ushort _Minification;
        private readonly IProgress<VolumeSliceDefinition> _ProgressNotifier;
        private readonly CancellationToken _Ct;

        private readonly List<VolumeSlice> _PreviewData;
        private byte[] _Buffer = Array.Empty<byte>();
        private readonly byte[] _MinifiedSliceBuffer;
        private readonly ushort _PreviewSizeX;
        private readonly ushort _PreviewSizeY;
        private readonly ushort _PreviewSizeZ;

        #endregion

        #region constructors

        internal PreviewCreator( VolumeMetadata metadata, ushort minification, IProgress<VolumeSliceDefinition> progressNotifier = null, CancellationToken ct = default )
        {
            _Metadata = metadata ?? throw new ArgumentNullException( nameof(metadata) );
            _Minification = minification;
            _ProgressNotifier = progressNotifier;
            _Ct = ct;

            _PreviewSizeX = ( ushort ) ( _Metadata.SizeX / ( double ) minification );
            _PreviewSizeY = ( ushort ) ( _Metadata.SizeY / ( double ) minification );
            _PreviewSizeZ = ( ushort ) ( _Metadata.SizeZ / ( double ) minification );
            _MinifiedSliceBuffer = new byte[_PreviewSizeX * _PreviewSizeY];

            _PreviewData = new List<VolumeSlice>( _PreviewSizeZ );

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

        internal static UncompressedVolume CreatePreview( IReadOnlyList<VolumeSlice> data, VolumeMetadata metadata, ushort minification )
        {
            var sizeX = metadata.SizeX;
            var sizeY = metadata.SizeY;
            var sizeZ = metadata.SizeZ;

            var previewSizeX = ( ushort ) ( sizeX / ( double ) minification );
            var previewSizeY = ( ushort ) ( sizeY / ( double ) minification );
            var previewSizeZ = ( ushort ) ( sizeZ / ( double ) minification );

            if( previewSizeX < 2 || previewSizeY < 2 || previewSizeZ < 2 )
                throw new ArgumentOutOfRangeException( nameof(minification) );

            var result = new List<VolumeSlice>( previewSizeZ );
            var srcBufferSize = sizeX * sizeY;
            var destBufferSize = previewSizeX * previewSizeY;

            var srcBuffer = ArrayPool<byte>.Shared.Rent( srcBufferSize );
            var destBuffer = ArrayPool<byte>.Shared.Rent( destBufferSize );

            for( var z = 0; z < previewSizeZ; z++ )
            {
                data[ z ].CopyDataTo( srcBuffer );
                CreateMinifiedSlice( minification, previewSizeX, previewSizeY, sizeX, srcBuffer, destBuffer );

                result.Add( new VolumeSlice( Direction.Z, ( ushort ) result.Count, destBuffer, destBufferSize ) );
            }

            ArrayPool<byte>.Shared.Return( srcBuffer );
            ArrayPool<byte>.Shared.Return( destBuffer );

            var previewMetadata = new VolumeMetadata(
                previewSizeX,
                previewSizeY,
                previewSizeZ,
                metadata.ResolutionX * minification,
                metadata.ResolutionY * minification,
                metadata.ResolutionZ * minification );

            return Volume.CreateUncompressed( previewMetadata, result );
        }

        private static void CreateMinifiedSlice( ushort minification, ushort previewSizeX, ushort previewSizeY, ushort stride, byte[] srcBuffer, byte[] destBuffer )
        {
            var position = 0;

            for( ushort y = 0, oy = 0; y < previewSizeY; y++, oy += minification )
            for( ushort x = 0, ox = 0; x < previewSizeX; x++, ox += minification )
                destBuffer[ position++ ] = srcBuffer[ oy * stride + ox ];
        }

        internal UncompressedVolume GetPreview()
        {
            var previewMetadata = new VolumeMetadata(
                _PreviewSizeX,
                _PreviewSizeY,
                _PreviewSizeZ,
                _Metadata.ResolutionX * _Minification,
                _Metadata.ResolutionY * _Minification,
                _Metadata.ResolutionZ * _Minification );

            return Volume.CreateUncompressed( previewMetadata, _PreviewData );
        }

        internal void WriteSlice( IntPtr slice, ushort width, ushort height, ushort z )
        {
            _Ct.ThrowIfCancellationRequested();

            if( z % _Minification != 0 )
                return;

            var pz = z / _Minification;
            if( pz >= _PreviewSizeZ )
                return;

            _ProgressNotifier?.Report( new VolumeSliceDefinition( Direction.Z, z ) );

            var size = width * height;
            if( _Buffer.Length < size )
                _Buffer = new byte[size];

            Marshal.Copy( slice, _Buffer, 0, size );

            CreateMinifiedSlice( _Minification, _PreviewSizeX, _PreviewSizeY, width, _Buffer, _MinifiedSliceBuffer );
            _PreviewData.Add( new VolumeSlice( Direction.Z, ( ushort ) _PreviewData.Count, _MinifiedSliceBuffer, _MinifiedSliceBuffer.Length ) );
        }

        #endregion
    }
}