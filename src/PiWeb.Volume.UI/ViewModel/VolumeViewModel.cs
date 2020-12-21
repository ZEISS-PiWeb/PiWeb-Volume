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

	using System;
	using System.Reactive.Linq;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Imaging;
	using System.Windows.Threading;
	using GalaSoft.MvvmLight;
	using GalaSoft.MvvmLight.CommandWpf;
	using Zeiss.IMT.PiWeb.Volume.UI.Model;

	#endregion

	public class VolumeViewModel : ViewModelBase
	{
		#region members

		private readonly Volume _Preview;
		private readonly ILogger _Logger;
		private readonly Dispatcher _Dispatcher;
		
		private int _SelectedLayerIndex;
		private int _MaxLayer;
		private int _MaxPreviewLayer;

		private Direction _Direction;
		private WriteableBitmap _SelectedLayerImage;
		private WriteableBitmap _PreviewLayerImage;
		
		private bool _ShowPreview;
		private IDisposable _Subcription;

		private VolumeSliceBuffer _SliceBuffer = new VolumeSliceBuffer();
		private DoubleRange _HorizontalRange;
		private DoubleRange _VerticalRange;

		private Layer _SelectedLayer;
		private Layer _PreviewLayer;

		#endregion

		#region constructors

		public VolumeViewModel( Volume model, Volume preview, ushort minification, ILogger logger )
		{
			Volume = model;
			Minification = minification;
			_Preview = preview;
			_Logger = logger;
			_Direction = Direction.Z;
			_Dispatcher = Dispatcher.CurrentDispatcher;
			_SelectedLayerIndex = model.Metadata.GetSize( Direction.Z ) / 2;

			Observable.FromEventPattern<EventArgs>( this, nameof(PreviewLayerChanged) )
				.Throttle( TimeSpan.FromMilliseconds( 100 ) )
				.Subscribe( n => UpdateLayerAsync() );

			UpdateProjection();
		}

		#endregion

		#region events

		public event EventHandler<EventArgs> PreviewLayerChanged;
		public event EventHandler<EventArgs> LayerChanged;

		#endregion

		#region commands

		public ICommand SetDirectionCommand => new RelayCommand<Direction>( d => Direction = d );

		public ICommand ShowPreviousSliceCommand => new RelayCommand( () => SelectedLayerIndex--, () => SelectedLayerIndex > 0 );

		public ICommand ShowNextSliceCommand => new RelayCommand( () => SelectedLayerIndex++, () => SelectedLayerIndex < MaxLayer );

		#endregion

		#region properties

		public ushort Minification { get; }

		public Volume Volume { get; }

		public bool ShowPreview
		{
			get => _ShowPreview;
			set
			{
				if( Set( ref _ShowPreview, value ) )
					RaisePropertyChanged( nameof(AvailableLayerImage) );
			}
		}

		public int MaxLayer
		{
			get => _MaxLayer;
			set => Set( ref _MaxLayer, value );
		}

		public int SelectedLayerIndex
		{
			get => _SelectedLayerIndex;
			set
			{
				Set( ref _SelectedLayerIndex, value );
				UpdatePreviewAsync();
			}
		}

		public WriteableBitmap SelectedLayerImage
		{
			get => _SelectedLayerImage;
			set => Set( ref _SelectedLayerImage, value );
		}

		public WriteableBitmap PreviewLayerImage
		{
			get => _PreviewLayerImage;
			set => Set( ref _PreviewLayerImage, value );
		}

		public WriteableBitmap AvailableLayerImage => ShowPreview ? PreviewLayerImage : SelectedLayerImage;

		public Layer SelectedLayer
		{
			get => _SelectedLayer;
			set => Set( ref _SelectedLayer, value );
		}

		public Layer PreviewLayer
		{
			get => _PreviewLayer;
			set => Set( ref _PreviewLayer, value );
		}

		public Direction Direction
		{
			get => _Direction;
			set
			{
				if( Set( ref _Direction, value ) ) UpdateProjection();
			}
		}

		public DoubleRange HorizontalRange
		{
			get => _HorizontalRange;
			private set => Set( ref _HorizontalRange, value );
		}

		public DoubleRange VerticalRange
		{
			get => _VerticalRange;
			private set => Set( ref _VerticalRange, value );
		}

		#endregion

		#region methods

		private void UpdateProjection()
		{
			switch( _Direction )
			{
				case Direction.Z:

					SelectedLayerImage = new WriteableBitmap( Volume.Metadata.SizeX, Volume.Metadata.SizeY, 96, 96,
						PixelFormats.Gray8, BitmapPalettes.Gray256 );
					PreviewLayerImage = new WriteableBitmap( _Preview.Metadata.SizeX, _Preview.Metadata.SizeY, 96, 96,
						PixelFormats.Gray8, BitmapPalettes.Gray256 );
					MaxLayer = Volume.Metadata.SizeZ - 1;
					_MaxPreviewLayer = _Preview.Metadata.SizeZ - 1;
					HorizontalRange = new DoubleRange( 0, Volume.Metadata.SizeX );
					VerticalRange = new DoubleRange( 0, Volume.Metadata.SizeY );
					break;
				case Direction.Y:
					SelectedLayerImage = new WriteableBitmap( Volume.Metadata.SizeX, Volume.Metadata.SizeZ, 96, 96,
						PixelFormats.Gray8, BitmapPalettes.Gray256 );
					PreviewLayerImage = new WriteableBitmap( _Preview.Metadata.SizeX, _Preview.Metadata.SizeZ, 96, 96,
						PixelFormats.Gray8, BitmapPalettes.Gray256 );
					MaxLayer = Volume.Metadata.SizeY - 1;
					_MaxPreviewLayer = _Preview.Metadata.SizeY - 1;
					HorizontalRange = new DoubleRange( 0, Volume.Metadata.SizeX );
					VerticalRange = new DoubleRange( 0, Volume.Metadata.SizeZ );
					break;
				case Direction.X:
					SelectedLayerImage = new WriteableBitmap( Volume.Metadata.SizeY, Volume.Metadata.SizeZ, 96, 96,
						PixelFormats.Gray8, BitmapPalettes.Gray256 );
					PreviewLayerImage = new WriteableBitmap( _Preview.Metadata.SizeY, _Preview.Metadata.SizeZ, 96, 96,
						PixelFormats.Gray8, BitmapPalettes.Gray256 );
					MaxLayer = Volume.Metadata.SizeX - 1;
					_MaxPreviewLayer = _Preview.Metadata.SizeX - 1;
					HorizontalRange = new DoubleRange( 0, Volume.Metadata.SizeY );
					VerticalRange = new DoubleRange( 0, Volume.Metadata.SizeZ );
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			var selectedLayerIndex = MaxLayer / 2;
			if( SelectedLayerIndex != selectedLayerIndex )
				SelectedLayerIndex = selectedLayerIndex;
			else
				UpdatePreviewAsync();
		}

		private void UpdatePreviewAsync()
		{
			_Subcription?.Dispose();

			var previewLayer = UpdateLayer( _Preview, Math.Min( ( ushort ) Math.Round( _SelectedLayerIndex / ( double ) Minification ), ( ushort ) _MaxPreviewLayer ) );
			WriteImage( PreviewLayerImage, previewLayer );
			PreviewLayer = previewLayer;
			ShowPreview = true;

			PreviewLayerChanged?.Invoke( this, EventArgs.Empty );
		}

		private void UpdateLayerAsync()
		{
			_Subcription?.Dispose();
			_Subcription = Observable
				.FromAsync( ct => Task.Run( () => UpdateLayer( Volume, ( ushort ) _SelectedLayerIndex ), ct ) )
				.ObserveOn( _Dispatcher )
				.Subscribe( layer =>
				{
					SelectedLayer = layer;
					WriteImage( SelectedLayerImage, layer );
					ShowPreview = false;

					LayerChanged?.Invoke( this, EventArgs.Empty );
				} );
		}

		private Layer UpdateLayer( Volume volume, ushort sliceIndex )
		{
			volume.GetSlice( _SliceBuffer, new VolumeSliceDefinition( _Direction, sliceIndex ), logger: _Logger );
			volume.Metadata.GetSliceSize( _Direction, out var width, out var height );

			return new Layer( _SliceBuffer.Data, width, height, sliceIndex );
		}

		private static void WriteImage( WriteableBitmap bitmap, Layer layer )
		{
			bitmap.WritePixels( new Int32Rect( 0, 0, layer.Width, layer.Height ), layer.Data, layer.Width, 0 );
		}

		#endregion
	}
}