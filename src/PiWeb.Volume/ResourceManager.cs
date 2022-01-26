#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume
{
	#region usings

	using System.Globalization;
	using System.Resources;

	#endregion

	internal static class Resources
	{
		#region methods

		/// <summary>
		/// Gets the resource with the specified name
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name">The name.</param>
		/// <returns></returns>
		internal static string GetResource<T>( string name )
		{
			return new ResourceManager( typeof( T ) ).GetString( name, CultureInfo.CurrentUICulture );
		}

		/// <summary>
		/// Formats the resource with the specified <paramref name="name"/> with the specified <paramref name="args"/>.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name">The name.</param>
		/// <param name="args">The arguments.</param>
		/// <returns></returns>
		internal static string FormatResource<T>( string name, params object[] args )
		{
			var value = new ResourceManager( typeof( T ) ).GetString( name, CultureInfo.CurrentUICulture );
			if( string.IsNullOrEmpty( value ) )
				return "";

			return string.Format( value, args );
		}

		#endregion
	}
}