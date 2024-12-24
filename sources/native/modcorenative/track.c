
#include "modcorenative.h"

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
		buf[count++] = ret;
		top = ebp;
	}

	return count;
}
