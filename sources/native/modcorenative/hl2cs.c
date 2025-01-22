
#include "modcorenative.h"


static void call_bridge_hl_to_cs(hl2c_table* table, void* retVal, va_list args)
{
	hl_value_fat* argsBuf = (hl_value_fat*) alloca(table->argsCount * sizeof(hl_value_fat));
	int smark = table->argSizeMarks;
	int fmark = table->argFloatMarks;
	for (int i = 0; i < table->argsCount; i++)
	{
		if ((fmark & 1) == 1)
		{
			if ((smark & 1) == 1)
			{
				//8 bytes
				argsBuf[i] = va_arg(args, hl_value_fat);
			}
			else
			{
				//4 bytes
				argsBuf[i] = va_arg(args, unsigned int);
			}
		}
		else
		{
			if ((smark & 1) == 1)
			{
				//8 bytes
				((double*)argsBuf)[i] = va_arg(args, double);
			}
			else
			{
				//4 bytes
				((float*)argsBuf)[i] = va_arg(args, float);
			}
		}
		smark >>= 1;
		fmark >>= 1;
	}
	table->callback(table, retVal, argsBuf);
}

EXTERNC EXPORT void* get_asm_call_bridge_hl_to_cs()
{
	return asm_call_bridge_hl_to_cs;
}

EXTERNC void* c_call_bridge_hl_to_cs(hl2c_table* table, void* pad, ...)
{
	va_list args;
	va_start(args, pad);
	void* result;
	call_bridge_hl_to_cs(table, &result, args);
	va_end(args);
	return *(void**)&result;
}


EXTERNC double c_call_bridge_hl_to_cs2(hl2c_table* table, void* pad, ...)
{
	va_list args;
	va_start(args, pad);
	double result;
	call_bridge_hl_to_cs(table, &result, args);
	va_end(args);
	return result;
}

#ifdef X86
EXTERNC hl_value_fat c_call_bridge_hl_to_cs_fat(hl2c_table* table, void* pad, ...)
{
	va_list args;
	va_start(args, pad);
	hl_value_fat result;
	call_bridge_hl_to_cs(table, &result, args);
	va_end(args);
	return result;
}
#endif

