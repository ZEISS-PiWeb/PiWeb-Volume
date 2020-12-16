#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.Viewer.ViewModel
{
	#region usings

	using Zeiss.IMT.PiWeb.Volume.UI.Interfaces;
	using Zeiss.IMT.PiWeb.Volume.UI.ViewModel;

	#endregion

	public class MainViewModel : VolumeManagementViewModel
	{
		#region constructors

		public MainViewModel(
			IFileService fileService,
			IMessageService messageService,
			IViewService viewService )
			: base( fileService, messageService, viewService )
		{
		}

		#endregion
	}
}