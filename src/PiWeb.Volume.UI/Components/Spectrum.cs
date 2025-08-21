#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.Components;

#region usings

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

#endregion

public class Spectrum : Control
{
	#region members

	public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
		nameof(Data), typeof( IReadOnlyCollection<SpectrumData> ), typeof( Spectrum ), new FrameworkPropertyMetadata( null, FrameworkPropertyMetadataOptions.AffectsRender ) );

	public static readonly DependencyProperty LogarithmicScaleProperty = DependencyProperty.Register(
		nameof(LogarithmicScale), typeof( bool ), typeof( Spectrum ), new FrameworkPropertyMetadata( false, FrameworkPropertyMetadataOptions.AffectsRender ) );

	private int? _HighlightedIndex;

	#endregion

	#region constructors

	public Spectrum()
	{
		ToolTipService.SetBetweenShowDelay( this, 0 );
		ToolTipService.SetInitialShowDelay( this, 0 );
		ToolTipService.SetShowDuration( this, 10000 );
	}

	#endregion

	#region properties

	public bool LogarithmicScale
	{
		get { return ( bool ) GetValue( LogarithmicScaleProperty ); }
		set { SetValue( LogarithmicScaleProperty, value ); }
	}

	public IReadOnlyCollection<SpectrumData> Data
	{
		get { return ( IReadOnlyCollection<SpectrumData> ) GetValue( DataProperty ); }
		set { SetValue( DataProperty, value ); }
	}

	#endregion

	#region methods

	protected override void OnMouseLeave( MouseEventArgs e )
	{
		base.OnMouseLeave( e );
		_HighlightedIndex = null;
		ToolTip = null;

		InvalidateVisual();
	}

	protected override void OnIsMouseDirectlyOverChanged( DependencyPropertyChangedEventArgs e )
	{
		base.OnIsMouseDirectlyOverChanged( e );
		if( !IsMouseOver )
		{
			_HighlightedIndex = null;
			ToolTip = null;
		}


		InvalidateVisual();
	}

	protected override void OnMouseMove( MouseEventArgs e )
	{
		base.OnMouseMove( e );

		if( !IsMouseOver || Data is null || Data.Count == 0 )
		{
			_HighlightedIndex = null;
			return;
		}

		var position = e.GetPosition( this );

		_HighlightedIndex = ( int ) ( position.X * 255.0 / ActualWidth );
		InvalidateVisual();
	}

	protected override void OnRender( DrawingContext drawingContext )
	{
		drawingContext.DrawRectangle( Background, null, new Rect( 0, 0, ActualWidth, ActualHeight ) );

		var width = ActualWidth / 256;
		var pen = new Pen( BorderBrush, 1 );

		for( var i = 1; i < 16; i++ )
			drawingContext.DrawLine( pen, new Point( width * i * 16 + 0.5, 0 ), new Point( width * i * 16 + 0.5, ActualHeight ) );

		if( Data is null || Data.Count == 0 )
			return;

		var max = Data.SelectMany( d => d.Values ).Max();
		if( LogarithmicScale )
			max = max > 0 ? Math.Log10( max ) : 0;

		for( var i = 0; i < 256; i++ )
		{
			foreach( var data in Data.OrderByDescending( d => d.Values[ i ] ) )
			{
				var count = data.Values[ i ];
				if( LogarithmicScale )
					count = count > 0 ? Math.Log10( count ) : 0;

				if( count < 1e-6 )
					continue;

				var height = count / max * ( ActualHeight - 24 - 1 ) + 1;
				var brush = new SolidColorBrush( i == _HighlightedIndex
					? new Color
					{
						A = data.Color.A,
						R = ( byte ) ( data.Color.R * 1.2 ),
						G = ( byte ) ( data.Color.G * 1.2 ),
						B = ( byte ) ( data.Color.B * 1.2 )
					}
					: data.Color );

				drawingContext.DrawRectangle( brush, null, new Rect( i * width, ActualHeight - height, width, height ) );
			}
		}

		if( _HighlightedIndex.HasValue )
		{
			var text = $"Index: {_HighlightedIndex.Value}; {string.Join( "; ", Data.Select( d => $"{d.Name}: {d.Values[ _HighlightedIndex.Value ]:N2}" ) )}";
			var ftext = new FormattedText( text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface( "Segoe UI" ), 12, Foreground, 1.0 )
			{
				TextAlignment = TextAlignment.Center
			};

			drawingContext.DrawText( ftext, new Point( ActualWidth * 0.5, 4 ) );
		}

		drawingContext.DrawLine( pen, new Point( 0, ActualHeight ), new Point( ActualWidth, ActualHeight ) );
	}

	#endregion
}

public class SpectrumData
{
	#region constructors

	public SpectrumData( string name, Color color )
	{
		Name = name;
		Color = color;
	}

	#endregion

	#region properties

	public double[] Values { get; } = new double[256];

	public string Name { get; }

	public Color Color { get; }

	#endregion
}