
#include "modcorenative.h"
#include <cassert>

#if _MSC_VER & _M_IX86
extern "C" EXPORT int modcore_x86_load_stacktrace(void** buf, int maxCount, void* bottom)
{
	void* top = 0;
	__asm
	{
		mov top, ebp
	}

	if (top == NULL)
	{
		return 0;
	}

	int count = 0;
	
	while (count < maxCount && top <= bottom)
	{
		void* ebp = ((void**)top)[0];
		void* ret = ((void**)top)[1];
		buf[count++] = ret;
		top = ebp;
	}

	return count;
}
#endif