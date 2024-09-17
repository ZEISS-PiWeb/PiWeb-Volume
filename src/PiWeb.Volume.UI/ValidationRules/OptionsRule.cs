#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.ValidationRules
{
	#region usings

	using System;
	using System.Globalization;
	using System.Windows.Controls;

	#endregion

	public class OptionsRule : ValidationRule
	{
		#region methods

		public override ValidationResult Validate( object value, CultureInfo cultureInfo )
		{
			var str = ( string ) value;

			if( string.IsNullOrEmpty( str ) )
				return ValidationResult.ValidResult;

			var options = str.Split( new[] { ';' } );

			foreach( var option in options )
			{
				var keyvalue = option.Split( new[] { '=' }, StringSplitOptions.RemoveEmptyEntries );
				if( keyvalue.Length != 2 )
					return new ValidationResult( false, "Please specify your options separated by ';' in the scheme 'key=value'." );
			}

			return ValidationResult.ValidResult;
		}

		#endregion
	}
}