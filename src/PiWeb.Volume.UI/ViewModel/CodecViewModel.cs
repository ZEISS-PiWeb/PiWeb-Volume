#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.ViewModel
{
	#region usings

	using System;
	using System.Collections.Generic;
	using GalaSoft.MvvmLight;
	using GalaSoft.MvvmLight.Messaging;

	#endregion

	public class CodecViewModel : ViewModelBase
	{
		#region members

		private int _Quality = 75;

		#endregion

		#region properties

		public IMessenger Messenger => MessengerInstance;

		public int Quality
		{
			get => _Quality;
			set => Set( ref _Quality, value );
		}

		#endregion

		#region methods

		public VolumeCompressionOptions GetOptions()
		{
			return new VolumeCompressionOptions( "zeiss.block", "gray8", new Dictionary<string, string>
			{
				{ "quality", Math.Max( 5, Math.Min( 100, Quality ) ).ToString() }
			}, 0 );
		}

		#endregion
	}
}