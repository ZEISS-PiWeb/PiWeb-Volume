#pragma once

#pragma region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#pragma endregion


extern "C" {

#include <libavformat/avformat.h>
#include <libswscale/swscale.h>

}

#include "StreamIoContext.h"
#include "VolumeError.h"
#include "SliceWriter.h"
#include <climits>

class VideoDecoder
{
	struct FormatContext {
		std::shared_ptr<StreamIoContext> mInputContext;
		std::shared_ptr<AVIOContext> mIoContext;
		std::shared_ptr<AVFormatContext> mFormatContext;
		std::shared_ptr<AVCodecContext> mCodecContext;
		std::shared_ptr<AVFrame> mFrame;
		std::shared_ptr<AVFrame> mTempFrame;
		std::shared_ptr<SwsContext> mSwsContext;
		AVStream* mpStream;
	};

private:

	static inline int64_t FrameToDts(AVStream* pavStream, int64_t frame);
	static inline int64_t DtsToFrame(AVStream* pavStream, int64_t dts);
	static AVPacket CreatePacket();

	static VolumeError Initialize(FormatContext* context, Stream* inputStream);
	static VolumeError CreateFormatContext(FormatContext* result, Stream* inputStream);
	static VolumeError AllocateFrame(enum AVPixelFormat pix_fmt, unsigned short width, unsigned short height, AVFrame** ppFrame );
	static VolumeError GetVideoStream(FormatContext* input, AVStream** stream);
	static VolumeError DecodeFrames(FormatContext context, SliceWriter* outputStream, const unsigned short index, const unsigned short count);
	static VolumeError DecodeFrame(FormatContext context, AVPacket* packet, int* decoded, SliceWriter* outputStream, const unsigned short index, const
	                               unsigned short count);
	static VolumeError WriteFrame(SliceWriter* destination, const uint8_t* source, unsigned __int16 width, unsigned __int16 height, unsigned __int16 slice);

public:

	static VolumeError Decode(Stream* inputStream, SliceWriter* outputStream, unsigned short index = 0, unsigned short count = USHRT_MAX);
};

