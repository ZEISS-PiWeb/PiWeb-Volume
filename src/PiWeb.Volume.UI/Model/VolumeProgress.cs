#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.UI.Model
{
	#region usings

	using System;

	#endregion

	public class VolumeProgress : IProgress<VolumeSliceDefinition>
	{
		#region members

		private readonly Volume _Volume;

		#endregion

		#region constructors

		public VolumeProgress( Volume volume )
		{
			_Volume = volume;
		}

		#endregion

		#region events

		public event EventHandler<VolumeProgressEventArgs> ProgressChanged;

		#endregion

		#region interface IProgress<VolumeSliceDefinition>

		public void Report( VolumeSliceDefinition value )
		{
			var target = _Volume.Metadata.GetSize( value.Direction );

			ProgressChanged?.Invoke( this, new VolumeProgressEventArgs( ( double ) value.Index / target, $"Loaded slice {value.Index} of {target}" ) );
		}

		#endregion
	}
}