
#include "modcorenative.h"

EXTERNC EXPORT int mcn_memory_readable(void* ptr, int len) {
#if WIN32
	return !IsBadReadPtr(ptr, len);
#else

#endif
}
