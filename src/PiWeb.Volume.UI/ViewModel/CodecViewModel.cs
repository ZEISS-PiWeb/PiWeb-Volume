#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.ViewModel;

#region usings

using System;
using System.Collections.Generic;
using System.Globalization;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Zeiss.PiWeb.Volume.Block;

#endregion

public class CodecViewModel : ViewModelBase
{
	#region members

	private static int _Quality = 75;
	private static double _QuantizationBase = 12;
	private static double _QuantizationGain = 1;

	#endregion

	#region properties

	public IMessenger Messenger => MessengerInstance;

	public int Quality
	{
		get => _Quality;
		set => Set( ref _Quality, value );
	}

	public double QuantizationBase
	{
		get => _QuantizationBase;
		set => Set( ref _QuantizationBase, value );
	}

	public double QuantizationGain
	{
		get => _QuantizationGain;
		set => Set( ref _QuantizationGain, value );
	}

	#endregion

	#region methods

	public VolumeCompressionOptions GetOptions()
	{
		return new VolumeCompressionOptions( BlockVolume.EncoderID, BlockVolume.PixelFormat, new Dictionary<string, string>
		{
			{ BlockVolume.QualityName, Math.Max( 5, Math.Min( 100, Quality ) ).ToString( CultureInfo.InvariantCulture ) },
			{ BlockVolume.QuantizationBaseName, Math.Max( 4, Math.Min( 24, QuantizationBase ) ).ToString( CultureInfo.InvariantCulture ) },
			{ BlockVolume.QuantizationGainName, Math.Max( 0.25, Math.Min( 4, QuantizationGain ) ).ToString( CultureInfo.InvariantCulture ) }
		}, 0 );
	}

	#endregion
}