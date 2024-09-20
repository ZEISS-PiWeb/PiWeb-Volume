namespace Zeiss.PiWeb.Volume.UI.Components;

#region usings

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

#endregion

public class SliceCrosshair : Control
{
	#region members

	public static readonly DependencyProperty LastMousePositionProperty = DependencyProperty.Register(
		nameof( LastMousePosition ), typeof( Point? ), typeof( SliceCrosshair ), new PropertyMetadata( default( Point? ) ) );

	#endregion

	#region properties

	public Point? LastMousePosition
	{
		get => (Point?)GetValue( LastMousePositionProperty );
		set => SetValue( LastMousePositionProperty, value );
	}

	#endregion

	#region methods

	protected override void OnMouseMove( MouseEventArgs e )
	{
		base.OnMouseMove( e );
		var position = e.GetPosition( this );

		SetCurrentValue( LastMousePositionProperty, new Point( Math.Round( position.X ), Math.Round( ActualHeight - position.Y ) ) );
		InvalidateVisual();
	}

	protected override void OnMouseLeave( MouseEventArgs e )
	{
		base.OnMouseLeave( e );
		SetCurrentValue( LastMousePositionProperty, null );
		InvalidateVisual();
	}

	protected override void OnRender( DrawingContext drawingContext )
	{
		base.OnRender( drawingContext );

		drawingContext.DrawRectangle( Brushes.Transparent, null, new Rect( 0, 0, ActualWidth, ActualHeight ) );

		if( LastMousePosition is null )
			return;

		var pen = new Pen( Foreground, 1 );
		var position = LastMousePosition.Value with { Y = ActualHeight - LastMousePosition.Value.Y };

		drawingContext.DrawLine( pen, position with { X = 0 }, position with { X = ActualWidth } );
		drawingContext.DrawLine( pen, position with { Y = 0 }, position with { Y = ActualHeight } );
	}

	#endregion
}