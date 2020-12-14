#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.UI.Model
{
    #region usings

    using System;

    #endregion

    public class DoubleProgress : IProgress<double>
    {
	    #region events

        public event EventHandler<VolumeProgressEventArgs> ProgressChanged;

        #endregion

        #region interface IProgress<VolumeSliceDefinition>

        public void Report( double value )
        {
	        ProgressChanged?.Invoke( this, new VolumeProgressEventArgs( value, $"Loaded {value:P}" ) );
        }

        #endregion
    }
}