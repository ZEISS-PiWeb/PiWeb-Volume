#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.Compare.ViewModel
{
	#region usings

	using System;
	using System.Buffers;
	using System.Reactive.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows.Media;
	using GalaSoft.MvvmLight;
	using Zeiss.IMT.PiWeb.Volume.UI.Components;
	using Zeiss.IMT.PiWeb.Volume.UI.Interfaces;
	using Zeiss.IMT.PiWeb.Volume.UI.Model;
	using Zeiss.IMT.PiWeb.Volume.UI.ViewModel;

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
			_LayerChangedSubscription?.Dispose();
			if( LeftVolume.VolumeViewModel is null || RightVolume.VolumeViewModel is null )
				return;

			var leftObservable = Observable.FromEventPattern<EventArgs>( LeftVolume.VolumeViewModel, nameof(VolumeViewModel.LayerChanged) );
			var rightObservable = Observable.FromEventPattern<EventArgs>( RightVolume.VolumeViewModel, nameof(VolumeViewModel.LayerChanged) );

			_LayerChangedSubscription = leftObservable
				.Merge( rightObservable )
				.Sample( TimeSpan.FromMilliseconds( 50 ) )
				.Subscribe( _ => OnLayerChanged() );
		}

		private void OnLayerChanged()
		{
			var left = LeftVolume.VolumeViewModel.SelectedLayer;
			var right = RightVolume.VolumeViewModel.SelectedLayer;

			LeftSpectrum = null;
			RightSpectrum = null;
			DeltaSpectrum = null;

			_CreateSpectrumSubscription?.Dispose();
			_CreateSpectrumSubscription = Observable.FromAsync( async ct => await CreateSpectrum( left, right, ct ) ).Subscribe( data =>
			{
				if( data.Left is null || data.Right is null || data.Delta is null )
					return;

				LeftSpectrum = new[] { data.Left };
				RightSpectrum = new[] { data.Right };
				DeltaSpectrum = data.Delta;
			} );
		}

		private async Task<(SpectrumData Left, SpectrumData[] Delta, SpectrumData Right)> CreateSpectrum( Layer left, Layer right, CancellationToken ct )
		{
			if( left.Data.Length != right.Data.Length )
				return await Task.FromResult<(SpectrumData, SpectrumData[], SpectrumData)>( ( null, null, null ) );

			return await Task.Run( () =>
			{
				ct.ThrowIfCancellationRequested();

				var leftData = new SpectrumData( "Voxels", Colors.LightGray );
				foreach( var value in left.Data )
					leftData.Values[ value ]++;

				ct.ThrowIfCancellationRequested();

				var rightData = new SpectrumData( "Voxels", Colors.LightGray );
				foreach( var value in right.Data )
					rightData.Values[ value ]++;

				ct.ThrowIfCancellationRequested();

				var deltaMax = new SpectrumData( "Maximum deviation", Colors.IndianRed );
				var deltaAvg = new SpectrumData( "Average deviation", Colors.DodgerBlue );

				const int spectrumLength = 256;
				var counts = ArrayPool<long>.Shared.Rent( spectrumLength );

				for( var i = 0; i < left.Data.Length; i++ )
				{
					var leftValue = left.Data[ i ];
					var rightValue = right.Data[ i ];
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

				return ( leftData, new[] { deltaAvg, deltaMax }, rightData );
			}, ct );
		}

		#endregion
	}
}