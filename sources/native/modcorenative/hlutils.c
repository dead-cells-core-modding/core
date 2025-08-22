
#include "modcorenative.h"

#ifdef WIN32
EXTERNC EXPORT void* hlu_get_hl_bytecode_from_exe(const uchar* exePath, int* outSize)
{
	HMODULE exe = LoadLibraryEx(exePath, NULL, LOAD_LIBRARY_AS_DATAFILE | LOAD_LIBRARY_AS_IMAGE_RESOURCE);
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

EXTERNC EXPORT void* hlu_get_exception_handle_helper() {
	return asm_prepare_exception_handle;
}

#ifndef WIN32

EXTERNC EXPORT void* hlu_load_so(const char* path, char** err)
{
	void* result = dlopen(path, RTLD_GLOBAL | RTLD_NOW);
	*err = dlerror();
	return result;
}
#endif

