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

	public readonly struct DoubleRange
	{
		#region constructors

		public DoubleRange( double start, double stop )
		{
			Start = start;
			Stop = stop;
		}

		#endregion

		#region properties

		public double Start { get; }
		public double Stop { get; }

		public double Lower => Math.Min( Start, Stop );
		public double Upper => Math.Max( Start, Stop );
		public double Size => Math.Abs( Stop - Start );

		#endregion

		#region methods

		public DoubleRange Invert()
		{
			return new DoubleRange( Stop, Start );
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

		public static double Clip( DoubleRange range, double value )
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

		public static double Transform( double value, DoubleRange sourceRange, DoubleRange destinationRange )
		{
			if( !sourceRange.Contains( value ) )
				return double.NaN;

			return ( value - sourceRange.Start ) / sourceRange.StartToEnd() * destinationRange.StartToEnd() + destinationRange.Start;
		}

		#endregion
	}
}