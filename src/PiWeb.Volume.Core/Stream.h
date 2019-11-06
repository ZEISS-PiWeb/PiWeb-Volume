#pragma once

#pragma region Copyright

/* * * * * * * * * * * * * * * * * * * * * * * * * */
/* Carl Zeiss IMT (IZfM Dresden)                   */
/* Softwaresystem PiWeb                            */
/* (c) Carl Zeiss 2019                             */
/* * * * * * * * * * * * * * * * * * * * * * * * * */

#pragma endregion


typedef __int32(__stdcall *Read)(void*, __int32 );
typedef __int32(__stdcall *Write)(const void*, __int32 );
typedef __int64(__stdcall *Seek)(__int64, unsigned __int32 );

typedef struct {

	Read Read;
	Write Write;
	Seek Seek;

} Stream;