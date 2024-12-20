﻿#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.View
{
	#region usings

	using System.Windows;

	#endregion

	/// <summary>
	/// Interaction logic for CodecView.xaml
	/// </summary>
	public partial class CodecView : Window
	{
		#region constructors

		public CodecView()
		{
			InitializeComponent();
		}

		#endregion

		#region methods

		private void OkButton_Clicked( object sender, RoutedEventArgs e )
		{
			DialogResult = true;
			Close();
		}

		#endregion
	}
}