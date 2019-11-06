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

    internal class DirectionMap
    {
        #region members

        private byte[] _X;
        private byte[] _Y;
        private byte[] _Z;

        #endregion

        #region properties

        internal byte[] this[ Direction direction ]
        {
            get
            {
                if( direction == Direction.Z )
                    return _Z;
                if( direction == Direction.Y )
                    return _Y;
                if( direction == Direction.X )
                    return _X;
                throw new ArgumentOutOfRangeException( nameof(direction) );
            }
            set
            {
                if( direction == Direction.Z )
                    _Z = value;
                else if( direction == Direction.Y )
                    _Y = value;
                else if( direction == Direction.X )
                    _X = value;
                else
                    throw new ArgumentOutOfRangeException( nameof(direction) );
            }
        }

        #endregion
    }
}