#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.UI.Converters
{
	#region usings

	using System;
	using System.Globalization;
	using System.Linq;
	using System.Windows;
	using System.Windows.Data;

	#endregion

	public class MergeConverter : IMultiValueConverter
	{
		#region interface IMultiValueConverter

		public object Convert( object[] values, Type targetType, object parameter, CultureInfo culture )
		{
			if( values is null || values.Length == 0 )
				return null;

			var reference = values[ 0 ];
			if( values.Any( v => !Equals( v, reference ) ) )
				return DependencyProperty.UnsetValue;

			if( reference is IConvertible convertible )
				return SafeChangeType( convertible, targetType, culture );

			return reference;
		}

		public object[] ConvertBack( object value, Type[] targetTypes, object parameter, CultureInfo culture )
		{
			if (value is IConvertible convertible)
				return Enumerable.Repeat( 0, targetTypes.Length )
					.Select( i => SafeChangeType( convertible, targetTypes[i], culture ) )
					.ToArray();
			
			return Enumerable.Repeat( 0, targetTypes.Length )
				.Select( i => value )
				.ToArray();
		}

		private static object SafeChangeType( IConvertible value, Type targetType, IFormatProvider formatProvider )
		{
			var t = Nullable.GetUnderlyingType(targetType) ?? targetType;
			return System.Convert.ChangeType( value, t, formatProvider );
		}

		#endregion
	}
}