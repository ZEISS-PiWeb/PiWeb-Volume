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

    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    #endregion

    public class VolumeDirectionButton : Button
    {
        #region members

        public static readonly DependencyProperty OuterPathProperty = DependencyProperty.Register(
            "OuterPath", typeof( Geometry ), typeof( VolumeDirectionButton ), new PropertyMetadata( default( Geometry ) ) );

        public static readonly DependencyProperty InnerPathProperty = DependencyProperty.Register(
            "InnerPath", typeof( Geometry ), typeof( VolumeDirectionButton ), new PropertyMetadata( default( Geometry ) ) );

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            "IsSelected", typeof( bool ), typeof( VolumeDirectionButton ), new PropertyMetadata( default( bool ) ) );

        #endregion

        #region properties

        public Geometry OuterPath
        {
            get => ( Geometry ) GetValue( OuterPathProperty );
            set => SetValue( OuterPathProperty, value );
        }

        public Geometry InnerPath
        {
            get => ( Geometry ) GetValue( InnerPathProperty );
            set => SetValue( InnerPathProperty, value );
        }

        public bool IsSelected
        {
            get => ( bool ) GetValue( IsSelectedProperty );
            set => SetValue( IsSelectedProperty, value );
        }

        #endregion
    }
}