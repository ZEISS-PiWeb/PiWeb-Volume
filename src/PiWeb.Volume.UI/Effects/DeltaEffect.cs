#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.Effects
{
	#region usings

	using System;
	using System.Windows;
	using System.Windows.Media;
	using System.Windows.Media.Effects;

	#endregion

	public class DeltaEffect : ShaderEffect
	{
		#region members

		public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty( nameof( Input ), typeof( DeltaEffect ), 0 );
		public static readonly DependencyProperty LeftProperty = RegisterPixelShaderSamplerProperty( nameof( Left ), typeof( DeltaEffect ), 1 );
		public static readonly DependencyProperty RightProperty = RegisterPixelShaderSamplerProperty( nameof( Right ), typeof( DeltaEffect ), 2 );
		public static readonly DependencyProperty MinColorProperty = DependencyProperty.Register( nameof( MinColor ), typeof( Color ), typeof( DeltaEffect ), new UIPropertyMetadata( Color.FromArgb( 255, 0, 128, 0 ), PixelShaderConstantCallback( 1 ) ) );
		public static readonly DependencyProperty MidColorProperty = DependencyProperty.Register( nameof( MidColor ), typeof( Color ), typeof( DeltaEffect ), new UIPropertyMetadata( Color.FromArgb( 255, 255, 255, 0 ), PixelShaderConstantCallback( 2 ) ) );
		public static readonly DependencyProperty MaxColorProperty = DependencyProperty.Register( nameof( MaxColor ), typeof( Color ), typeof( DeltaEffect ), new UIPropertyMetadata( Color.FromArgb( 255, 255, 0, 0 ), PixelShaderConstantCallback( 3 ) ) );
		public static readonly DependencyProperty MinProperty = DependencyProperty.Register( nameof( Min ), typeof( double ), typeof( DeltaEffect ), new UIPropertyMetadata( 0.02D, PixelShaderConstantCallback( 4 ) ) );
		public static readonly DependencyProperty MaxProperty = DependencyProperty.Register( nameof( Max ), typeof( double ), typeof( DeltaEffect ), new UIPropertyMetadata( 0.05D, PixelShaderConstantCallback( 5 ) ) );

		#endregion

		#region constructors

		public DeltaEffect()
		{
			try
			{
				PixelShader = new PixelShader { UriSource = new Uri( "pack://application:,,,/PiWeb.Volume.UI;component/Effects/DeltaEffect.ps" ) };
			}
			catch( Exception )
			{
				// ignored
			}

			UpdateShaderValue( InputProperty );
			UpdateShaderValue( LeftProperty );
			UpdateShaderValue( RightProperty );
			UpdateShaderValue( MinColorProperty );
			UpdateShaderValue( MidColorProperty );
			UpdateShaderValue( MaxColorProperty );
			UpdateShaderValue( MinProperty );
			UpdateShaderValue( MaxProperty );
		}

		#endregion

		#region properties

		public Brush Input
		{
			get => ( Brush ) GetValue( InputProperty );
			set => SetValue( InputProperty, value );
		}

		public Brush Left
		{
			get => ( Brush ) GetValue( LeftProperty );
			set => SetValue( LeftProperty, value );
		}

		public Brush Right
		{
			get => ( Brush ) GetValue( RightProperty );
			set => SetValue( RightProperty, value );
		}

		/// <summary>The minimum color.</summary>
		public Color MinColor
		{
			get => ( Color ) GetValue( MinColorProperty );
			set => SetValue( MinColorProperty, value );
		}

		/// <summary>The minimum color.</summary>
		public Color MidColor
		{
			get => ( Color ) GetValue( MidColorProperty );
			set => SetValue( MidColorProperty, value );
		}

		/// <summary>The minimum color.</summary>
		public Color MaxColor
		{
			get => ( Color ) GetValue( MaxColorProperty );
			set => SetValue( MaxColorProperty, value );
		}

		public double Min
		{
			get => ( double ) GetValue( MinProperty );
			set => SetValue( MinProperty, value );
		}

		public double Max
		{
			get => ( double ) GetValue( MaxProperty );
			set => SetValue( MaxProperty, value );
		}

		#endregion
	}
}