#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.UI.ViewModel
{
    #region usings

    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.CommandWpf;
    using Zeiss.IMT.PiWeb.Volume.Convert;
    using Zeiss.IMT.PiWeb.Volume.UI.Interfaces;
    using Zeiss.IMT.PiWeb.Volume.UI.Model;

    #endregion

    public class VolumeManagementViewModel : ViewModelBase
    {
        #region members

        private readonly IFileService _FileService;
        private readonly IMessageService _MessageService;
        private readonly IViewService _ViewService;
        private readonly ILogger _Logger = new ConsoleLogger();

        private VolumeViewModel _VolumeViewModel;
        private string _FileName;
        private string _ProgressMessage;
        private double _Progress;
        private bool _IsLoading;

        #endregion

        #region constructors

        public VolumeManagementViewModel(
            IFileService fileService,
            IMessageService messageService,
            IViewService viewService )
        {
            _FileService = fileService;
            _MessageService = messageService;
            _ViewService = viewService;
        }

        #endregion

        #region events

        public event EventHandler<EventArgs> VolumeChanged;

        #endregion

        #region commands

        public ICommand OpenVolumeCommand => new RelayCommand( ExecuteOpenVolume );

        public ICommand SaveVolumeCommand => new RelayCommand( ExecuteSaveVolume, CanExecuteSaveVolume );

        public ICommand DecompressCommand => new RelayCommand( ExecuteDecompress, CanExecuteDecompress );

        public ICommand CreatePreviewCommand => new RelayCommand( ExecuteCreatePreview, CanExecuteCreatePreview );

        #endregion

        #region properties

        public VolumeViewModel VolumeViewModel
        {
            get => _VolumeViewModel;
            set
            {
                if( _VolumeViewModel?.Volume is IDisposable disposable )
                    disposable.Dispose();

                if( Set( ref _VolumeViewModel, value ) )
                    VolumeChanged?.Invoke( this, EventArgs.Empty );
            }
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

        private async void ExecuteSaveVolume()
        {
            if( !_FileService.SelectSaveFileName( out var fileName ) )
                return;

            await using var stream = File.Create( fileName );

            var codecViewModel = new CodecViewModel();

            if( _ViewService.RequestView( codecViewModel ) != true )
                return;

            var options = codecViewModel.GetOptions();
            var multiDirection = codecViewModel.MultiDirection;

            IsLoading = true;

            var volume = VolumeViewModel.Volume;
            var progress = new VolumeProgress( volume );

            progress.ProgressChanged += OnProgressChanged;

            if( volume is UncompressedVolume uncompressedVolume )
                await Task.Run( () => uncompressedVolume.Save( stream, options, multiDirection, progress, _Logger ) );
            else if( volume is StreamedVolume streamedVolume )
                await Task.Run( () => streamedVolume.Save( stream, options, progress, _Logger ) );

            progress.ProgressChanged -= OnProgressChanged;

            ProgressMessage = null;
            Progress = 0.0;

            IsLoading = false;
        }

        private bool CanExecuteSaveVolume()
        {
            return VolumeViewModel?.Volume is UncompressedVolume ||
                   VolumeViewModel?.Volume is StreamedVolume;
        }

        private bool CanExecuteDecompress()
        {
            return VolumeViewModel?.Volume.GetCompressionState( Direction.Z ) == VolumeCompressionState.CompressedInDirection;
        }

        private async void ExecuteDecompress()
        {
            var volume = VolumeViewModel?.Volume as CompressedVolume;
            if( volume?.GetCompressionState( Direction.Z ) != VolumeCompressionState.CompressedInDirection )
                return;

            IsLoading = true;

            var progress = new VolumeProgress( volume );

            progress.ProgressChanged += OnProgressChanged;

            var decompressedVolume = await Task.Run( () => volume.Decompress( progress, _Logger ) );

            progress.ProgressChanged -= OnProgressChanged;

            ProgressMessage = null;
            Progress = 0.0;

            VolumeViewModel = new VolumeViewModel( decompressedVolume, decompressedVolume, 1, _Logger );

            IsLoading = false;
        }

        private bool CanExecuteCreatePreview()
        {
            return VolumeViewModel?.Volume is CompressedVolume || 
                   VolumeViewModel?.Volume is StreamedVolume;
        }

        private async void ExecuteCreatePreview()
        {
            await CreatePreview();
        }

        private async void ExecuteOpenVolume()
        {
            if( !_FileService.SelectOpenFileName( out var fileName ) )
                return;

            IsLoading = true;
            FileName = fileName;

            switch( Path.GetExtension( fileName ).ToLowerInvariant() )
            {
                case ".volx":
                    await LoadPiWebVolume();
                    break;
                case ".vgi":
                    await LoadVgiVolume();
                    break;
                case ".uint16_scv":
                    await LoadScvVolume();
                    break;
                default:
                    IsLoading = false;
                    FileName = null;
                    break;
            }
        }

        private async Task LoadPiWebVolume()
        {
            await using var stream = File.OpenRead( FileName );

            var volume = Volume.Load( stream, _Logger );

            ProgressMessage = null;
            Progress = 0.0;

            VolumeViewModel = new VolumeViewModel( volume, null, 4, _Logger );

            IsLoading = false;
        }

        private async Task LoadVgiVolume()
        {
            var uint16File = Path.ChangeExtension( FileName, ".uint16" );
            if( !File.Exists( uint16File ) )
            {
                _MessageService.ShowMessage(
                    MessageBoxImage.Error,
                    "A '.uint16' file with the same name as the vgi file must be placed in the same directory as the vgi file" );
                return;
            }

            var loadOptionsViewModel = new LoadOptionsViewModel();
            if( _ViewService.RequestView( loadOptionsViewModel ) != true )
                return;

            await using var vgi = File.OpenRead( FileName );
            await using var data = File.OpenRead( uint16File );

            var progress = new DoubleProgress();

            progress.ProgressChanged += OnProgressChanged;

            var volume = await Task.Run( () => ConvertVolume.FromVgi(
                vgi,
                data,
                loadOptionsViewModel.Extrapolate,
                loadOptionsViewModel.Minimum,
                loadOptionsViewModel.Maximum,
                loadOptionsViewModel.Streamed,
                progress, _Logger ) );

            progress.ProgressChanged -= OnProgressChanged;

            ProgressMessage = null;
            Progress = 0.0;

            VolumeViewModel = new VolumeViewModel( volume, volume, 1, _Logger );
            IsLoading = false;
        }

        private async Task LoadScvVolume()
        {
            var loadOptionsViewModel = new LoadOptionsViewModel();
            if( _ViewService.RequestView( loadOptionsViewModel ) != true )
                return;

            var scv = File.OpenRead( FileName );

            var progress = new DoubleProgress();

            progress.ProgressChanged += OnProgressChanged;

            var volume = await Task.Run( () => ConvertVolume.FromScv(
                scv,
                loadOptionsViewModel.Extrapolate,
                loadOptionsViewModel.Minimum,
                loadOptionsViewModel.Maximum,
                loadOptionsViewModel.Streamed,
                progress, _Logger ) );

            progress.ProgressChanged -= OnProgressChanged;

            if( volume is StreamedVolume streamed )
            {
                VolumeViewModel = new VolumeViewModel( streamed, null, 1, _Logger );
            }
            else
            {
                scv.Close();
                VolumeViewModel = new VolumeViewModel( volume, volume, 1, _Logger );
            }

            ProgressMessage = null;
            Progress = 0.0;

            IsLoading = false;
        }

        private async Task CreatePreview()
        {
            if( !( VolumeViewModel?.Volume is CompressedVolume ) &&
                !( VolumeViewModel?.Volume is StreamedVolume  ))
                return;

            var volume = VolumeViewModel.Volume;
            var progress = new VolumeProgress( volume );

            IsLoading = true;
            
            progress.ProgressChanged += OnProgressChanged;

            var preview = await Task.Run( () => volume.CreatePreview( 4, progress, _Logger ) );

            progress.ProgressChanged -= OnProgressChanged;

            ProgressMessage = null;
            Progress = 0.0;

            VolumeViewModel = new VolumeViewModel( volume, preview, 4, _Logger );

            IsLoading = false;
        }

        private void OnProgressChanged( object sender, VolumeProgressEventArgs e )
        {
            Progress = e.Progress;
            ProgressMessage = e.Message;
        }

        #endregion
    }
}