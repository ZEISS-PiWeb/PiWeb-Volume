#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.UI.Components
{
    #region usings

    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using GalaSoft.MvvmLight.CommandWpf;
    using Range = Zeiss.IMT.PiWeb.Volume.UI.Model.Range;

    #endregion

    public class Navigator : ScrollViewer
    {
        #region members

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register( "Zoom", typeof( double ), typeof( Navigator ), new PropertyMetadata( 1.0, OnZoomChanged ) );

        public static readonly DependencyProperty MinZoomProperty =
            DependencyProperty.Register( "MinZoom", typeof( double ), typeof( Navigator ), new PropertyMetadata( 0.1 ) );

        public static readonly DependencyProperty MaxZoomProperty =
            DependencyProperty.Register( "MaxZoom", typeof( double ), typeof( Navigator ), new PropertyMetadata( 7.5 ) );

        public static readonly DependencyProperty ZoomMarginProperty =
            DependencyProperty.Register( "ZoomMargin", typeof( Thickness ), typeof( Navigator ), new PropertyMetadata( new Thickness( 15 ), OnZoomMarginChanged ) );

        public static readonly DependencyProperty ZoomModifierKeyProperty =
            DependencyProperty.Register( "ZoomModifierKey", typeof( ModifierKeys ), typeof( Navigator ), new PropertyMetadata( ModifierKeys.Control ) );

        public static readonly DependencyProperty IsZoomEnabledProperty =
            DependencyProperty.Register( "IsZoomEnabled", typeof( bool ), typeof( Navigator ),
                                         new PropertyMetadata( false ) );

        public static readonly DependencyProperty IsPanningEnabledProperty =
            DependencyProperty.Register( "IsPanningEnabled", typeof( bool ), typeof( Navigator ),
                                         new PropertyMetadata( false ) );

        public static readonly DependencyProperty PanButtonProperty =
            DependencyProperty.Register( "PanButton", typeof( MouseButton ), typeof( Navigator ), new PropertyMetadata( MouseButton.Right ) );

        public static readonly DependencyProperty HorizontalPanningProperty = DependencyProperty.Register(
            "HorizontalPanning", typeof( double ), typeof( Navigator ), new PropertyMetadata( default( double ), OnHorizontalPanningChanged ) );

        public static readonly DependencyProperty VerticalPanningProperty = DependencyProperty.Register(
            "VerticalPanning", typeof( double ), typeof( Navigator ), new PropertyMetadata( default( double ), OnVerticelPanningChanged ) );

        public static readonly DependencyProperty FitProperty = DependencyProperty.Register(
            "Fit", typeof( Rect ), typeof( Navigator ), new PropertyMetadata( Rect.Empty, OnFitChanged ) );

        public static readonly DependencyProperty HorizontalRangeProperty = DependencyProperty.Register(
            "HorizontalRange", typeof( Range ), typeof( Navigator ), new PropertyMetadata( default( Range ) ) );

        public static readonly DependencyProperty VerticalRangeProperty = DependencyProperty.Register(
            "VerticalRange", typeof( Range ), typeof( Navigator ), new PropertyMetadata( default( Range ) ) );
        

        public static readonly ICommand ZoomToContentSizeCommand = new RelayCommand<Navigator>( ExecuteZoomToContentSize );

        public static readonly ICommand ResetZoomCommand = new RelayCommand<Navigator>( ExecuteResetZoom );


        private Point? _PanStart;

        #endregion

        #region properties

        public Range HorizontalRange
        {
            get => ( Range ) GetValue( HorizontalRangeProperty );
            set => SetCurrentValue( HorizontalRangeProperty, value );
        }

        public Range VerticalRange
        {
            get => ( Range ) GetValue( VerticalRangeProperty );
            set => SetCurrentValue( VerticalRangeProperty, value );
        }

        public Rect Fit
        {
            get => ( Rect ) GetValue( FitProperty );
            set => SetValue( FitProperty, value );
        }

        public double VerticalPanning
        {
            get => ( double ) GetValue( VerticalPanningProperty );
            set => SetCurrentValue( VerticalPanningProperty, value );
        }

        public double HorizontalPanning
        {
            get => ( double ) GetValue( HorizontalPanningProperty );
            set => SetCurrentValue( HorizontalPanningProperty, value );
        }

        public double Zoom
        {
            get => ( double ) GetValue( ZoomProperty );
            set => SetCurrentValue( ZoomProperty, value );
        }

        public double MinZoom
        {
            get => ( double ) GetValue( MinZoomProperty );
            set => SetCurrentValue( MinZoomProperty, value );
        }

        public double MaxZoom
        {
            get => ( double ) GetValue( MaxZoomProperty );
            set => SetCurrentValue( MaxZoomProperty, value );
        }

        public Thickness ZoomMargin
        {
            get => ( Thickness ) GetValue( ZoomMarginProperty );
            set => SetCurrentValue( ZoomMarginProperty, value );
        }

        public ModifierKeys ZoomModifierKey
        {
            get => ( ModifierKeys ) GetValue( ZoomModifierKeyProperty );
            set => SetCurrentValue( ZoomModifierKeyProperty, value );
        }

        public bool IsZoomEnabled
        {
            get => ( bool ) GetValue( IsZoomEnabledProperty );
            set => SetCurrentValue( IsZoomEnabledProperty, value );
        }

        public bool IsPanningEnabled
        {
            get => ( bool ) GetValue( IsPanningEnabledProperty );
            set => SetCurrentValue( IsPanningEnabledProperty, value );
        }

        public MouseButton PanButton
        {
            get => ( MouseButton ) GetValue( PanButtonProperty );
            set => SetCurrentValue( PanButtonProperty, value );
        }

        #endregion

        #region methods

        protected override void OnRenderSizeChanged( SizeChangedInfo sizeInfo )
        {
            base.OnRenderSizeChanged( sizeInfo );

            ApplyZoom();
        }

        protected override void OnContentChanged( object oldContent, object newContent )
        {
            base.OnContentChanged( oldContent, newContent );

            if( oldContent is FrameworkElement oldElement )
                oldElement.SizeChanged -= OnContentSizeChanged;

            if( newContent is FrameworkElement newElement )
                newElement.SizeChanged += OnContentSizeChanged;

            ApplyZoom();
            ScrollAndZoomIntoView( Fit );
        }

        private void OnContentSizeChanged( object sender, SizeChangedEventArgs e )
        {
            if( sender is FrameworkElement content )
                content.SizeChanged -= OnContentSizeChanged;


            ApplyZoom();
            ScrollAndZoomIntoView( Fit );
        }

        private static void OnHorizontalPanningChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
        {
            ( d as Navigator )?.ScrollToHorizontalOffset( ( double ) e.NewValue );
            ( d as Navigator )?.UpdateRanges();
        }

        private static void OnVerticelPanningChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
        {
            ( d as Navigator )?.ScrollToVerticalOffset( ( double ) e.NewValue );
            ( d as Navigator )?.UpdateRanges();
        }

        private static void OnZoomMarginChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
        {
            ( d as Navigator )?.ApplyZoom();
        }

        private static void ExecuteZoomIn( Control control )
        {
            if( control == null ) return;

            var zoom = ( double ) control.GetValue( ZoomProperty );
            var max = ( double ) control.GetValue( MaxZoomProperty );
            control.SetCurrentValue( ZoomProperty, Math.Min( max, zoom * 1.1 ) );
        }

        private static bool CanExecuteZoomIn( Control control )
        {
            if( control == null )
                return false;

            var max = ( double ) control.GetValue( MaxZoomProperty );
            var zoom = ( double ) control.GetValue( ZoomProperty );

            return zoom < max;
        }

        private static void ExecuteZoomOut( Control control )
        {
            if( control == null ) return;

            var zoom = ( double ) control.GetValue( ZoomProperty );
            var min = ( double ) control.GetValue( MinZoomProperty );
            control.SetCurrentValue( ZoomProperty, Math.Max( min, zoom / 1.1 ) );
        }

        private static bool CanExecuteZoomOut( Control control )
        {
            if( control == null )
                return false;

            var min = ( double ) control.GetValue( MinZoomProperty );
            var zoom = ( double ) control.GetValue( ZoomProperty );

            return zoom > min;
        }

        private static void ExecuteZoomToContentSize( Navigator control )
        {
            control?.ZoomToContentSize();
        }

        private static void ExecuteResetZoom( Navigator control )
        {
            control.Zoom = 1;
        }

        private void ZoomToContentSize()
        {
            var zoom = GetContentSizeZoom( true, true );
            if( !double.IsNaN( zoom ) )
                Zoom = zoom;
        }

        private double GetContentSizeZoom( bool horizontal, bool vertical )
        {
            var margin = ZoomMargin;
            var content = Content as FrameworkElement;

            if( ActualWidth <= margin.Left + margin.Right || ActualHeight <= margin.Top + margin.Bottom )
                return double.NaN;

            if( content == null ||
                content.HorizontalAlignment == HorizontalAlignment.Stretch ||
                content.VerticalAlignment == VerticalAlignment.Stretch ||
                Math.Abs( content.ActualWidth ) < 1e-8 ||
                Math.Abs( content.ActualHeight ) < 1e-8 )
                return double.NaN;

            var horizontalZoom = ( ActualWidth - margin.Left - margin.Right ) / content.ActualWidth;
            var verticalZoom = ( ActualHeight - margin.Top - margin.Bottom ) / content.ActualHeight;

            if( horizontal && vertical )
                return Math.Min( horizontalZoom, verticalZoom );
            if( horizontal )
                return horizontalZoom;
            return verticalZoom;
        }

        private void ApplyZoom()
        {
            if( !( Content is FrameworkElement content ) )
                return;

            if( content.LayoutTransform is ScaleTransform scale && Math.Abs( scale.ScaleX - Zoom ) < 1e-8 )
                return;

            content.LayoutTransform = new ScaleTransform( Zoom, Zoom );
        }

        private void UpdateRanges()
        {
            if( !( Content is FrameworkElement content ) )
                return;

            var zoom = Zoom;

            var contentWidth = content.DesiredSize.Width;
            var contentHeight = content.DesiredSize.Height;

            double left;
            double right;

            double up;
            double down;

            if( contentWidth < ActualWidth )
            {
                left = ( ActualWidth - contentWidth ) / -2 / zoom;
                right = left + ActualWidth / zoom;
            }
            else
            {
                left = HorizontalOffset / zoom;
                right = ( HorizontalOffset + ActualWidth ) / zoom;
            }

            if( contentHeight < ActualHeight )
            {
                up = ( ActualHeight - contentHeight ) / -2 / zoom;
                down = up + ActualHeight / zoom;
            }
            else
            {
                up = VerticalOffset / zoom;
                down = ( VerticalOffset + ActualHeight ) / zoom;
            }

            HorizontalRange = new Range( left, right );
            VerticalRange = new Range( up, down );
        }

        protected override void OnScrollChanged( ScrollChangedEventArgs e )
        {
            base.OnScrollChanged( e );
            
            UpdateRanges();
            
            HorizontalPanning = HorizontalOffset;
            VerticalPanning = VerticalOffset;
        }

        private static void OnFitChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
        {
            ( ( Navigator ) d ).ScrollAndZoomIntoView( ( Rect ) e.NewValue );
        }

        public void ScrollAndZoomIntoView( Rect viewPort )
        {
            var fitZoom = GetContentSizeZoom( true, true );
            if( viewPort == Rect.Empty || double.IsNaN( fitZoom ) )
                return;

            var margin = ZoomMargin;

            var horizontalZoom = ( ActualWidth - margin.Left - margin.Right ) / viewPort.Width;
            var verticalZoom = ( ActualHeight - margin.Top - margin.Bottom ) / viewPort.Height;

            Zoom = Math.Min( horizontalZoom, verticalZoom );

            var aspect = Math.Max( viewPort.Width / ActualWidth, viewPort.Height / ActualHeight );

            //center of viewport - 0.5 * aspect corrected size of viewport * zoom == offset

            HorizontalPanning = ( viewPort.X + 0.5 * viewPort.Width - 0.5 * aspect * ActualWidth ) * Zoom;
            VerticalPanning = ( viewPort.Y + 0.5 * viewPort.Height - 0.5 * aspect * ActualHeight ) * Zoom;

            UpdateRanges();
        }

        private static void OnZoomChanged( DependencyObject obj, DependencyPropertyChangedEventArgs args )
        {
            ( obj as Navigator )?.ApplyZoom();
            ( obj as Navigator )?.UpdateRanges();
        }

        protected override void OnPreviewMouseWheel( MouseWheelEventArgs e )
        {
            if( !IsZoomEnabled || ZoomModifierKey != ModifierKeys.None && ( Keyboard.Modifiers & ZoomModifierKey ) == 0 )
            {
                base.OnPreviewMouseWheel( e );
                e.Handled = false;
            }

            var min = MinZoom;
            var max = MaxZoom;

            var oldZoom = Zoom;
            var newZoom = Math.Max( min, Math.Min( max, oldZoom * ( 1.0 + e.Delta / 960.0 ) ) );

            Zoom = newZoom;

            var currentHorizontalOffset = HorizontalOffset;
            var currentVerticalOffset = VerticalOffset;

            var deltaZoom = newZoom / oldZoom;
            var origin = e.GetPosition( this );

            var newVerticalOffset = ( origin.Y + currentVerticalOffset ) * deltaZoom - origin.Y;
            var newHorizontalOffset = ( origin.X + currentHorizontalOffset ) * deltaZoom - origin.X;

            if( double.IsNaN( newVerticalOffset ) || double.IsNaN( newHorizontalOffset ) )
            {
                newVerticalOffset = 0;
                newHorizontalOffset = 0;
            }

            VerticalPanning = newVerticalOffset;
            HorizontalPanning = newHorizontalOffset;

            UpdateRanges();

            e.Handled = true;
        }

        protected override void OnMouseLeave( MouseEventArgs e )
        {
            base.OnMouseLeave( e );
            if( e.Handled )
                return;

            SetCurrentValue( CursorProperty, Cursors.Arrow );
            _PanStart = null;
        }

        protected override void OnPreviewMouseUp( MouseButtonEventArgs e )
        {
            base.OnPreviewMouseUp( e );
            if( e.Handled )
                return;

            SetCurrentValue( CursorProperty, Cursors.Arrow );
            _PanStart = null;
        }

        protected override void OnPreviewMouseDown( MouseButtonEventArgs e )
        {
            base.OnPreviewMouseDown( e );
            if( e.Handled )
                return;

            if( !IsPanningEnabled )
                return;

            if( e.ChangedButton == PanButton && e.ButtonState == MouseButtonState.Pressed )
            {
                e.Handled = true;
                SetCurrentValue( CursorProperty, Cursors.Hand );
                _PanStart = e.GetPosition( this );
            }
            else
            {
                e.Handled = false;
            }
        }

        protected override void OnMouseMove( MouseEventArgs e )
        {
            base.OnMouseMove( e );
            if( e.Handled )
                return;

            if( !_PanStart.HasValue || !IsPanningEnabled )
                return;

            var nextPoint = e.GetPosition( this );
            var delta = _PanStart.Value - nextPoint;

            _PanStart = nextPoint;

            var currentHorizontalOffset = HorizontalOffset;
            var currentVerticalOffset = VerticalOffset;

            HorizontalPanning = currentHorizontalOffset + delta.X;
            VerticalPanning = currentVerticalOffset + delta.Y;
        }

        #endregion
    }
}