#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.Extensions
{
	#region usings

	using System;
	using System.Globalization;
	using System.Windows.Markup;

	#endregion

	public class HexExtension : MarkupExtension
	{
		#region constructors

		public HexExtension( string value )
		{
			Value = value;
		}

		#endregion

		#region properties

		public string Value { get; set; }

		#endregion

		#region methods

		public override object ProvideValue( IServiceProvider serviceProvider )
		{
			if( byte.TryParse( Value, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var result ) )
				return result;
			return 0;
		}

		#endregion
	}
}