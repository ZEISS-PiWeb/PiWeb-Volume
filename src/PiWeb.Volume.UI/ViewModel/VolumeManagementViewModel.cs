#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.ViewModel;

#region usings

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Zeiss.PiWeb.Volume.Convert;
using Zeiss.PiWeb.Volume.UI.Interfaces;
using Zeiss.PiWeb.Volume.UI.Model;

#endregion

public class VolumeManagementViewModel : ViewModelBase
{
	#region members

	private readonly IFileService _FileService;
	private readonly IMessageService _MessageService;
	private readonly IViewService _ViewService;
	private readonly ILogger _Logger = new ConsoleLogger();

	private VolumeViewModel? _VolumeViewModel;
	private string? _FileName;
	private string? _ProgressMessage;
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

	public event EventHandler<EventArgs>? VolumeChanged;

	#endregion

	#region commands

	public ICommand OpenVolumeCommand => new RelayCommand( ExecuteOpenVolume );

	public ICommand SaveVolumeCommand => new RelayCommand( ExecuteSaveVolume, CanExecuteSaveVolume );

	public ICommand DecompressCommand => new RelayCommand( ExecuteDecompress, CanExecuteDecompress );

	public ICommand CreatePreviewCommand => new RelayCommand( ExecuteCreatePreview, CanExecuteCreatePreview );

	#endregion

	#region properties

	public VolumeViewModel? VolumeViewModel
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

	public string? ProgressMessage
	{
		get => _ProgressMessage;
		set => Set( ref _ProgressMessage, value );
	}

	public string? FileName
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


		switch( Path.GetExtension( fileName ).ToLower() )
		{
			case ".volx":
				await SavePiWebVolume( fileName );
				break;
			case ".uint8_scv":
				await SaveCalypsoVolume( fileName );
				break;
			case ".img":
				await SaveJp3dVolume( fileName );
				break;
		}
	}

	private async Task SavePiWebVolume( string fileName )
	{
		await using var stream = File.Create( fileName );
		var codecViewModel = new CodecViewModel();

		if( _ViewService.RequestView( codecViewModel ) != true )
			return;

		if( VolumeViewModel is null )
			return;

		var options = codecViewModel.GetOptions();

		IsLoading = true;

		var volume = VolumeViewModel.Volume;
		var progress = new VolumeProgress( volume );

		progress.ProgressChanged += OnProgressChanged;

		if( volume is UncompressedVolume uncompressedVolume )
			await Task.Run( () => uncompressedVolume.Save( stream, options, false, progress, _Logger ) );
		else if( volume is StreamedVolume streamedVolume )
			await Task.Run( () => streamedVolume.Save( stream, options, progress, _Logger ) );

		progress.ProgressChanged -= OnProgressChanged;

		ProgressMessage = null;
		Progress = 0.0;

		IsLoading = false;
	}

	private async Task SaveCalypsoVolume( string fileName )
	{

		await using var stream = File.Create( fileName );
		IsLoading = true;

		var volume = VolumeViewModel?.Volume;
		if( volume is null )
			return;

		var progress = new VolumeProgress( volume );

		progress.ProgressChanged += OnProgressChanged;

		var scv = Scv.FromMetaData( volume.Metadata, 8 );
		scv.Write( stream );
		stream.Seek( scv.HeaderLength, SeekOrigin.Begin );

		await Task.Run( () =>
		{
			var buffer = new byte[ volume.Metadata.GetSliceLength( Direction.Z ) ];
			for( ushort z = 0; z < volume.Metadata.SizeZ; z++ )
			{
				volume.GetSlice( new VolumeSliceDefinition( Direction.Z, z ), buffer, progress );
				stream.Write( buffer, 0, buffer.Length );
			}
		} );

		progress.ProgressChanged -= OnProgressChanged;

		ProgressMessage = null;
		Progress = 0.0;

		IsLoading = false;
	}

	private async Task SaveJp3dVolume( string fileName )
	{
		IsLoading = true;

		var volume = VolumeViewModel?.Volume;
		if( volume is null )
			return;

		var progress = new VolumeProgress( volume );

		await using( var imgStream = File.Create( Path.ChangeExtension( fileName, ".img" ) ) )
		await using( var writer = new StreamWriter( imgStream, Encoding.ASCII ) )
		{
			await writer.WriteLineAsync( "Bpp\t8" );
			await writer.WriteLineAsync( "Color Map\t2" );
			await writer.WriteLineAsync( $"Dimensions\t{256} {256} {256}" );
		}

		progress.ProgressChanged += OnProgressChanged;

		await using( var binStream = File.Create( Path.ChangeExtension( fileName, ".bin" ) ) )
		{
			await Task.Run( () =>
			{
				var buffer = new byte[ volume.Metadata.GetSliceLength( Direction.Z ) ];
				for( ushort z = 0; z < 256; z++ )
				{
					volume.GetSlice( new VolumeSliceDefinition( Direction.Z, (ushort)( z + 256 ) ), buffer, progress );

					for( var y = 0; y < 256; y++ )
						binStream.Write( buffer, ( y + 256 ) * volume.Metadata.SizeX + 256, 256 );
				}
			} );
		}


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

		CommandManager.InvalidateRequerySuggested();
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

		try
		{
			switch( Path.GetExtension( fileName ).ToLowerInvariant() )
			{
				case ".volx":
					await LoadPiWebVolume( fileName );
					break;
				case ".vgi":
					await LoadVgiVolume( fileName );
					break;
				case ".uint8_scv":
				case ".uint16_scv":
					await LoadScvVolume( fileName );
					break;
				case ".gom_volume":
					await LoadGomVolume( fileName );
					break;
				case ".img":
					await LoadJp3dVolume( fileName );
					break;
				default:
					FileName = null;
					break;
			}
		}
		catch( Exception e )
		{
			_MessageService.ShowMessage( MessageBoxImage.Error, e.GetBaseException().Message );
		}
		finally
		{
			IsLoading = false;
		}
	}

	private async Task LoadPiWebVolume( string filename )
	{
		await using var stream = File.OpenRead( filename );

		var volume = Volume.Load( stream, _Logger );

		ProgressMessage = null;
		Progress = 0.0;

		VolumeViewModel = new VolumeViewModel( volume, null, 4, _Logger );
	}

	private async Task LoadVgiVolume( string filename )
	{
		var uint16File = Path.ChangeExtension( filename, ".uint16" );
		var loadOptionsViewModel = new LoadOptionsViewModel
		{
			MinimumValue = 0,
			MaximumValue = ushort.MaxValue,
			Minimum = 0,
			Maximum = ushort.MaxValue
		};

		if( _ViewService.RequestView( loadOptionsViewModel ) != true )
			return;

		await using var vgi = File.OpenRead( filename );
		await using var data = File.OpenRead( uint16File );

		var progress = new DoubleProgress();

		progress.ProgressChanged += OnProgressChanged;

		var volume = await Task.Run( () => ConvertVolume.FromVgi(
			vgi,
			data,
			loadOptionsViewModel.Extrapolate,
			(ushort)loadOptionsViewModel.Minimum,
			(ushort)loadOptionsViewModel.Maximum,
			loadOptionsViewModel.Streamed,
			progress, _Logger ) );

		progress.ProgressChanged -= OnProgressChanged;

		ProgressMessage = null;
		Progress = 0.0;

		VolumeViewModel = new VolumeViewModel( volume, volume, 1, _Logger );
	}

	private async Task LoadGomVolume( string filename )
	{
		await using var gomStream = File.OpenRead( filename );

		var metadata = Gom.ParseMetadata( gomStream, out var min, out var max, out var dataFile, out var rawDataType );
		var (minval, maxval) = rawDataType switch
		{
			Gom.DataType.Int16  => ( short.MinValue, short.MaxValue ),
			Gom.DataType.UInt16 => ( ushort.MinValue, ushort.MaxValue ),
			Gom.DataType.Single => ( -1.0f, 1.0f ),
			_                   => ( -1.0f, 1.0f ),
		};

		var dataPath = Path.Combine( Path.GetDirectoryName( filename )!, dataFile );
		var rawData = File.OpenRead( dataPath );

		var loadOptionsViewModel = new LoadOptionsViewModel
		{
			MinimumValue = minval,
			MaximumValue = maxval,
			Maximum = max,
			Minimum = min
		};

		if( _ViewService.RequestView( loadOptionsViewModel ) != true )
			return;

		var progress = new DoubleProgress();

		progress.ProgressChanged += OnProgressChanged;

		var volume = await Task.Run( () => ConvertVolume.FromGomVolume(
			metadata,
			rawData,
			rawDataType,
			loadOptionsViewModel.Extrapolate,
			loadOptionsViewModel.Minimum,
			loadOptionsViewModel.Maximum,
			loadOptionsViewModel.Streamed,
			progress,
			_Logger ) );

		progress.ProgressChanged -= OnProgressChanged;

		if( volume is StreamedVolume streamed )
		{
			VolumeViewModel = new VolumeViewModel( streamed, null, 1, _Logger );
		}
		else
		{
			rawData.Close();
			VolumeViewModel = new VolumeViewModel( volume, volume, 1, _Logger );
		}

		ProgressMessage = null;
		Progress = 0.0;
	}

	private async Task LoadScvVolume( string filename )
	{
		var bitDepthFromExtension = string.Equals( Path.GetExtension( filename ), ".uint16_scv" ) ? 16 : 8;
		var maximumValue = bitDepthFromExtension == 8 ? byte.MaxValue : ushort.MaxValue;
		var loadOptionsViewModel = new LoadOptionsViewModel
		{
			MinimumValue = 0,
			MaximumValue = maximumValue,
			Minimum = 0,
			Maximum = maximumValue
		};

		if( _ViewService.RequestView( loadOptionsViewModel ) != true )
			return;

		var scv = File.OpenRead( filename );

		var progress = new DoubleProgress();

		progress.ProgressChanged += OnProgressChanged;

		var volume = await Task.Run( () => ConvertVolume.FromScv(
			scv,
			bitDepthFromExtension,
			loadOptionsViewModel.Extrapolate,
			(ushort)loadOptionsViewModel.Minimum,
			(ushort)loadOptionsViewModel.Maximum,
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
	}

	private async Task LoadJp3dVolume( string filename )
	{
		await using var imgStream = File.OpenRead( filename );
		await using var binStream = File.OpenRead( Path.ChangeExtension( filename, ".bin" ) );

		var imgLines = await File.ReadAllLinesAsync( filename );
		if( imgLines.Length < 3 )
			return;

		var dimensionRegex = new Regex( "Dim.*\\s+(\\d+)\\s+(\\d+)\\s+(\\d+)" );
		var match = dimensionRegex.Match( imgLines[ 2 ] );
		if( !match.Success || match.Groups.Count < 4 )
			return;

		var sizex = ushort.Parse( match.Groups[ 1 ].Value );
		var sizey = ushort.Parse( match.Groups[ 2 ].Value );
		var sizez = ushort.Parse( match.Groups[ 3 ].Value );

		var metadata = new VolumeMetadata( sizex, sizey, sizez, 1, 1, 1 );
		var data = new byte[ sizez ][];
		var dataReader = new BinaryReader( binStream );
		for( var z = 0; z < sizez; z++ )
			data[ z ] = dataReader.ReadBytes( sizex * sizey );

		var volume = new UncompressedVolume( metadata, data );

		VolumeViewModel = new VolumeViewModel( volume, null, 1, _Logger );
		ProgressMessage = null;
		Progress = 0.0;
	}

	private async Task CreatePreview()
	{
		if( !( VolumeViewModel?.Volume is CompressedVolume ) &&
			!( VolumeViewModel?.Volume is StreamedVolume ) )
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

	private void OnProgressChanged( object? sender, VolumeProgressEventArgs e )
	{
		Progress = e.Progress;
		ProgressMessage = e.Message;
	}

	#endregion
}