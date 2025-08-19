#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Viewer.ViewModel;

#region usings

using Zeiss.PiWeb.Volume.UI.Interfaces;
using Zeiss.PiWeb.Volume.UI.ViewModel;

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