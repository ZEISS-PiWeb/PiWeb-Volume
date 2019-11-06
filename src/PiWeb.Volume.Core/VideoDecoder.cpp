#pragma region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#pragma endregion

#include "VideoDecoder.h"

VolumeError VideoDecoder::CreateFormatContext(FormatContext* result, Stream* inputStream)
{
	auto _inputContext = std::make_shared<StreamIoContext>(inputStream);
	auto _ioContext = _inputContext->CreateContext(false);

	if (_ioContext == nullptr)
		return VolumeError::IoContextAllocationFailed;

	const auto pAvFormatContext = avformat_alloc_context();

	pAvFormatContext->pb = _ioContext.get();
	pAvFormatContext->flags = AVFMT_FLAG_CUSTOM_IO;

	const auto probeSize = 32 * 1024;

	AVProbeData sProbeData;
	sProbeData.buf = new unsigned char[probeSize];
	sProbeData.buf_size = probeSize;
	sProbeData.filename = "";
	sProbeData.mime_type = nullptr;

	_inputContext->Read(sProbeData.buf, probeSize);

	pAvFormatContext->iformat = av_probe_input_format(&sProbeData, 1);

	delete sProbeData.buf;

	_inputContext->Seek(0, SEEK_SET);

	if (pAvFormatContext->iformat == nullptr)
		return VolumeError::FormatContextUnsupportedFormat;

	const auto formatContext = std::shared_ptr<AVFormatContext>(pAvFormatContext, [](AVFormatContext* format) { avformat_close_input(&format); });

	*result = FormatContext{
		_inputContext,
		_ioContext,
		formatContext
	};

	return VolumeError::Success;
}

VolumeError VideoDecoder::AllocateFrame(enum AVPixelFormat pix_fmt, unsigned short width, unsigned short height, AVFrame** frame)
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

VolumeError VideoDecoder::GetVideoStream(FormatContext* input, AVStream** stream) {

	int result;
	auto _raw_format_context = input->mFormatContext.get();

	if ((result = avformat_open_input(&_raw_format_context, "", _raw_format_context->iformat, nullptr)) < 0)
		return VolumeError::FormatContextOpenFailed;



	if ((result = avformat_find_stream_info(_raw_format_context, nullptr)) < 0)
		return VolumeError::FormatContextNoVideoStreamInfo;



	if ((result = av_find_best_stream(_raw_format_context, AVMEDIA_TYPE_VIDEO, -1, -1, nullptr, 0)) < 0)
		return VolumeError::FormatContextNoVideoStream;

	*stream = _raw_format_context->streams[result];

	return VolumeError::Success;
}

VolumeError VideoDecoder::Initialize(FormatContext* context, Stream* inputStream)
{
	VolumeError result;
	AVFrame* pFrame;
	AVFrame* pTmpFrame;

	if ((result = CreateFormatContext(context, inputStream)) != VolumeError::Success)
		return result;

	if ((result = GetVideoStream(context, &context->mpStream)) != VolumeError::Success)
		return result;

	const auto decoderParameters = context->mpStream->codecpar;
	const auto decoder = avcodec_find_decoder(decoderParameters->codec_id);

	if (decoder == nullptr)
		return VolumeError::FormatContextUnsupportedFormat;

	auto decoderContext = context->mpStream->codec;

	if (avcodec_open2(decoderContext, decoder, nullptr) < 0)
		return VolumeError::CodecOpenFailed;

	decoderContext->flags |= AV_CODEC_FLAG_LOW_DELAY;
	decoderContext->flags2 |= AV_CODEC_FLAG2_FAST;

	context->mCodecContext = std::shared_ptr<AVCodecContext>(decoderContext, [](AVCodecContext* c) { avcodec_close(c); });

	const auto pSwsContext = sws_getContext(decoderContext->width, decoderContext->height, decoderContext->pix_fmt, decoderContext->width, decoderContext->height, AV_PIX_FMT_GRAY8, SWS_BICUBIC, nullptr, nullptr, nullptr);

	if (pSwsContext == nullptr)
		return VolumeError::ScaleContextAllocationFailed;

	context->mSwsContext = std::shared_ptr<SwsContext>(pSwsContext, [](SwsContext* s) {	sws_freeContext(s); });

	if ((result = AllocateFrame(AV_PIX_FMT_GRAY8, context->mCodecContext->width, context->mCodecContext->height, &pFrame)) != VolumeError::Success)
		return result;

	context->mFrame = std::shared_ptr<AVFrame>(pFrame, [](AVFrame* f) {	av_frame_free(&f); });

	if ((result = AllocateFrame(context->mCodecContext->pix_fmt, context->mCodecContext->width, context->mCodecContext->height, &pTmpFrame)) != VolumeError::Success)
		return result;

	context->mTempFrame = std::shared_ptr<AVFrame>(pTmpFrame, [](AVFrame* f) {	av_frame_free(&f); });

	return VolumeError::Success;
}

AVPacket VideoDecoder::CreatePacket()
{
	AVPacket pkt;
	av_init_packet(&pkt);
	pkt.data = nullptr;
	pkt.size = 0;

	return pkt;
}

int64_t VideoDecoder::FrameToDts(AVStream* pavStream, const int64_t frame)
{
	return frame * pavStream->r_frame_rate.den *  pavStream->time_base.den / (int64_t(pavStream->r_frame_rate.num) *	pavStream->time_base.num);
}

int64_t VideoDecoder::DtsToFrame(AVStream* pavStream, const int64_t dts)
{
	return dts * pavStream->r_frame_rate.num *  pavStream->time_base.num / (int64_t(pavStream->r_frame_rate.den) *	pavStream->time_base.den);
}

VolumeError VideoDecoder::Decode(Stream* inputStream, SliceWriter* outputStream, const unsigned short index, const unsigned short count)
{
	VolumeError result;
	FormatContext input;
	if ((result = Initialize(&input, inputStream)) != VolumeError::Success)
		return result;

	const auto pFormatContext = input.mFormatContext.get();

	if (index > 0)
	{
		const auto seekTarget = FrameToDts(pFormatContext->streams[0], index);
		if (av_seek_frame(pFormatContext, 0, seekTarget, AVSEEK_FLAG_FRAME | AVSEEK_FLAG_BACKWARD) < 0) {
			return VolumeError::FormatContextSeekFailed;
		}
	}

	result = DecodeFrames(input, outputStream, index, count);

	return result;

}

VolumeError VideoDecoder::DecodeFrames(FormatContext context, SliceWriter* outputStream, const unsigned short index, const unsigned short count)
{
	const auto pFormatContext = context.mFormatContext.get();
	const auto pStream = context.mpStream;

	auto pkt = CreatePacket();

	int decoded;
	VolumeError result;
	int error;

	while ((error = av_read_frame(pFormatContext, &pkt)) == 0) {

		const auto frameIndex = DtsToFrame(pStream, pkt.dts);

		if (frameIndex >= index + count)
			return VolumeError::Success;

		result = DecodeFrame(context, &pkt, &decoded, outputStream, index, count);
		if (result != VolumeError::Success)
			return result;

		av_packet_unref(&pkt);
	}

	if (error != AVERROR_EOF && error != AVERROR(EAGAIN))
		return VolumeError::CodecReceiveFrameFailed;

	pkt.data = nullptr;
	pkt.size = 0;


	do {
		result = DecodeFrame(context, &pkt, &decoded, outputStream, index, count);
		if (result != VolumeError::Success)
			return result;

	} while (decoded);

	return VolumeError::Success;
}

VolumeError VideoDecoder::DecodeFrame(
	FormatContext context,
	AVPacket* packet,
	int *decoded,
	SliceWriter* outputStream,
	const unsigned short index,
	const unsigned short count)
{	
	const auto pDecoderContext = context.mCodecContext.get();
	const auto pTempFrame = context.mTempFrame.get();
	const auto pFrame = context.mFrame.get();
	const auto sws = context.mSwsContext.get();
	const auto pStream = context.mpStream;

	int result;
	*decoded = 0;

	if ((result = avcodec_decode_video2(pDecoderContext, pTempFrame, decoded, packet)) < 0)
	{
		if (result == AVERROR(EAGAIN) || result == AVERROR_EOF)
			return VolumeError::Success;

		return VolumeError::CodecSendPackageFailed;
	}

	if (*decoded)
	{
		const auto slice = DtsToFrame(pStream, packet->dts);

		if (slice < index || slice > index + count - 1 )
			return VolumeError::Success;
			
		if (sws_scale(sws, static_cast<const uint8_t * const *>(pTempFrame->data),
			pTempFrame->linesize, 0, pTempFrame->height, pFrame->data,
			pFrame->linesize) < 0)
			return VolumeError::ScaleContextRescaleFailed;

		const auto width = pFrame->linesize[0];
		const auto height = pDecoderContext->height;

		if (width > USHRT_MAX || width < 0 || height > USHRT_MAX || height < 0 || slice > USHRT_MAX || slice < 0)
			return VolumeError::FrameInvalidSize;

		const auto error = WriteFrame(outputStream, pFrame->data[0], static_cast<unsigned __int16>(width), static_cast<unsigned __int16>(height), static_cast<unsigned __int16>(slice));
		if (error != VolumeError::Success)
			return error;
	}

	return VolumeError::Success;
}


VolumeError VideoDecoder::WriteFrame(SliceWriter* destination, const uint8_t* source, const unsigned __int16 width, const unsigned __int16 height, const unsigned __int16 slice)
{
	destination->WriteSlice(static_cast<const void*>(source), width, height, slice);

	return VolumeError::Success;
}