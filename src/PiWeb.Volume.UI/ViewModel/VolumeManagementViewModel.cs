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
		private VolumeViewModel _VolumeViewModel;
		private string _FileName;
		private double _Progress;
		private string _ProgressMessage;
		private bool _IsLoading;

		public event EventHandler<EventArgs> VolumeChanged;

		#endregion

		#region constructors

		public VolumeManagementViewModel(
			IFileService fileService,
			IMessageService messageService )
		{
			_FileService = fileService;
			_MessageService = messageService;
		}

		#endregion

		#region commands

		public ICommand OpenVolumeCommand => new RelayCommand( ExecuteOpenVolume );

		public ICommand DecompressCommand => new RelayCommand( ExecuteDecompress, CanExecuteDecompress );

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

			var decompressedVolume = await Task.Run( () => volume.Decompress( progress ) );

			progress.ProgressChanged -= OnProgressChanged;

			ProgressMessage = null;
			Progress = 0.0;

			VolumeViewModel = new VolumeViewModel( decompressedVolume, decompressedVolume, 1 );

			IsLoading = false;
		}

		#endregion

		#region properties

		public VolumeViewModel VolumeViewModel
		{
			get => _VolumeViewModel;
			set
			{
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
			using var stream = File.OpenRead( FileName );

			var volume = Volume.Load( stream );

			var progress = new VolumeProgress( volume );

			progress.ProgressChanged += OnProgressChanged;

			var preview = await Task.Run( () => volume.CreatePreview( 4, progress ) );

			progress.ProgressChanged -= OnProgressChanged;

			ProgressMessage = null;
			Progress = 0.0;

			VolumeViewModel = new VolumeViewModel( volume, preview, 4 );

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

			using var vgi = File.OpenRead( FileName );
			using var data = File.OpenRead( uint16File );

			var progress = new DoubleProgress();

			progress.ProgressChanged += OnProgressChanged;

			var volume = await Task.Run( () => ConvertVolume.FromVgi( vgi, data, progress ) );

			progress.ProgressChanged -= OnProgressChanged;

			ProgressMessage = null;
			Progress = 0.0;

			VolumeViewModel = new VolumeViewModel( volume, volume, 1 );
			IsLoading = false;
		}

		private async Task LoadScvVolume()
		{
			using var scv = File.OpenRead( FileName );

			var progress = new DoubleProgress();

			progress.ProgressChanged += OnProgressChanged;

			var volume = await Task.Run( () => ConvertVolume.FromScv( scv, progress ) );

			progress.ProgressChanged -= OnProgressChanged;

			ProgressMessage = null;
			Progress = 0.0;

			VolumeViewModel = new VolumeViewModel( volume, volume, 1 );
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