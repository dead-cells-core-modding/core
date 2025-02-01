
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

EXTERNC EXPORT void* hlu_call_c2hl(void* f, hl_type* t, void** args, vdynamic* ret)
{
	hl_trap_ctx_ex ctx;
	vdynamic* exc;
	hl_trap(ctx, exc, on_exception);

	ctx.tcheck = 0x4e455445;

	void* result = callback_c2hl(f, t, args, ret);
	hl_endtrap(ctx);
	return result;
on_exception:
	hl_fatal("on_exception");
	return NULL;
}

