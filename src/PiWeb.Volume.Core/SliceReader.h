#pragma once

#pragma region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#pragma endregion


typedef bool(__stdcall *ReadSlice)(void*, unsigned __int16, unsigned __int16);
typedef bool(__stdcall *ReadMask)(void*, unsigned __int16, unsigned __int16);

typedef struct {

	ReadSlice ReadSlice;
	ReadMask ReadMask;

} SliceReader;