
#include "modcorenative.h"


static void call_bridge_hl_to_cs(hl2c_table* table, void* retVal, void* arg0, va_list args)
{
	int64* argsBuf = (int64*) alloca(table->argsCount * sizeof(int64));

	int argIdx = 0;
	int i = 0;
	for (; argIdx < table->argsCount; i++)
	{
		if (i == 0) {
			argsBuf[argIdx++] = arg0;
			continue;
		}
#ifdef WIN32
		if (i == 4 || i == 5) { //hl2c_table* table, void* retAddr
			continue;
		}
#endif
		int64 val = va_arg(args, int64);
		argsBuf[argIdx++] = val;
	}
	void* err = NULL;
	table->callback(table, retVal, argsBuf, &err);
	if (err != NULL) {
		hl_throw(err);
	}
}

EXTERNC EXPORT void* get_asm_call_bridge_hl_to_cs()
{
	return asm_call_bridge_hl_to_cs;
}

#ifdef WIN32
EXTERNC void* c_call_bridge_hl_to_cs(void* arg0, void* arg1, void* arg2, void* arg3, hl2c_table* table, void* retAddr, ...)
{
	va_list args;
	//va_start(args, arg0); //DONT USE IT!!!!!!!!!!!! Fucking !!!!!!!!!
	args = &arg1;
	void* result;
	call_bridge_hl_to_cs(table, &result, arg0, args);
	va_end(args);
	return *(void**)&result;
}
#endif
