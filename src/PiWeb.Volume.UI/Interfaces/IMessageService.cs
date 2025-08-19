#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.Interfaces;

#region usings

using System.Windows;

#endregion

public interface IMessageService
{
	#region methods

	void ShowMessage( MessageBoxImage image, string message );

	#endregion
}