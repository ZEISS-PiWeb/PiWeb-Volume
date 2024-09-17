#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume
{
	/// <summary>
	/// This interface is responsible for log handling in the PiWeb volume library.
	/// </summary>
	public interface ILogger
	{
		#region methods

		/// <summary>
		/// Logs a message with the givel <paramref name="level"/>.
		/// </summary>
		/// <param name="level">The log level of the message.</param>
		/// <param name="message">The log message.</param>
		void Log( LogLevel level, string message );

		#endregion
	}
}