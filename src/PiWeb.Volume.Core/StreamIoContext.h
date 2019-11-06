#pragma once

#pragma region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#pragma endregion


#include "Stream.h"

extern "C" {
#include <libavformat/avformat.h>
}

#include <iostream>

class StreamIoContext
{
private:

	Stream* mpStream;
	int mBuffersize;
	unsigned char* mpBuffer;

	static int Read(void* ptr, uint8_t* buf, int bufSize);
	static int64_t Seek(void* ptr, int64_t pos, int32_t whence);
	static int Write(void * self, uint8_t* buffer, int size);
	
public:
	StreamIoContext(Stream* mpStream);	

	int Read(uint8_t* buf, int bufSize) const;
	int64_t Seek(int64_t pos, int32_t whence) const;
	int Write(uint8_t* buffer, int size) const;

	std::shared_ptr<AVIOContext> CreateContext( bool writable );
};

