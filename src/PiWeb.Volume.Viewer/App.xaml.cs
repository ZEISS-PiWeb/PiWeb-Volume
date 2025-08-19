#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Viewer
{
	#region usings

	using System.Windows;
	using Zeiss.PiWeb.Volume.UI.Services;
	using Zeiss.PiWeb.Volume.Viewer.View;
	using Zeiss.PiWeb.Volume.Viewer.ViewModel;

	#endregion

	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		#region methods

		protected override void OnStartup( StartupEventArgs e )
		{
			base.OnStartup( e );

			var mainWindowViewModel = new MainViewModel( new FileService(), new MessageService(), new ViewService() );

			var window = new MainView { DataContext = mainWindowViewModel };
			window.Show();
		}

		#endregion
	}
}