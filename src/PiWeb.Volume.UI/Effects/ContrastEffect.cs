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

	public class ContrastEffect : ShaderEffect
	{
		#region members

		public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty( "Input", typeof( ContrastEffect ), 0 );
		public static readonly DependencyProperty LowProperty = DependencyProperty.Register( "Low", typeof( double ), typeof( ContrastEffect ), new UIPropertyMetadata( 0.0, PixelShaderConstantCallback( 0 ) ) );
		public static readonly DependencyProperty HighProperty = DependencyProperty.Register( "High", typeof( double ), typeof( ContrastEffect ), new UIPropertyMetadata( 1.0, PixelShaderConstantCallback( 1 ) ) );

		#endregion

		#region constructors

		public ContrastEffect()
		{
			try
			{
				PixelShader = new PixelShader { UriSource = new Uri( "pack://application:,,,/PiWeb.Volume.UI;component/Effects/ContrastEffect.ps" ) };
			}
			catch( Exception )
			{
				// ignored
			}

			UpdateShaderValue( InputProperty );
			UpdateShaderValue( LowProperty );
			UpdateShaderValue( HighProperty );
		}

		#endregion

		#region properties

		public Brush Input
		{
			get => ( ( Brush ) ( GetValue( InputProperty ) ) );
			set => SetValue( InputProperty, value );
		}

		public double Low
		{
			get => ( ( double ) ( GetValue( LowProperty ) ) );
			set => SetValue( LowProperty, value );
		}

		public double High
		{
			get => ( ( double ) ( GetValue( HighProperty ) ) );
			set => SetValue( HighProperty, value );
		}

		#endregion
	}
}