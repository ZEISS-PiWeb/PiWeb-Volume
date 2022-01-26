#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.Services
{
	#region usings

	using System.Windows;
	using Zeiss.PiWeb.Volume.UI.Interfaces;

	#endregion

	// ReSharper disable once ClassNeverInstantiated.Global
	public class MessageService : IMessageService
	{
		#region interface IMessageService

		public void ShowMessage( MessageBoxImage image, string message )
		{
			MessageBox.Show( Application.Current.MainWindow, message, string.Empty, MessageBoxButton.OK, image );
		}

		#endregion
	}
}