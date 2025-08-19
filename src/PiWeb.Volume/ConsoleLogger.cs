#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume;

#region usings

using System;

#endregion

/// <summary>
/// This is a simple logging class that writes log messages to
/// <see cref="Console.Out"/>.
/// </summary>
public class ConsoleLogger : ILogger
{
	#region interface ILogger

	/// <inheritdoc />
	public void Log( LogLevel level, string message )
	{
		Console.Out.WriteLine( $"[{level}] {message}" );
	}

	#endregion
}