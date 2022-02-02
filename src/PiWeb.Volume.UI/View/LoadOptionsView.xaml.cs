#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.View
{
	#region usings

	using System.Windows;

	#endregion

	public partial class LoadOptionsView
	{
		#region constructors

		public LoadOptionsView()
		{
			InitializeComponent();
		}

		#endregion

		#region methods

		private void OkButton_Click( object sender, RoutedEventArgs e )
		{
			DialogResult = true;
			Close();
		}

		#endregion
	}
}