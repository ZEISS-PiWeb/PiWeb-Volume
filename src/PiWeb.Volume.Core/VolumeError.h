#pragma once

#pragma region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#pragma endregion

enum VolumeError : int
{
	Success = 0x00000000,
	IoContextAllocationFailed = -0x00010001,
	FormatContextAllocationFailed = -0x00020001,
	FormatContextOpenFailed = -0x00020002,
	FormatContextNoVideoStream = -0x00020003,
	FormatContextNoVideoStreamInfo = -0x00020004,
	FormatContextUnsupportedFormat = -0x00020005,
	FormatContextStreamAllocFailed = -0x00020006,
	FormatContextWriteHeaderFailed = -0x00020007, 
	FormatContextWriteTailFailed = -0x00020008,
	FormatContextSeekFailed = -0x00020009,
	CodecContextAllocationFailed = -0x00030001,
	CodecInvalidName = -0x00030002,	
	CodecInvalidOptions = -0x00030003,
	CodecOpenFailed = -0x00030004,
	CodecEncodeFailed = -0x00030005,
	CodecDecodeFailed = -0x00030006,
	CodecSendPackageFailed = -0x00030007,
	CodecReceiveFrameFailed = -0x00030008,
	CodecWriteInterleavedFrameFailed = -0x00030009,
	CodecInvalidPixelFormat = -0x0003000A,
	ImageAllocationFailed = -0x00040001,
	FrameAllocationFailed = -0x00050001,
	FrameBufferAllocationFailed = -0x00050002,
	FrameNotWritable = -0x00050003,
	FrameInvalidSize = -0x00050004,
	ScaleContextAllocationFailed = -0x00060001,
	ScaleContextRescaleFailed = -0x00060002	
};