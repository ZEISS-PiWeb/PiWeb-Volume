#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume
{
    /// <summary>
    /// Holds the properties that define a volume slice.
    /// </summary>
    public struct VolumeSliceDefinition
    {
        /// <summary>
        /// Creates a decription of a volume slice.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="index"></param>
        public VolumeSliceDefinition( Direction direction, ushort index )
        {
            Direction = direction;
            Index = index;
        }

        /// <summary>
        /// Direction
        /// </summary>
        public Direction Direction { get; }

        /// <summary>
        /// Slice index
        /// </summary>
        public ushort Index { get; }
    }
}