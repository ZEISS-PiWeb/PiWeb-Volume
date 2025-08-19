#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Compare.ViewModel;

#region usings

using System;
using System.Buffers;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using Zeiss.PiWeb.Volume.UI.Components;
using Zeiss.PiWeb.Volume.UI.Interfaces;
using Zeiss.PiWeb.Volume.UI.Model;
using Zeiss.PiWeb.Volume.UI.ViewModel;

#endregion

public class MainViewModel : ViewModelBase
{
	#region members

	private double _HorizontalPanning;
	private double _VerticalPanning;
	private double _Zoom = 1;

	private IDisposable _LayerChangedSubscription;

	private SpectrumData[] _LeftSpectrum;
	private SpectrumData[] _RightSpectrum;
	private SpectrumData[] _DeltaSpectrum;

	private int _DeltaMin = 1;
	private int _DeltaMax = 10;

	private IDisposable _CreateSpectrumSubscription;

	private double _ContrastLow;
	private double _ContrastHigh = 256;

	private BitmapScalingMode _BitmapScalingMode = BitmapScalingMode.Fant;

	#endregion

	#region constructors

	public MainViewModel(
		IFileService fileService,
		IMessageService messageService,
		IViewService viewService )
	{
		LeftVolume = new VolumeManagementViewModel( fileService, messageService, viewService );
		RightVolume = new VolumeManagementViewModel( fileService, messageService, viewService );

		LeftVolume.VolumeChanged += OnVolumeChanged;
		RightVolume.VolumeChanged += OnVolumeChanged;
	}

	#endregion

	#region properties

	public BitmapScalingMode BitmapScalingMode
	{
		get => _BitmapScalingMode;
		set => Set( ref _BitmapScalingMode, value );
	}

	public SpectrumData[] LeftSpectrum
	{
		get => _LeftSpectrum;
		set => Set( ref _LeftSpectrum, value );
	}

	public SpectrumData[] RightSpectrum
	{
		get => _RightSpectrum;
		set => Set( ref _RightSpectrum, value );
	}

	public SpectrumData[] DeltaSpectrum
	{
		get => _DeltaSpectrum;
		set => Set( ref _DeltaSpectrum, value );
	}

	public VolumeManagementViewModel LeftVolume { get; }

	public VolumeManagementViewModel RightVolume { get; }

	public double HorizontalPanning
	{
		get => _HorizontalPanning;
		set => Set( ref _HorizontalPanning, value );
	}

	public double VerticalPanning
	{
		get => _VerticalPanning;
		set => Set( ref _VerticalPanning, value );
	}

	public double Zoom
	{
		get => _Zoom;
		set => Set( ref _Zoom, value );
	}

	public int DeltaMin
	{
		get => _DeltaMin;
		set => Set( ref _DeltaMin, value );
	}

	public int DeltaMax
	{
		get => _DeltaMax;
		set => Set( ref _DeltaMax, value );
	}

	public double ContrastLow
	{
		get => _ContrastLow;
		set => Set( ref _ContrastLow, value );
	}

	public double ContrastHigh
	{
		get => _ContrastHigh;
		set => Set( ref _ContrastHigh, value );
	}

	#endregion

	#region methods

	private void OnVolumeChanged( object sender, EventArgs e )
	{
		if( sender == LeftVolume && LeftVolume.VolumeViewModel != null && RightVolume.VolumeViewModel != null )
			LeftVolume.VolumeViewModel.SelectedLayerIndex = RightVolume.VolumeViewModel.SelectedLayerIndex;

		if( sender == RightVolume && RightVolume.VolumeViewModel != null && LeftVolume.VolumeViewModel != null )
			RightVolume.VolumeViewModel.SelectedLayerIndex = LeftVolume.VolumeViewModel.SelectedLayerIndex;

		_LayerChangedSubscription?.Dispose();

		_LayerChangedSubscription = new[] { LeftVolume.VolumeViewModel, RightVolume.VolumeViewModel }
			.Where( v => v != null )
			.Select( v => Observable.FromEventPattern<EventArgs>( v, nameof(VolumeViewModel.LayerChanged) ) )
			.Merge()
			.Sample( TimeSpan.FromMilliseconds( 50 ) )
			.Subscribe( _ => OnLayerChanged() );
	}

	private void OnLayerChanged()
	{
		var left = LeftVolume.VolumeViewModel?.SelectedLayer;
		var right = RightVolume.VolumeViewModel?.SelectedLayer;

		LeftSpectrum = null;
		RightSpectrum = null;
		DeltaSpectrum = null;

		_CreateSpectrumSubscription?.Dispose();
		_CreateSpectrumSubscription = Observable.FromAsync( async ct => await CreateSpectrum( left, right, ct ) ).Subscribe( data =>
		{
			if( data.Left is null || data.Right is null || data.Delta is null )
				return;

			if( data.Left != null )
				LeftSpectrum = new[] { data.Left };
			RightSpectrum = new[] { data.Right };
			DeltaSpectrum = data.Delta;
		} );
	}

	private async Task<(SpectrumData Left, SpectrumData[] Delta, SpectrumData Right)> CreateSpectrum( Layer? left, Layer? right, CancellationToken ct )
	{
		return await Task.Run( () =>
		{
			ct.ThrowIfCancellationRequested();


			var leftData = new SpectrumData( "Voxels", Colors.LightGray );
			var rightData = new SpectrumData( "Voxels", Colors.LightGray );

			if( left.HasValue )
			{
				foreach( var value in left.Value.Data )
					leftData.Values[ value ]++;
			}

			ct.ThrowIfCancellationRequested();

			if( right.HasValue )
			{
				foreach( var value in right.Value.Data )
					rightData.Values[ value ]++;
			}

			ct.ThrowIfCancellationRequested();

			var deltaMax = new SpectrumData( "Maximum deviation", Colors.IndianRed );
			var deltaAvg = new SpectrumData( "Average deviation", Colors.DodgerBlue );

			if( left.HasValue && right.HasValue && left.Value.Data.Length == right.Value.Data.Length )
			{
				const int spectrumLength = 256;
				var counts = ArrayPool<long>.Shared.Rent( spectrumLength );

				for( var i = 0; i < left.Value.Data.Length; i++ )
				{
					var leftValue = left.Value.Data[ i ];
					var rightValue = right.Value.Data[ i ];
					var delta = Math.Abs( leftValue - rightValue );

					counts[ leftValue ]++;
					deltaAvg.Values[ leftValue ] += delta;
					deltaMax.Values[ leftValue ] = Math.Max( deltaMax.Values[ leftValue ], delta );
				}

				for( var i = 0; i < spectrumLength; i++ )
				{
					var count = counts[ i ];
					if( count == 0 )
						continue;

					deltaAvg.Values[ i ] /= count;
				}

				ArrayPool<long>.Shared.Return( counts );
			}

			return ( leftData, new[] { deltaAvg, deltaMax }, rightData );
		}, ct );
	}

	#endregion
}