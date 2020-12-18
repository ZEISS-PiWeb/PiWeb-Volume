#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.IMT.PiWeb.Volume.Interop
{
	#region usings

	using System;
	using System.IO;
	using System.Runtime.InteropServices;

	#endregion

	internal delegate int Read( IntPtr ptr, int length );

	internal delegate int Write( IntPtr ptr, int length );

	internal delegate long Seek( long pos, SeekOrigin origin );

	[StructLayout( LayoutKind.Sequential )]
	internal class InteropStream
	{
		#region members

		public Read Read;

		public Write Write;

		public Seek Seek;

		#endregion
	}
}