#pragma once

#pragma region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#pragma endregion

#include "SliceReader.h"
#include "SliceWriter.h"
#include "Stream.h"

#ifdef __cplusplus
extern "C" {
#endif

#ifdef PIWEBVOLUME_EXPORTS 
#define PIWEBVOLUMEAPI __declspec( dllexport ) 
#else
#define PIWEBVOLUMEAPI __declspec( dllimport ) 
#endif

	int PIWEBVOLUMEAPI CompressVolume( SliceReader* input, Stream* output, unsigned short width, unsigned short height, const char* encoderName, const char* pixelFormat, const char* options, int bitrate );

	int PIWEBVOLUMEAPI DecompressVolume( Stream* input, SliceWriter* output );

	int PIWEBVOLUMEAPI DecompressSlices(Stream* input, SliceWriter* output, unsigned short index, unsigned short count);

#ifdef __cplusplus
}
#endif
