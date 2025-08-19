#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.Model;

#region usings

using System;

#endregion

public class VolumeProgressEventArgs : EventArgs
{
	#region constructors

	public VolumeProgressEventArgs( double progress, string message )
	{
		Progress = progress;
		Message = message;
	}

	#endregion

	#region properties

	public double Progress { get; }

	public string Message { get; }

	#endregion
}