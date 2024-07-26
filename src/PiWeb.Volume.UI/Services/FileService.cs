#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.Services
{
    #region usings

    using Microsoft.Win32;
    using Zeiss.PiWeb.Volume.UI.Interfaces;

    #endregion

    // ReSharper disable once ClassNeverInstantiated.Global
    public class FileService : IFileService
    {
        #region constants

        private const string ReadableVolumeFileFilter = "All volume files|*.uint16_scv;*.uint8_scv;*.vgi;*.volx|PiWeb volumes|*.volx|Calypso volumes|*.uint16_scv;*.uint8_scv|VG volumes|*.vgi";
        private const string WritableVolumeFileFilter = "PiWeb volumes|*.volx|Calypso volumes|*.uint8_scv";

        #endregion

        #region interface IFileService

        public bool SelectOpenFileName( out string fileName )
        {
            fileName = null;

            var dialog = new OpenFileDialog
            {
                Filter = ReadableVolumeFileFilter,
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false
            };

            if( dialog.ShowDialog() != true || string.IsNullOrEmpty( dialog.FileName ) )
                return false;

            fileName = dialog.FileName;
            return true;
        }

        public bool SelectSaveFileName( out string fileName )
        {
	        fileName = null;

	        var dialog = new SaveFileDialog
	        {
		        Filter = WritableVolumeFileFilter
	        };

	        if( dialog.ShowDialog() != true || string.IsNullOrEmpty( dialog.FileName ) )
		        return false;

	        fileName = dialog.FileName;
	        return true;
        }

        #endregion
    }
}