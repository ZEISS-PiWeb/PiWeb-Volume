#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.UI.Interfaces
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