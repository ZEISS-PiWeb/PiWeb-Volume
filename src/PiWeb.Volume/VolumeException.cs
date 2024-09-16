#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume
{
	#region usings

	using System;

	#endregion

	/// <summary>
	/// Exceptions thrown by the volume compressor and decompressor
	/// </summary>
	/// <seealso cref="System.Exception" />
	public sealed class VolumeException : Exception
	{
		#region constructors

		/// <inheritdoc />
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Zeiss.PiWeb.Volume.VolumeException" /> class.
		/// </summary>
		/// <param name="error">The error.</param>
		internal VolumeException( VolumeError error )
		{
			Error = error;
		}

		/// <inheritdoc />
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Zeiss.PiWeb.Volume.VolumeException" /> class.
		/// </summary>
		/// <param name="error">The error.</param>
		/// <param name="message">The message.</param>
		internal VolumeException( VolumeError error, string message ) : base( message )
		{
			Error = error;
		}

		/// <inheritdoc />
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Zeiss.PiWeb.Volume.VolumeException" /> class.
		/// </summary>
		/// <param name="error">The error.</param>
		/// <param name="message">The message.</param>
		/// <param name="innerException">The inner exception.</param>
		internal VolumeException( VolumeError error, string message, Exception innerException ) : base( message, innerException )
		{
			Error = error;
		}

		#endregion

		#region properties

		/// <summary>
		/// Gets the error code.
		/// </summary>
		/// <value>
		/// The error.
		/// </value>
		public VolumeError Error { get; }

		#endregion
	}
}