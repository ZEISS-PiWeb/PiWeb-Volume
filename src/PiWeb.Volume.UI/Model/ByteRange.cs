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

	public readonly struct ByteRange
	{
		#region constructors

		public ByteRange( byte start, byte stop )
		{
			Start = start;
			Stop = stop;
		}

		#endregion

		#region properties

		public byte Start { get; }
		public byte Stop { get; }

		public byte Lower => Math.Min( Start, Stop );
		public byte Upper => Math.Max( Start, Stop );
		public byte Size => (byte)Math.Abs( Stop - Start );

		#endregion
	}
}