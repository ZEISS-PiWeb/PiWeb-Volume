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
    using System.Threading.Tasks;

    #endregion

    /// <summary>
    /// A single layer of a discrete volume.
    /// </summary>
    public sealed class VolumeSlice
    {
        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeSlice"/> class.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="index">The index.</param>
        /// <param name="data">The data.</param>
        internal VolumeSlice( Direction direction, ushort index, byte[] data )
        {
            Direction = direction;
            Index = index;
            Data = data;
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
        /// Gets the data.
        /// </summary>
        /// <value>
        /// The data.
        /// </value>
        public byte[] Data { get; }

        #endregion

        #region methods

        /// <summary>
        /// Extracts the specified direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="index">The index.</param>
        /// <param name="volumeMetadata">The volume metadata.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentOutOfRangeException">direction - null</exception>
        internal static VolumeSlice Extract( Direction direction, ushort index, VolumeMetadata volumeMetadata, byte[][] data )
        {
            var sx = volumeMetadata.SizeX;
            var sy = volumeMetadata.SizeY;
            var sz = volumeMetadata.SizeZ;

            switch( direction )
            {
                case Direction.X:
                {
                    var result = new byte[sy * sz];

                    Parallel.For( 0, sz, z =>
                    {
                        for( var y = 0; y < sy; y++ )
                        {
                            result[ z * sy + y ] = data[ z ][ y * sx + index ];
                        }
                    } );

                    return new VolumeSlice( direction, index, result );
                }

                case Direction.Y:
                {
                    var result = new byte[sx * sz];

                    Parallel.For( 0, sz, z => { Array.Copy( data[ z ], index * sx, result, z * sx, sx ); } );

                    return new VolumeSlice( direction, index, result );
                }

                case Direction.Z:
                {
                    var result = new byte[sx * sy];
                    Array.Copy( data[ index ], 0, result, 0, sx * sy );
                    return new VolumeSlice( direction, index, result );
                }
                default:
                    throw new ArgumentOutOfRangeException( nameof(direction), direction, null );
            }
        }

        #endregion
    }
}