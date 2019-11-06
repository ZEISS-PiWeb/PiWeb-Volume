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

    #endregion

    /// <summary>
    /// Describes a continous range of slices in a specific direction.
    /// </summary>
    public struct VolumeSliceRangeDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeSliceRangeDefinition"/> struct.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="first">The first.</param>
        /// <param name="last">The last.</param>
        public VolumeSliceRangeDefinition( Direction direction, ushort first, ushort last )
        {
            First = Math.Min( first, last );
            Last = Math.Max( first, last );
            Direction = direction;
        }

        /// <summary>
        /// Gets the direction.
        /// </summary>
        /// <value>
        /// The direction.
        /// </value>
        public Direction Direction { get; }

        /// <summary>
        /// Gets the inclusive first slice.
        /// </summary>
        public ushort First { get; }

        /// <summary>
        /// Gets the inclusive last slice.
        /// </summary>
        public ushort Last { get; }
    }
}