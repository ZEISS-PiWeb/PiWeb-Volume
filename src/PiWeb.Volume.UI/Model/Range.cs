#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.UI.Model
{
    #region usings

    using System;

    #endregion

    public class Range
    {
        #region constructors

        public Range( double start, double stop )
        {
            Start = start;
            Stop = stop;
        }

        #endregion

        #region properties

        public double Start { get; set; }
        public double Stop { get; set; }

        public double Lower => Math.Min( Start, Stop );

        public double Upper => Math.Max( Start, Stop );

        public double Size => Math.Abs( Stop - Start );

        #endregion

        #region methods

        public void Invert()
        {
            var tmp = Start;
            Start = Stop;
            Stop = tmp;
        }

        private double StartToEnd()
        {
            return Stop - Start;
        }

        public bool Contains( double value )
        {
            if( Start < Stop )
            {
                return value >= Start && value <= Stop;
            }
            else
            {
                return value >= Stop && value <= Start;
            }
        }

        public static double Clip( Range range, double value )
        {
            if( !range.Contains( value ) )
            {
                if( range.Start < range.Stop )
                {
                    if( value < range.Start ) return range.Start;
                    if( value > range.Stop ) return range.Stop;
                }
                else if( range.Start > range.Stop )
                {
                    if( value > range.Start ) return range.Start;
                    if( value < range.Stop ) return range.Stop;
                }
                else
                {
                    return range.Start;
                }
            }

            return value;
        }

        public static double Transform( double value, Range sourceRange, Range destinationRange )
        {
            if( !sourceRange.Contains( value ) )
                return double.NaN;

            return ( value - sourceRange.Start ) / sourceRange.StartToEnd() * destinationRange.StartToEnd() + destinationRange.Start;
        }

        #endregion
    }
}