#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.UI.ViewModel
{
    #region usings

    using System.IO;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.CommandWpf;
    using Zeiss.IMT.PiWeb.Volume.UI.Interfaces;
    using Zeiss.IMT.PiWeb.Volume.UI.Model;

    #endregion

    public class MainViewModel : ViewModelBase
    {
        #region members

        private readonly IFileService _FileService;
        private VolumeViewModel _VolumeViewModel;
        private string _FileName;
        private double _Progress;
        private string _ProgressMessage;

        private bool _IsLoading;

        #endregion

        #region constructors

        public MainViewModel( IFileService fileService )
        {
            _FileService = fileService;
        }

        #endregion

        #region commands

        public ICommand OpenVolumeCommand => new RelayCommand( ExecuteOpenVolume );

        #endregion

        #region properties

        public VolumeViewModel VolumeViewModel
        {
            get => _VolumeViewModel;
            set => Set( ref _VolumeViewModel, value );
        }

        public double Progress
        {
            get => _Progress;
            set => Set( ref _Progress, value );
        }

        public string ProgressMessage
        {
            get => _ProgressMessage;
            set => Set( ref _ProgressMessage, value );
        }

        public string FileName
        {
            get => _FileName;
            set => Set( ref _FileName, value );
        }

        public bool IsLoading
        {
            get => _IsLoading;
            set => Set( ref _IsLoading, value );
        }

        #endregion

        #region methods

        private async void ExecuteOpenVolume()
        {
            if( !_FileService.SelectOpenFileName( out var fileName ) )
                return;

            IsLoading = true;
            FileName = fileName;

            using( var stream = File.OpenRead( FileName ) )
            {
                var volume = Volume.Load( stream );

                var progress = new VolumeProgress( volume );

                progress.ProgressChanged += OnProgressChanged;

                var preview = await Task.Run( () => volume.CreatePreview( 4, progress ) );

                progress.ProgressChanged -= OnProgressChanged;

                ProgressMessage = null;
                Progress = 0.0;

                VolumeViewModel = new VolumeViewModel( volume, preview );
                IsLoading = false;
            }
        }

        private void OnProgressChanged( object sender, VolumeProgressEventArgs e )
        {
            Progress = e.Progress;
            ProgressMessage = e.Message;
        }

        #endregion
    }
}