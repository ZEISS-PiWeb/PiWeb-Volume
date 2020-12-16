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
    using System.Globalization;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using Zeiss.IMT.PiWeb.Volume.UI.Model;

    #endregion

    public class Ruler : Control
    {
        #region members

        public static DependencyProperty ValueRangeProperty =
            DependencyProperty.Register( "ValueRange", typeof( DoubleRange? ), typeof( Ruler ), new FrameworkPropertyMetadata( null, FrameworkPropertyMetadataOptions.AffectsRender )
            );

        public static DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register( "StrokeThickness", typeof( double ), typeof( Ruler ), new FrameworkPropertyMetadata( 1.0, FrameworkPropertyMetadataOptions.AffectsRender )
            );

        public static DependencyProperty StrokeProperty =
            DependencyProperty.Register( "Stroke", typeof( Brush ), typeof( Ruler ), new FrameworkPropertyMetadata( Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender ) );

        public static DependencyProperty InvertProperty =
            DependencyProperty.Register( "Invert", typeof( bool ), typeof( Ruler ), new FrameworkPropertyMetadata( false, FrameworkPropertyMetadataOptions.AffectsRender )
            );

        public static DependencyProperty HighlightBrushProperty =
            DependencyProperty.Register( "HighlightBrush", typeof( Brush ), typeof( Ruler ), new FrameworkPropertyMetadata( Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender )
            );


        public static DependencyProperty HighlightedRangeProperty =
            DependencyProperty.Register( "HighlightedRange", typeof( DoubleRange? ), typeof( Ruler ), new FrameworkPropertyMetadata( null, FrameworkPropertyMetadataOptions.AffectsRender )
            );

        public static DependencyProperty OrientationProperty =
            DependencyProperty.Register( "Orientation", typeof( Orientation ), typeof( Ruler ), new FrameworkPropertyMetadata( Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsRender )
            );

        #endregion

        #region properties

        public DoubleRange? ValueRange
        {
            get => ( DoubleRange? ) GetValue( ValueRangeProperty );
            set => SetCurrentValue( ValueRangeProperty, value );
        }


        public double StrokeThickness
        {
            get => ( double ) GetValue( StrokeThicknessProperty );
            set => SetCurrentValue( StrokeThicknessProperty, value );
        }


        public Brush Stroke
        {
            get => ( Brush ) GetValue( StrokeProperty );
            set => SetCurrentValue( StrokeProperty, value );
        }


        public bool Invert
        {
            get => ( bool ) GetValue( InvertProperty );
            set => SetCurrentValue( InvertProperty, value );
        }


        public Brush HighlightBrush
        {
            get => ( Brush ) GetValue( HighlightBrushProperty );
            set => SetCurrentValue( HighlightBrushProperty, value );
        }

        public DoubleRange? HighlightedRange
        {
            get => ( DoubleRange? ) GetValue( HighlightedRangeProperty );
            set => SetCurrentValue( HighlightedRangeProperty, value );
        }

        public Orientation Orientation
        {
            get => ( Orientation ) GetValue( OrientationProperty );
            set => SetCurrentValue( OrientationProperty, value );
        }

        #endregion

        #region methods

        private static double CalculateStep( double valueRangeSize, double drawingRangeSize )
        {
            var step = 1.0;
            var pxperstep = drawingRangeSize / valueRangeSize * step;

            while( pxperstep < 5.0 || pxperstep > 10 )
            {
                if( pxperstep < 5.0 ) step *= 2;
                else if( pxperstep > 10.0 ) step /= 2;

                pxperstep = drawingRangeSize / valueRangeSize * step;
            }

            return step;
        }

        protected override void OnRender( DrawingContext ctx )
        {
            if( ActualWidth <= 0 || ActualHeight <= 0 )
                return;

            if( ValueRange is null || ValueRange.Value.Size <= 0 )
                return;

            var pen = new Pen( Stroke, StrokeThickness );

            ctx.PushGuidelineSet( new GuidelineSet( new[] { pen.Thickness / 2 }, new[] { pen.Thickness / 2 } ) );

            var valueRange = new DoubleRange( ValueRange.Value.Start, ValueRange.Value.Stop );

            if( Invert )
	            valueRange = valueRange.Invert();
            
            var valueRangeBase = new DoubleRange( valueRange.Start, valueRange.Stop );

            var w = ActualWidth;
            var h = ActualHeight;

            if( Orientation == Orientation.Vertical )
            {
                var rotation = new RotateTransform( 90 );
                var translation = new TranslateTransform( 0.0, -ActualWidth );

                ctx.PushTransform( rotation );
                ctx.PushTransform( translation );

                w = ActualHeight;
                h = ActualWidth;
            }

            ctx.DrawRectangle( Background, null, new Rect( 0, 0, w, h ) );
            ctx.DrawLine( pen, new Point( 0, h ), new Point( w, h ) );

            var step = CalculateStep( valueRange.Size, w );

            var drawingRange = new DoubleRange( 0, w );

            if( HighlightedRange.HasValue && HighlightedRange.Value.Size > 0 )
            {
	            var highlightedRange = HighlightedRange.Value;
                var highlightedValueRange = new DoubleRange( DoubleRange.Clip( valueRangeBase, highlightedRange.Start ), DoubleRange.Clip( valueRangeBase, highlightedRange.Stop ) );

                var highlightedDrawingRange = new DoubleRange(
                    Math.Floor( DoubleRange.Transform( highlightedValueRange.Start, valueRangeBase, drawingRange ) ), Math.Floor( DoubleRange.Transform( highlightedValueRange.Stop, valueRangeBase, drawingRange ) ) );

                if( Invert )
                    highlightedDrawingRange.Invert();

                ctx.DrawRectangle( HighlightBrush, null, new Rect( highlightedDrawingRange.Lower, 0, highlightedDrawingRange.Size, h ) );
            }

            var sign = valueRange.Start > valueRange.Stop ? -1.0 : 1.0;

            for( var i = 0; i < ( int ) ( valueRange.Size / step ); i++ )
            {
                var firstValue = Math.Floor( valueRange.Start / step ) * step;

                var value = firstValue + sign * ( step + i * step );

                var x = Math.Floor( DoubleRange.Transform( value, valueRange, drawingRange ) );

                if( Math.Abs( value % ( 10 * step ) ) < 1e-9 )
                {
                    var text = value.ToString( "0.###", CultureInfo.CurrentCulture );
                    var formattedText = new FormattedText( text, CultureInfo.CurrentCulture, FlowDirection, FontFamily.GetTypefaces().First(), FontSize, Foreground );

                    if( x + 3 + formattedText.Width < w )
                        ctx.DrawText( formattedText, new Point( x + 3, 0 ) );

                    ctx.DrawLine( pen, new Point( x, 0 ), new Point( x, h ) );
                }
                else
                {
                    ctx.DrawLine( pen, new Point( x, h * 0.75 ), new Point( x, h ) );
                }
            }

            if( Orientation == Orientation.Vertical )
            {
                ctx.Pop();
                ctx.Pop();
            }

            ctx.Pop();
        }

        #endregion
    }
}