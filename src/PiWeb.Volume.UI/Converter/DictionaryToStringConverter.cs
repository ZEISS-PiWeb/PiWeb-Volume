#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.UI.Converter
{
    #region usings

    using System;
    using System.Collections;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Data;

    #endregion

    public class DictionaryToStringConverter : IValueConverter
    {
        #region interface IValueConverter

        public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
        {
            if( value is IDictionary dictionary )
                return string.Join( ";", dictionary.Keys.OfType<object>().Select( key => $"-{key}={dictionary[ key ]}" ) );

            return string.Empty;
        }

        public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}