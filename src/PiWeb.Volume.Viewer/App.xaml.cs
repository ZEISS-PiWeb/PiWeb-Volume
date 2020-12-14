﻿#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.Viewer
{
	#region usings

	using System.Windows;
	using Unity;
	using Zeiss.IMT.PiWeb.Volume.UI.Interfaces;
	using Zeiss.IMT.PiWeb.Volume.UI.Services;
	using Zeiss.IMT.PiWeb.Volume.Viewer.View;
	using Zeiss.IMT.PiWeb.Volume.Viewer.ViewModel;

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

			IUnityContainer container = new UnityContainer();

			container.RegisterType<IFileService, FileService>();
			container.RegisterType<IMessageService, MessageService>();

			var mainWindowViewModel = container.Resolve<MainViewModel>();
			var window = new MainView { DataContext = mainWindowViewModel };
			window.Show();
		}

		#endregion
	}
}