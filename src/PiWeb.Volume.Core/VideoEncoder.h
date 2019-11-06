#pragma once

#pragma region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#pragma endregion

extern "C" {

#include <libavcodec/avcodec.h>
#include <libavutil/opt.h>
#include <libavformat/avformat.h>
#include <libswscale/swscale.h>
#include <libavutil/pixdesc.h>
}


#include "StreamIoContext.h"
#include "VolumeError.h"
#include "SliceReader.h"

class VideoEncoder
{
	struct Context {
		std::shared_ptr<AVFormatContext> mFormatContext;
		std::shared_ptr<StreamIoContext> mOutputContext;
		std::shared_ptr<AVIOContext> mIoContext;		
		AVStream* mpStream;
		AVCodec * mpCodec;
		std::shared_ptr<AVCodecContext> mCodecContext;
		std::shared_ptr<AVFrame> mFrame;
		std::shared_ptr<AVFrame> mTempFrame;
		std::shared_ptr<SwsContext> mSwsContext;
		int64_t next_pts = 0;
	};

private:
	
	unsigned __int16 mWidth;
	unsigned __int16 mHeight;

	static VolumeError CreateFormatContext(Context& result, Stream* outputStream);
	static VolumeError AllocateFrame(enum AVPixelFormat pix_fmt, unsigned short width, unsigned short height, AVFrame** ppFrame );
	static VolumeError InitializeContext(Context& context);
	VolumeError CreateCodec(Context& context, const char* codecName, const char* pixelFormat, const char* options, int bitrate) const;
	static VolumeError GetVideoFrame( Context& context, SliceReader* inputStream, AVFrame** ppFrame);
	static VolumeError WriteVideoFrame( Context& context, SliceReader* inputStream, Stream* outputStream, bool* finished );

public:
	VideoEncoder(unsigned short width, unsigned short height);
	VolumeError Encode(SliceReader* inputStream, Stream* outputStream, const char* codec, const char* pixelFormat, const char* options, int bitrate) const;
};

