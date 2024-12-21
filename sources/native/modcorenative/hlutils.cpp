
#include "modcorenative.h"

#if WIN32
EXTERNC EXPORT void* hlu_get_hl_bytecode_from_exe(const uchar* exePath, int* outSize)
{
	HMODULE exe = LoadLibraryW(exePath);
	if (exe == NULL) {
		return NULL;
	}
	HRSRC res = FindResource(exe, "hlboot.dat", RT_RCDATA);
	if (res == NULL) {
		FreeLibrary(exe);
		return NULL;
	}
	*outSize = SizeofResource(exe, res);
	return LockResource(LoadResource(exe, res));
}
#endif

