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

	using GalaSoft.MvvmLight;

	#endregion

	public class LoadOptionsViewModel : ViewModelBase
	{
		#region members

		private static bool _Extrapolate;
		private static double _Minimum = 0;
		private static double _Maximum = 255;

		private bool _Streamed;

		#endregion

		#region properties


		/// <summary>
		/// Minimum value for the extrapolation bounds.
		/// </summary>
		public double MinimumValue { get; init; } = 0;

		/// <summary>
		/// Maximum value for the extrapolation bounds.
		/// </summary>
		public double MaximumValue { get; init; } = 255;

		/// <summary>
		/// Gets or sets a value, indicating whether the volume should not be loaded into memory but instead
		/// be read on demand from the file stream.
		/// </summary>
		public bool Streamed
		{
			get => _Streamed;
			set => Set( ref _Streamed, value );
		}

		/// <summary>
		/// Gets or sets a value indicating, whether the values in the file should be extrapolated.
		/// </summary>
		public bool Extrapolate

		{
			get => _Extrapolate;
			set => Set( ref _Extrapolate, value );
		}

		/// <summary>
		/// The lower bound to extrapolate the values with.
		/// </summary>
		public double Minimum
		{
			get => _Minimum;
			set => Set( ref _Minimum, value );
		}

		/// <summary>
		/// The upper bound to extrapolate the values with.
		/// </summary>
		public double Maximum
		{
			get => _Maximum;
			set => Set( ref _Maximum, value );
		}

		#endregion
	}
}