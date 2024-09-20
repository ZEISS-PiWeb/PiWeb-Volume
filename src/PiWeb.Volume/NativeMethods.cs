#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume
{
	#region usings

	using System.Runtime.InteropServices;
	using Zeiss.PiWeb.Volume.Interop;

	#endregion

	internal static class NativeMethods
	{
		#region methods

		[DllImport( "PiWeb.Volume.Core.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
		internal static extern VolumeError CompressVolume( InteropSliceReader inputStream, InteropStream outputStream, ushort width, ushort height, [MarshalAs( UnmanagedType.LPStr )] string encoderName, [MarshalAs( UnmanagedType.LPStr )] string pixelFormat, [MarshalAs( UnmanagedType.LPStr )] string options, int bitrate );

		[DllImport( "PiWeb.Volume.Core.dll", CallingConvention = CallingConvention.Cdecl )]
		internal static extern VolumeError DecompressVolume( InteropStream inputStream, InteropSliceWriter outputStream );

		[DllImport( "PiWeb.Volume.Core.dll", CallingConvention = CallingConvention.Cdecl )]
		internal static extern VolumeError DecompressSlices( InteropStream inputStream, InteropSliceWriter outputStream, ushort index, ushort count );

		#endregion
	}
}