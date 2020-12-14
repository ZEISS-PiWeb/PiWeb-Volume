#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.UI.Services
{
	#region usings

	using System.Windows;
	using Zeiss.IMT.PiWeb.Volume.UI.Interfaces;

	#endregion

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