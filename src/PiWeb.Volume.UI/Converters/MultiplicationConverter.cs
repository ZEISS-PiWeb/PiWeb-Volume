#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.Converters
{
	#region usings

	using System;
	using System.Globalization;
	using System.Windows.Data;

	#endregion

	public class MultiplicationConverter : IValueConverter
	{
		#region properties

		public double Factor { get; set; }

		#endregion

		#region interface IValueConverter

		public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
		{
			if( value is double doubleValue )
				return doubleValue * Factor;

			if( value is int intValue )
				return intValue * Factor;

			if( value is ushort ushortValue )
				return ushortValue * Factor;

			return value;
		}

		public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
		{
			if( value is double doubleValue )
				return doubleValue / Factor;

			if( value is int intValue )
				return intValue / Factor;

			if( value is ushort ushortValue )
				return ushortValue / Factor;

			return value;
		}

		#endregion
	}
}