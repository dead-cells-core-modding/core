
#include "modcorenative.h"

#if WIN32
#include <DbgHelp.h>
#endif

EXTERNC EXPORT void* mcn_get_ebp()
{
	return get_ebp();
}

EXTERNC EXPORT void* mcn_get_esp()
{
	return get_esp();
}

EXTERNC EXPORT int mcn_load_stacktrace(void** buf, int maxCount, void* bottom)
{
	void* top = get_ebp();

	if (top == NULL)
	{
		return 0;
	}

	int count = 0;

	while (count < maxCount && top <= bottom)
	{
		void* ebp = ((void**)top)[0];
		void* ret = ((void**)top)[1];
		buf[count++] = ebp;
		top = ebp;
	}

	return count;
}

EXTERNC void init_trace()
{
#if WIN32
	SymInitialize(GetCurrentProcess(), NULL, TRUE);
#endif
}

EXTERNC EXPORT int mcn_get_sym(void* ptr, wchar_t* symNameBuf, int* symNameLen, 
	wchar_t* moduleNameBuf, int* moduleNameBufLen,
	char** fileName, int* line)
{
#if WIN32
	int maxNameLen = 256;

	DWORD64  dwDisplacement = 0;
	{
		
		SYMBOL_INFO* sym = (SYMBOL_INFO*)alloca(sizeof(SYMBOL_INFO) + maxNameLen * sizeof(TCHAR));
		sym->SizeOfStruct = sizeof(SYMBOL_INFO);
		sym->MaxNameLen = maxNameLen;
		if (SymFromAddrW(GetCurrentProcess(), ptr, &dwDisplacement, sym))
		{
			*symNameLen = sym->NameLen;
			lstrcpyW(symNameBuf, sym->Name);
			*moduleNameBufLen = GetModuleFileNameW(sym->ModBase, moduleNameBuf, *moduleNameBufLen);
		}
	}
	{
		IMAGEHLP_LINE sym;
		sym.SizeOfStruct = sizeof(IMAGEHLP_LINE);

		if (SymGetLineFromAddr(GetCurrentProcess(), ptr, &dwDisplacement, &sym))
		{
			*fileName = sym.FileName;
			*line = sym.LineNumber;
		}
		else
		{
			*fileName = NULL;
			*line = 0;
		}
	}
	return 1;
#else
	return 0;
#endif
}
