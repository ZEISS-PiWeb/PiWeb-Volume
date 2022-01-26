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

	using GalaSoft.MvvmLight;
	using Zeiss.PiWeb.Volume.UI.Interfaces;
	using Zeiss.PiWeb.Volume.UI.View;
	using Zeiss.PiWeb.Volume.UI.ViewModel;

	#endregion

	// ReSharper disable once ClassNeverInstantiated.Global
	public class ViewService : IViewService
	{
		#region interface IViewService

		public bool? RequestView( ViewModelBase viewModel )
		{
			switch( viewModel )
			{
				case CodecViewModel codecViewModel:
					return new CodecView { DataContext = codecViewModel }.ShowDialog();
				case LoadOptionsViewModel loadOptionsViewModel:
					return new LoadOptionsView { DataContext = loadOptionsViewModel }.ShowDialog();
			}

			return null;
		}

		#endregion
	}
}