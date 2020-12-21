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

	using System.Windows;

	#endregion

	public interface IMessageService
	{
		#region methods

		void ShowMessage( MessageBoxImage image, string message );

		#endregion
	}
}