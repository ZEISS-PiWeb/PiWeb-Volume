namespace Zeiss.PiWeb.Volume.Compare
{
	using System.Windows;
	using Zeiss.PiWeb.Volume.Compare.View;
	using Zeiss.PiWeb.Volume.Compare.ViewModel;
	using Zeiss.PiWeb.Volume.UI.Services;

	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		#region methods

		/// <inheritdoc />
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