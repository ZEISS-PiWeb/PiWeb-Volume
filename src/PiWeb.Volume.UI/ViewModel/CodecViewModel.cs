#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.UI.ViewModel
{
	#region usings

	using System.Collections.Generic;
	using GalaSoft.MvvmLight;
	using GalaSoft.MvvmLight.Messaging;

	#endregion

	public class CodecViewModel : ViewModelBase
	{
		#region members

		private static string _Encoder = "zeiss.block"; //"nvenc";

		private static Dictionary<string, string> _EncoderOptions = new Dictionary<string, string>
		{
			{ "quality", "90" } //{ "cq", "31" }
		};

		private static int _Bitrate = -1;

		private static string _PixelFormat = "gray8"; //"yuv420p";

		private bool _MultiDirection;

		#endregion

		#region properties

		public IMessenger Messenger => MessengerInstance;

		public string Encoder
		{
			get => _Encoder;
			set => Set( ref _Encoder, value );
		}

		public Dictionary<string, string> EncoderOptions
		{
			get => _EncoderOptions;
			set => Set( ref _EncoderOptions, value );
		}

		public int Bitrate
		{
			get => _Bitrate;
			set => Set( ref _Bitrate, value );
		}

		public bool MultiDirection
		{
			get => _MultiDirection;
			set => Set( ref _MultiDirection, value );
		}

		public string PixelFormat
		{
			get => _PixelFormat;
			set => Set( ref _PixelFormat, value );
		}

		public VolumeCompressionOptions GetOptions()
		{
			return new VolumeCompressionOptions( Encoder, PixelFormat, EncoderOptions, Bitrate );
		}

		#endregion
	}
}