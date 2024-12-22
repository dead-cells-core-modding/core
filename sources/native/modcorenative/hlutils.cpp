
#include "modcorenative.h"

#if WIN32
EXTERNC EXPORT void* hlu_get_hl_bytecode_from_exe(const uchar* exePath, int* outSize)
{
	HMODULE exe = LoadLibrary(exePath);
	if (exe == NULL) {
		return NULL;
	}
	HRSRC res = FindResource(exe, L"hlboot.dat", RT_RCDATA);
	if (res == NULL) {
		FreeLibrary(exe);
		return NULL;
	}
	*outSize = SizeofResource(exe, res);
	HGLOBAL hres = LoadResource(exe, res);
	if (hres == NULL) {
		FreeLibrary(exe);
		return NULL;
	}
	return LockResource(hres);
}
#endif

