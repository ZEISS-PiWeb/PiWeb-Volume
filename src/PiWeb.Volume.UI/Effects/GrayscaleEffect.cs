#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
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

    public class GrayscaleEffect : ShaderEffect
    {
        #region members

        public static readonly DependencyProperty InputProperty = RegisterPixelShaderSamplerProperty( "Input", typeof( GrayscaleEffect ), 0 );
        public static readonly DependencyProperty RProperty = DependencyProperty.Register( "R", typeof( double ), typeof( GrayscaleEffect ), new UIPropertyMetadata( 0.299, PixelShaderConstantCallback( 0 ) ) );
        public static readonly DependencyProperty GProperty = DependencyProperty.Register( "G", typeof( double ), typeof( GrayscaleEffect ), new UIPropertyMetadata( 0.587, PixelShaderConstantCallback( 1 ) ) );
        public static readonly DependencyProperty BProperty = DependencyProperty.Register( "B", typeof( double ), typeof( GrayscaleEffect ), new UIPropertyMetadata( 0.144, PixelShaderConstantCallback( 2 ) ) );
        public static readonly DependencyProperty OpacityProperty = DependencyProperty.Register( "Opacity", typeof( double ), typeof( GrayscaleEffect ), new UIPropertyMetadata( 1D, PixelShaderConstantCallback( 3 ) ) );

        #endregion

        #region constructors

        public GrayscaleEffect()
        {
            try
            {
                PixelShader = new PixelShader { UriSource = new Uri( "pack://application:,,,/PiWeb.Volume.UI;component/Effects/GrayscaleEffect.ps" ) };
            }
            catch( Exception )
            {
                // ignored
            }

            UpdateShaderValue( InputProperty );
            UpdateShaderValue( RProperty );
            UpdateShaderValue( GProperty );
            UpdateShaderValue( BProperty );
            UpdateShaderValue( OpacityProperty );
        }

        #endregion

        #region properties

        public Brush Input
        {
            get => ( Brush ) GetValue( InputProperty );
            set => SetValue( InputProperty, value );
        }

        public double R
        {
            get => ( double ) GetValue( RProperty );
            set => SetValue( RProperty, value );
        }

        public double G
        {
            get => ( double ) GetValue( GProperty );
            set => SetValue( GProperty, value );
        }

        public double B
        {
            get => ( double ) GetValue( BProperty );
            set => SetValue( BProperty, value );
        }

        public double Opacity
        {
            get => ( double ) GetValue( OpacityProperty );
            set => SetValue( OpacityProperty, value );
        }

        #endregion
    }
}