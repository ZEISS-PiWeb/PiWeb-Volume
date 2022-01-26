#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.Converters
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Windows.Data;

	public class OptionsConverter : IValueConverter
	{
		#region interface IValueConverter

		public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
		{
			if( !( value is Dictionary<string, string> options ) )
				return null;

			return string.Join( ";", options.Select( o => $"{o.Key}={o.Value}" ) );
		}

		public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
		{
			if( !( value is string str ) )
				return null;

			var options = str.Split( new[] { ';' }, StringSplitOptions.RemoveEmptyEntries );
			var result = new Dictionary<string, string>();

			foreach( var option in options )
			{
				var keyvalue = option.Split( new[] { '=' }, StringSplitOptions.RemoveEmptyEntries );
				if( keyvalue.Length != 2 )
					return null;

				result.Add( keyvalue[ 0 ], keyvalue[ 1 ] );
			}

			return result;
		}

		#endregion
	}
}