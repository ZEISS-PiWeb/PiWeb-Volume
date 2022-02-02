#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume
{
	#region usings

	using System;

	#endregion

	/// <summary>
	/// Exception that is used when the volume is initialized with invalid slice data.
	/// </summary>
	public sealed class VolumeIntegrityException : Exception
	{
		#region constructors

		/// <inheritdoc />
		internal VolumeIntegrityException( string message )
			: base( message )
		{ }

		#endregion
	}
}