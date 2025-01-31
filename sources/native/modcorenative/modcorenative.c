
#include "modcorenative.h"

#ifdef WIN32

static int mcn_memory_readable_throw_handler(int code) {
	if (code == EXCEPTION_ACCESS_VIOLATION)
	{
		return EXCEPTION_EXECUTE_HANDLER;
	}
	return EXCEPTION_CONTINUE_SEARCH;
}
EXTERNC EXPORT int mcn_memory_readable(volatile int* ptr) {
#if DEBUG
	__try
	{
		volatile int old = *ptr;
		*ptr = old;
		return 1;
	}
	__except (mcn_memory_readable_throw_handler(GetExceptionCode()))
	{
		return 0;
	}
#else
	return !IsBadReadPtr(ptr, 4);
#endif
}
#else
EXTERNC EXPORT int mcn_memory_readable(void* ptr, int len) {
	//TODO
}
#endif