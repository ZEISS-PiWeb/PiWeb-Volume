#pragma region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#pragma endregion

#include "StreamIoContext.h"


StreamIoContext::StreamIoContext(Stream* pStream)
{
	mpStream = pStream;

	mBuffersize = 32 * 1024;
	mpBuffer = new unsigned char[mBuffersize];
}

int StreamIoContext::Write(uint8_t* buffer, int size) const
{	
	return mpStream->Write(static_cast<const void*>(buffer), size);
}

int StreamIoContext::Read(uint8_t* buf, int bufSize) const
{	
	return mpStream->Read(static_cast<void*>(buf), bufSize);	
}

int64_t StreamIoContext::Seek(int64_t pos, int32_t whence) const
{
	switch (whence)
	{
	case SEEK_CUR:
	case SEEK_SET:
	case SEEK_END:
		return mpStream->Seek(pos, whence);
	case AVSEEK_SIZE:
		return -1;
	default: ;
	}

	return -1;
}

int StreamIoContext::Write(void * self, uint8_t* buffer, int size)
{
	const auto context = static_cast<StreamIoContext*>(self);
	return context->Write(buffer, size);
}

int StreamIoContext::Read(void* ptr, uint8_t* buf, int bufSize)
{
	const auto context = static_cast<StreamIoContext*>(ptr);
	return context->Read(buf, bufSize);
}

int64_t StreamIoContext::Seek(void* ptr, int64_t pos, int32_t whence)
{
	const auto context = static_cast<StreamIoContext*>(ptr);
	return context->Seek(pos, whence);
}

std::shared_ptr<AVIOContext> StreamIoContext::CreateContext(bool writable) {
	const auto _context = avio_alloc_context(mpBuffer, mBuffersize, writable ? 1 : 0, static_cast<void*>(this), Read, Write, Seek);

	if (_context != nullptr)
		return std::shared_ptr<AVIOContext>(_context, [](AVIOContext* context) {
		avio_flush(context);
		avio_context_free(&context);
	});
	else
		return std::shared_ptr<AVIOContext>(nullptr);
}