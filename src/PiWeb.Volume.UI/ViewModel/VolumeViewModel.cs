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
    using System.Buffers;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using GalaSoft.MvvmLight;
    using GalaSoft.MvvmLight.CommandWpf;
    using Range = Zeiss.IMT.PiWeb.Volume.UI.Model.Range;

    #endregion

    public class VolumeViewModel : ViewModelBase
    {
        #region constants

        private const ushort Minification = 4;

        #endregion

        #region members

        private readonly Dispatcher _Dispatcher;

        private readonly Volume _Preview;
        private int _SelectedLayerIndex;
        private int _MaxLayer;

        private WriteableBitmap _SelectedLayer;
        private WriteableBitmap _PreviewLayer;

        private Direction _Direction;
        private bool _ShowPreview;
        private int _MaxPreviewLayer;
        private IDisposable _Subcription;

        private Range _HorizontalRange;
        private Range _VerticalRange;

        #endregion

        #region constructors

        public VolumeViewModel( CompressedVolume model, Volume preview )
        {
            Volume = model;
            _Preview = preview;
            _Direction = Direction.Z;
            _Dispatcher = Dispatcher.CurrentDispatcher;
            _SelectedLayerIndex = model.Metadata.GetSize( Direction.Z ) / 2;

            Observable
                .FromEventPattern<EventArgs>( this, nameof(PreviewLayerChanged) )
                .Throttle( TimeSpan.FromMilliseconds( 100 ) )
                .Subscribe( n => UpdateLayerAsync() );

            UpdateProjection();
        }

        #endregion

        #region events

        public event EventHandler<EventArgs> PreviewLayerChanged;

        #endregion

        #region commands

        public ICommand SetDirectionCommand => new RelayCommand<Direction>( d => Direction = d );

        public ICommand ShowPreviousSliceCommand => new RelayCommand( () => SelectedLayerIndex--, () => SelectedLayerIndex > 0 );

        public ICommand ShowNextSliceCommand => new RelayCommand( () => SelectedLayerIndex++, () => SelectedLayerIndex < MaxLayer );

        #endregion

        #region properties

        public CompressedVolume Volume { get; }

        public bool ShowPreview
        {
            get => _ShowPreview;
            set => Set( ref _ShowPreview, value );
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

        public WriteableBitmap SelectedLayer
        {
            get => _SelectedLayer;
            set => Set( ref _SelectedLayer, value );
        }

        public WriteableBitmap PreviewLayer
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

        public Range HorizontalRange
        {
            get => _HorizontalRange;
            private set => Set( ref _HorizontalRange, value );
        }

        public Range VerticalRange
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

                    SelectedLayer = new WriteableBitmap( Volume.Metadata.SizeX, Volume.Metadata.SizeY, 96, 96,
                        PixelFormats.Gray8, BitmapPalettes.Gray256 );
                    PreviewLayer = new WriteableBitmap( _Preview.Metadata.SizeX, _Preview.Metadata.SizeY, 96, 96,
                        PixelFormats.Gray8, BitmapPalettes.Gray256 );
                    MaxLayer = Volume.Metadata.SizeZ - 1;
                    _MaxPreviewLayer = _Preview.Metadata.SizeZ - 1;
                    HorizontalRange = new Range( 0, Volume.Metadata.SizeX );
                    VerticalRange = new Range( 0, Volume.Metadata.SizeY );
                    break;
                case Direction.Y:
                    SelectedLayer = new WriteableBitmap( Volume.Metadata.SizeX, Volume.Metadata.SizeZ, 96, 96,
                        PixelFormats.Gray8, BitmapPalettes.Gray256 );
                    PreviewLayer = new WriteableBitmap( _Preview.Metadata.SizeX, _Preview.Metadata.SizeZ, 96, 96,
                        PixelFormats.Gray8, BitmapPalettes.Gray256 );
                    MaxLayer = Volume.Metadata.SizeY - 1;
                    _MaxPreviewLayer = _Preview.Metadata.SizeY - 1;
                    HorizontalRange = new Range( 0, Volume.Metadata.SizeX );
                    VerticalRange = new Range( 0, Volume.Metadata.SizeZ );
                    break;
                case Direction.X:
                    SelectedLayer = new WriteableBitmap( Volume.Metadata.SizeY, Volume.Metadata.SizeZ, 96, 96,
                        PixelFormats.Gray8, BitmapPalettes.Gray256 );
                    PreviewLayer = new WriteableBitmap( _Preview.Metadata.SizeY, _Preview.Metadata.SizeZ, 96, 96,
                        PixelFormats.Gray8, BitmapPalettes.Gray256 );
                    MaxLayer = Volume.Metadata.SizeX - 1;
                    _MaxPreviewLayer = _Preview.Metadata.SizeX - 1;
                    HorizontalRange = new Range( 0, Volume.Metadata.SizeY );
                    VerticalRange = new Range( 0, Volume.Metadata.SizeZ );
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

            var sliceIndex = Math.Min( ( ushort ) Math.Round( _SelectedLayerIndex / ( double ) Minification ), ( ushort ) _MaxPreviewLayer );
            var (width, height, slice) = FetchSlice( _Preview, sliceIndex );

            WriteImage( PreviewLayer, width, height, slice );
            ShowPreview = true;

            PreviewLayerChanged?.Invoke( this, EventArgs.Empty );
        }

        private void UpdateLayerAsync()
        {
            _Subcription?.Dispose();
            _Subcription = Observable
                .FromAsync( ct => Task.Run( () => FetchSlice( Volume, ( ushort ) _SelectedLayerIndex ), ct ) )
                .ObserveOn( _Dispatcher )
                .Subscribe( data =>
                {
                    var (width, height, slice) = data;
                    WriteImage( SelectedLayer, width, height, slice );
                    ShowPreview = false;
                } );
        }

        private (int width, int height, VolumeSlice slice) FetchSlice( Volume volume, ushort sliceIndex )
        {
            var slice = volume.GetSlice( new VolumeSliceDefinition( _Direction, sliceIndex ) );
            volume.Metadata.GetSliceSize( _Direction, out var width, out var height );

            return ( width, height, slice );
        }

        private static void WriteImage( WriteableBitmap bitmap, int width, int height, VolumeSlice slice )
        {
            var bufferSize = width * height;
            var buffer = ArrayPool<byte>.Shared.Rent( bufferSize );
            slice.CopyDataTo( buffer );

            var sourceRect = new Int32Rect( 0, 0, width, height );
            bitmap.WritePixels( sourceRect, buffer, width, 0, 0 );
            ArrayPool<byte>.Shared.Return( buffer );
        }

        #endregion
    }
}