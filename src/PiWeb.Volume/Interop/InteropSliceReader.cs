#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume.Interop
{
	#region usings

	using System;
	using System.Runtime.InteropServices;

	#endregion

	internal delegate bool ReadSliceCallback( IntPtr slice, ushort width, ushort height );

	[StructLayout( LayoutKind.Sequential )]
	internal class InteropSliceReader
	{
		#region members

		public ReadSliceCallback? ReadSlice;

		#endregion
	}
}