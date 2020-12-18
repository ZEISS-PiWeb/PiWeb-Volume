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
	using System.Runtime.InteropServices;

	#endregion

	internal delegate void WriteSliceCallback( IntPtr line, ushort width, ushort height, ushort z );

	[StructLayout( LayoutKind.Sequential )]
	internal class InteropSliceWriter
	{
		#region members

		public WriteSliceCallback WriteSlice;

		#endregion
	}
}