namespace Zeiss.IMT.PiWeb.Volume.Compare
{
	using System.Windows;
	using Unity;
	using Zeiss.IMT.PiWeb.Volume.Compare.View;
	using Zeiss.IMT.PiWeb.Volume.Compare.ViewModel;
	using Zeiss.IMT.PiWeb.Volume.UI.Interfaces;
	using Zeiss.IMT.PiWeb.Volume.UI.Services;

	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		#region methods

		protected override void OnStartup( StartupEventArgs e )
		{
			base.OnStartup( e );

			IUnityContainer container = new UnityContainer();

			container.RegisterType<IFileService, FileService>();
			container.RegisterType<IMessageService, MessageService>();
			container.RegisterType<IViewService, ViewService>();

			var mainWindowViewModel = container.Resolve<MainViewModel>();
			var window = new MainView { DataContext = mainWindowViewModel };
			window.Show();
		}

		#endregion
	}
}