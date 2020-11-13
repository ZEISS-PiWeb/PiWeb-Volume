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
    using System.Threading.Tasks;

    #endregion

    /// <summary>
    /// A single layer of a discrete volume.
    /// </summary>
    public readonly struct VolumeSlice
    {
        #region members
        
        private readonly byte[] _Data;

        #endregion
        
        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeSlice"/> class.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="index">The index.</param>
        /// <param name="data">The data.</param>
        /// <param name="length">The length of the data.</param>
        /// <remarks>
        /// The content of the provided buffer will be copied. The buffer might be longer than the specified length.
        /// </remarks>
        internal VolumeSlice( Direction direction, ushort index, byte[] data, int length )
        {
            Direction = direction;
            Index = index;
            Length = length;
            _Data = StreamHelper.CompressBytes( data );
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets the direction.
        /// </summary>
        /// <value>
        /// The direction.
        /// </value>
        public Direction Direction { get; }

        /// <summary>
        /// Gets the index.
        /// </summary>
        /// <value>
        /// The index.
        /// </value>
        public ushort Index { get; }

        /// <summary>
        /// Gets the length of the slice data in bytes.
        /// </summary>
        /// <value>
        /// The length.
        /// </value>
        public int Length { get; }

        #endregion

        #region methods

        /// <summary>
        /// Copies the slice data to the specified target array.
        /// </summary>
        public void CopyDataTo( byte[] buffer )
        {
            if( buffer.Length < Length )
                throw new ArgumentOutOfRangeException( $"Invalid buffer size. The buffer has to be at least {Length} bytes." );
            
            StreamHelper.DecompressBytes( _Data, buffer );
        }

        /// <summary>
        /// Extracts the specified direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="index">The index.</param>
        /// <param name="volumeMetadata">The volume metadata.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException">direction - null</exception>
        internal static VolumeSlice Extract( Direction direction, ushort index, VolumeMetadata volumeMetadata, IReadOnlyList<VolumeSlice> data )
        {
            var sx = volumeMetadata.SizeX;
            var sy = volumeMetadata.SizeY;
            var sz = volumeMetadata.SizeZ;

            switch( direction )
            {
                case Direction.X:
                {
                    var length = sy * sz;
                    var result = ArrayPool<byte>.Shared.Rent( length );

                    Parallel.For( 0, sz, z =>
                    {
                        var buffer = ArrayPool<byte>.Shared.Rent( data[ z ].Length );
                        data[ z ].CopyDataTo( buffer );
                        
                        for( var y = 0; y < sy; y++ )
                        {
                            result[ z * sy + y ] = buffer[ y * sx + index ];
                        }
                        ArrayPool<byte>.Shared.Return( buffer );
                    } );

                    var slice = new VolumeSlice( direction, index, result, length );
                    ArrayPool<byte>.Shared.Return( result );

                    return slice;
                }

                case Direction.Y:
                {
                    var length = sx * sz;
                    var result = ArrayPool<byte>.Shared.Rent( length );

                    Parallel.For( 0, sz, z =>
                    {
                        var buffer = ArrayPool<byte>.Shared.Rent( data[ z ].Length );
                        data[ z ].CopyDataTo( buffer );
                        
                        Array.Copy( buffer, index * sx, result, z * sx, sx );
                        
                        ArrayPool<byte>.Shared.Return( buffer );
                    } );
                
                    var slice = new VolumeSlice( direction, index, result, length );
                    ArrayPool<byte>.Shared.Return( result );

                    return slice;
                }

                case Direction.Z:
                {
                    return data[ index ];
                }
                default:
                    throw new ArgumentOutOfRangeException( nameof(direction), direction, null );
            }
        }

        #endregion
    }
}