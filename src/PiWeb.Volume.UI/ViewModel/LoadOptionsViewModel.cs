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

	using GalaSoft.MvvmLight;

	#endregion

	public class LoadOptionsViewModel : ViewModelBase
	{
		#region members

		private static bool _Extrapolate;
		private static byte _Minimum = 0;
		private static byte _Maximum = 255;

		private bool _Streamed;

		#endregion

		#region properties

		public bool Streamed
		{
			get => _Streamed;
			set => Set( ref _Streamed, value );
		}

		public bool Extrapolate

		{
			get => _Extrapolate;
			set => Set( ref _Extrapolate, value );
		}
		
		public byte Minimum
		{
			get => _Minimum;
			set => Set( ref _Minimum, value );
		}

		public byte Maximum
		{
			get => _Maximum;
			set => Set( ref _Maximum, value );
		}

		#endregion
	}
}