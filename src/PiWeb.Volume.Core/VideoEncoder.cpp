#pragma region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#pragma endregion

#include "VideoEncoder.h"

VideoEncoder::VideoEncoder(const unsigned __int16 width, const unsigned __int16 height)
{
	mWidth = width;
	mHeight = height;
}

VolumeError VideoEncoder::CreateFormatContext(Context& result, Stream* outputStream)
{
	auto outputContext = std::make_shared<StreamIoContext>(outputStream);
	auto ioContext = outputContext->CreateContext(true);

	const auto pAVFormatContext = avformat_alloc_context();

	pAVFormatContext->pb = ioContext.get();
	pAVFormatContext->flags = AVFMT_FLAG_CUSTOM_IO;

	const auto outputFormat = av_guess_format("matroska", nullptr, nullptr);
	pAVFormatContext->oformat = outputFormat;

	const auto formatContext = std::shared_ptr<AVFormatContext>(pAVFormatContext, [](AVFormatContext* format) {
		if (format->streams != nullptr)
			format->streams[0]->codec = nullptr;
		avformat_free_context(format);
	});

	result.mOutputContext = outputContext;
	result.mIoContext = ioContext;
	result.mFormatContext = formatContext;

	return VolumeError::Success;
}

VolumeError VideoEncoder::CreateCodec(Context& context, const char* codecName,  const char* pixelFormat, const char* options, int bitrate) const
{	
	if (codecName == nullptr || strlen(codecName) == 0)
		return VolumeError::CodecInvalidName;

	const auto codec = avcodec_find_encoder_by_name(codecName);
	if (!codec) {
		return VolumeError::CodecInvalidName;
	}

	context.mpCodec = codec;
	context.mpStream = avformat_new_stream(context.mFormatContext.get(), nullptr);
	if (context.mpStream == nullptr) {
		return VolumeError::FormatContextStreamAllocFailed;
	}

	auto pCodecContext = avcodec_alloc_context3(codec);
	if (!pCodecContext) {
		return VolumeError::CodecContextAllocationFailed;
	}
	
	context.mCodecContext = std::shared_ptr<AVCodecContext>(pCodecContext, [](AVCodecContext* c) {	avcodec_free_context(&c); });
	context.mpStream->codec = pCodecContext;

	if (bitrate > 0)
		pCodecContext->bit_rate = bitrate;

	pCodecContext->width = mWidth;
	pCodecContext->height = mHeight;
	pCodecContext->time_base = AVRational{ 1, 25 };
	pCodecContext->framerate = AVRational{ 25, 1 };

	const auto pix_fmt = av_get_pix_fmt(pixelFormat);
	if (pix_fmt == AV_PIX_FMT_NONE)
		return VolumeError::CodecInvalidPixelFormat;

	pCodecContext->gop_size = 32;// 1024;
	pCodecContext->pix_fmt = pix_fmt;
	pCodecContext->level = 31;

	if (options != nullptr && strlen(options) > 0 && av_set_options_string(static_cast<void*>(context.mCodecContext.get()), options, "= ", ";,") < 0)
		return VolumeError::CodecInvalidOptions;

	if (pCodecContext->codec_id == AV_CODEC_ID_MPEG2VIDEO) {
		/* just for testing, we also add B-frames */
		pCodecContext->max_b_frames = 2;

	}
	if (pCodecContext->codec_id == AV_CODEC_ID_MPEG1VIDEO) {
		pCodecContext->mb_decision = 2;

	}

	if (context.mFormatContext->oformat->flags & AVFMT_GLOBALHEADER)
		pCodecContext->flags |= AV_CODEC_FLAG_GLOBAL_HEADER;

	context.mpStream->time_base = pCodecContext->time_base;
	context.mpStream->r_frame_rate = pCodecContext->framerate;

	return VolumeError::Success;
}

VolumeError VideoEncoder::AllocateFrame(enum AVPixelFormat pix_fmt, unsigned short width, unsigned short height, AVFrame** frame)
{
	const auto picture = av_frame_alloc();
	if (!picture)
		return VolumeError::FrameAllocationFailed;

	picture->format = pix_fmt;
	picture->width = static_cast<int>(width);
	picture->height = static_cast<int>(height);

	/* allocate the buffers for the frame data */
	if (av_frame_get_buffer(picture, 32) < 0)
		return VolumeError::FrameBufferAllocationFailed;

	*frame = picture;

	return VolumeError::Success;
}

VolumeError VideoEncoder::InitializeContext(Context& context)
{
	VolumeError result;
	AVFrame* pFrame;
	AVFrame* pTmpFrame;

	const auto c = context.mCodecContext.get();

	if (avcodec_open2(c, context.mpCodec, nullptr) < 0) {
		return VolumeError::CodecOpenFailed;
	}

	const auto pSwsContext = sws_getContext(c->width, c->height, AV_PIX_FMT_GRAY8, c->width, c->height, c->pix_fmt, SWS_BICUBIC, nullptr, nullptr, nullptr);

	if (pSwsContext == nullptr)
		return VolumeError::ScaleContextAllocationFailed;

	context.mSwsContext = std::shared_ptr<SwsContext>(pSwsContext, [](SwsContext* s) {	sws_freeContext(s); });

	if ((result = AllocateFrame(context.mCodecContext->pix_fmt, context.mCodecContext->width, context.mCodecContext->height, &pFrame)) != VolumeError::Success)
		return result;

	context.mFrame = std::shared_ptr<AVFrame>(pFrame, [](AVFrame* f) {	av_frame_free(&f); });

	if ((result = AllocateFrame(AV_PIX_FMT_GRAY8, c->width, c->height, &pTmpFrame)) != VolumeError::Success)
		return result;

	context.mTempFrame = std::shared_ptr<AVFrame>(pTmpFrame, [](AVFrame* f) {	av_frame_free(&f); });

	return VolumeError::Success;
}

VolumeError VideoEncoder::GetVideoFrame(Context& context, SliceReader* inputStream, AVFrame** ppFrame)
{
	const auto c = context.mCodecContext.get();
	const auto sws = context.mSwsContext.get();
	auto pFrame = context.mFrame.get();
	auto pTmpFrame = context.mTempFrame.get();
	
	if (av_frame_make_writable(pTmpFrame) < 0)
		return VolumeError::FrameNotWritable;

	if (c->width > USHRT_MAX || c->height > USHRT_MAX)
		return VolumeError::FrameInvalidSize;

	if (!inputStream->ReadSlice(pTmpFrame->data[0], static_cast<unsigned __int16>(pTmpFrame->linesize[0]), static_cast<unsigned __int16>(pTmpFrame->height)))
	{
		*ppFrame = nullptr;
		return VolumeError::Success;
	}

	if (sws_scale(sws, static_cast<const uint8_t * const *>(pTmpFrame->data),
		pTmpFrame->linesize, 0, pTmpFrame->height, pFrame->data,
		pFrame->linesize) < 0)
		return VolumeError::ScaleContextRescaleFailed;

	pFrame->pts = context.next_pts++;
	*ppFrame = pFrame;

	return VolumeError::Success;
}

VolumeError VideoEncoder::WriteVideoFrame(Context& context, SliceReader* inputStream, Stream* outputStream, bool* finished)
{
	VolumeError result;

	AVFrame* pFrame = nullptr;

	if ((result = GetVideoFrame(context, inputStream, &pFrame)) != VolumeError::Success)
		return result;

	const auto codecContext = context.mCodecContext.get();
	const auto formatContext = context.mFormatContext.get();

	AVPacket pkt = { nullptr };
	av_init_packet(&pkt);

	int got_packet;

	if (avcodec_encode_video2(codecContext, &pkt, pFrame, &got_packet) < 0)
		return VolumeError::CodecEncodeFailed;

	if (got_packet)
	{
		av_packet_rescale_ts(&pkt, codecContext->time_base, context.mpStream->time_base);
		pkt.stream_index = context.mpStream->index;

		if (av_interleaved_write_frame(formatContext, &pkt) < 0)
			return VolumeError::CodecWriteInterleavedFrameFailed;
	}

	*finished = !(pFrame || got_packet);

	return VolumeError::Success;
}

VolumeError VideoEncoder::Encode(SliceReader* inputStream, Stream* outputStream, const char* codec, const char* pixelFormat, const char* options, int bitrate) const
{

	Context context;
	VolumeError result;

	if ((result = CreateFormatContext(context, outputStream)) < 0)
		return result;

	if ((result = CreateCodec(context, codec, pixelFormat, options, bitrate)) != VolumeError::Success)
		return result;

	if ((result = InitializeContext(context)) != VolumeError::Success)
		return result;

	const auto pFormatContext = context.mFormatContext.get();

	if (avformat_write_header(pFormatContext, nullptr) < 0)
		return VolumeError::FormatContextWriteHeaderFailed;

	bool finished = false;
	while (!finished) {
		if ((result = WriteVideoFrame(context, inputStream, outputStream, &finished)) != VolumeError::Success)
			return result;
	}

	if (av_write_trailer(pFormatContext) < 0)
		return VolumeError::FormatContextWriteTailFailed;

	return VolumeError::Success;
}
