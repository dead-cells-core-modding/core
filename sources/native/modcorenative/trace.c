
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

#if WIN32
static bool is_executable(void* ptr, int64 page_mask) {
	MEMORY_BASIC_INFORMATION meminfo = { 0 };
	if (VirtualQuery((int64)ptr & page_mask, &meminfo, sizeof(meminfo)) != 0) {
		if ((meminfo.Protect &
			(PAGE_EXECUTE | PAGE_EXECUTE_READ | PAGE_EXECUTE_READWRITE)) != 0) {
			return true;
		}
	}
	return false;
}
#endif

static int stack_frame_compare(const hlu_stack_frame* a, hlu_stack_frame* b) {
	return (int64)a->esp - (int64)b->esp;
}

EXTERNC EXPORT int mcn_load_stacktrace(hlu_stack_frame* buf, int maxCount, void* bottom)
{
#if WIN32

	SYSTEM_INFO sysInfo = { 0 };
	GetSystemInfo(&sysInfo);

	int64 page_mask = ~((int64)sysInfo.dwPageSize - 1);
#else
	int64 page_mask = ~((int64)sysconf(_SC_PAGESIZE) - 1);
#endif
	void** stack = &buf;
	
	int index = 0;
	while (stack <= bottom && index < maxCount) {
		void* ip = *stack;
		if (!is_executable(ip, page_mask)) {
			stack++;
			continue;
		}
		void* ebp = *(stack - 1);
		stack++;
		if (ebp <= stack || ebp >= bottom) {
			continue;
		}
		void* eip = ((void**)ebp)[1];
		if (is_executable(eip, page_mask)) {
			buf[index].eip = eip;
			buf[index].esp = ebp;
			index++;
		}
	}
	qsort(buf, index, sizeof(hlu_stack_frame), stack_frame_compare);
	return index;
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
		
		SYMBOL_INFOW* sym = (SYMBOL_INFO*)alloca(sizeof(SYMBOL_INFO) + maxNameLen * sizeof(TCHAR));
		sym->SizeOfStruct = sizeof(SYMBOL_INFO);
		sym->MaxNameLen = maxNameLen;
		if (SymFromAddrW(GetCurrentProcess(), (int64)ptr, &dwDisplacement, sym))
		{
			*symNameLen = sym->NameLen;
			lstrcpyW(symNameBuf, sym->Name);
			*moduleNameBufLen = GetModuleFileNameW((HMODULE)sym->ModBase, moduleNameBuf, *moduleNameBufLen);
		}
	}
	{
		IMAGEHLP_LINE sym;
		sym.SizeOfStruct = sizeof(IMAGEHLP_LINE);

		if (SymGetLineFromAddr(GetCurrentProcess(), (int64)ptr, &dwDisplacement, &sym))
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
