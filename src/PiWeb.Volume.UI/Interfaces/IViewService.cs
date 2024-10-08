#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.Interfaces
{
	#region usings

	using GalaSoft.MvvmLight;

	#endregion

	public interface IViewService
	{
		#region methods

		public bool? RequestView( ViewModelBase viewModel );

		#endregion
	}
}