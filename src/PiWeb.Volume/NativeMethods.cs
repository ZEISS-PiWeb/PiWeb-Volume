#region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2020                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

// ReSharper disable UnusedParameter.Global
namespace Zeiss.PiWeb.Volume;

#region usings

using Zeiss.PiWeb.Volume.Interop;

#endregion

internal static class NativeMethods
{
	#region methods

	internal static VolumeError CompressVolume( InteropSliceReader inputStream, InteropStream outputStream, ushort width, ushort height, string encoderName, string pixelFormat, string options, int bitrate )
	{
		return VolumeError.CodecInvalidName;
	}

	internal static VolumeError DecompressVolume( InteropStream inputStream, InteropSliceWriter outputStream )
	{
		return VolumeError.CodecInvalidName;
	}

	internal static VolumeError DecompressSlices( InteropStream inputStream, InteropSliceWriter outputStream, ushort index, ushort count )
	{
		return VolumeError.CodecInvalidName;
	}

	#endregion
}