
#include "modcorenative.h"

typedef void(*csapi_PrintStackDebug_type)();

EXTERNC EXPORT csapi_PrintStackDebug_type csapi_PrintStackDebug;

EXTERNC EXPORT void dbg_PrintStack()
{
	printf("========================================Debug Stack================\n");
	csapi_PrintStackDebug();
}
