#region copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss Industrielle Messtechnik GmbH        */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#endregion

namespace Zeiss.PiWeb.Volume;

/// <summary>
/// Errors returned by the volume compressor
/// </summary>
public enum VolumeError
{
	/// <summary>
	/// The success
	/// </summary>
	Success = 0,

	/// <summary>
	/// The io context allocation failed
	/// </summary>
	IoContextAllocationFailed = -0x00010001,

	/// <summary>
	/// Reading from the input stream failed
	/// </summary>
	IoContextReadFailed = -0x00010002,

	/// <summary>
	/// The format context allocation failed
	/// </summary>
	FormatContextAllocationFailed = -0x00020001,

	/// <summary>
	/// The format context open failed
	/// </summary>
	FormatContextOpenFailed = -0x00020002,

	/// <summary>
	/// The format context no video stream
	/// </summary>
	FormatContextNoVideoStream = -0x00020003,

	/// <summary>
	/// The format context no video stream information
	/// </summary>
	FormatContextNoVideoStreamInfo = -0x00020004,

	/// <summary>
	/// The format context unsupported format
	/// </summary>
	FormatContextUnsupportedFormat = -0x00020005,

	/// <summary>
	/// The format context stream alloc failed
	/// </summary>
	FormatContextStreamAllocFailed = -0x00020006,

	/// <summary>
	/// The format context write header failed
	/// </summary>
	FormatContextWriteHeaderFailed = -0x00020007,

	/// <summary>
	/// The format context write tail failed
	/// </summary>
	FormatContextWriteTailFailed = -0x00020008,

	/// <summary>
	/// The format context seek failed
	/// </summary>
	FormatContextSeekFailed = -0x00020009,

	/// <summary>
	/// The codec context allocation failed
	/// </summary>
	CodecContextAllocationFailed = -0x00030001,

	/// <summary>
	/// The codec invalid name
	/// </summary>
	CodecInvalidName = -0x00030002,

	/// <summary>
	/// The specified codec options are invalid.
	/// </summary>
	CodecInvalidOptions = -0x00030003,

	/// <summary>
	/// Cannot open the specified codec.
	/// </summary>
	CodecOpenFailed = -0x00030004,

	/// <summary>
	/// The encoding failed
	/// </summary>
	CodecEncodeFailed = -0x00030005,

	/// <summary>
	/// The decoding failed
	/// </summary>
	CodecDecodeFailed = -0x00030006,

	/// <summary>
	/// Error when sending a frame package
	/// </summary>
	CodecSendPackageFailed = -0x00030007,

	/// <summary>
	/// Error on receiving a frame package
	/// </summary>
	CodecReceiveFrameFailed = -0x00030008,

	/// <summary>
	/// The codec write interleaved frame failed
	/// </summary>
	CodecWriteInterleavedFrameFailed = -0x00030009,

	/// <summary>
	/// The specified pixel format is unknown
	/// </summary>
	CodecInvalidPixelFormat = -0x0003000A,

	/// <summary>
	/// The image allocation failed
	/// </summary>
	ImageAllocationFailed = -0x00040001,

	/// <summary>
	/// The frame allocation failed
	/// </summary>
	FrameAllocationFailed = -0x00050001,

	/// <summary>
	/// The frame buffer allocation failed
	/// </summary>
	FrameBufferAllocationFailed = -0x00050002,

	/// <summary>
	/// The frame is not writable
	/// </summary>
	FrameNotWritable = -0x00050003,

	/// <summary>
	/// The frame buffer is too small to store the frame.
	/// </summary>
	FrameBufferTooSmall = -0x00050004,

	/// <summary>
	/// The scale context allocation failed
	/// </summary>
	ScaleContextAllocationFailed = -0x00060001,

	/// <summary>
	/// The scale context rescale failed
	/// </summary>
	ScaleContextRescaleFailed = -0x00060002,
}