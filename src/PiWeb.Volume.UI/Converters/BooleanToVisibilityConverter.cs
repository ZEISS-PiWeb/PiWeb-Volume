#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.Converters
{
    #region usings

    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    #endregion

    public class BooleanToVisibilityConverter : IValueConverter
    {
        #region properties

        public bool Invert { get; set; }

        #endregion

        #region interface IValueConverter

        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if( !( value is bool boolValue ) )
                return Visibility.Collapsed;

            if( Invert )
                boolValue = !boolValue;

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}