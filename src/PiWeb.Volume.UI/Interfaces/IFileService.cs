#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.Interfaces
{
	public interface IFileService
	{
		#region methods

		bool SelectOpenFileName( out string fileName );

		bool SelectSaveFileName( out string fileName );

		#endregion
	}
}