
#pragma once

#include <stdlib.h>
#include <malloc.h>
#include <string.h>

#ifdef WIN32
#include <Windows.h>
#else
#endif

#include "hl.h"
#include "hlmodule.h"

typedef void(*real_hl_global_init)(void);

#ifdef __cplusplus
#define EXTERNC extern "C"
#else
#define EXTERNC
#endif

typedef unsigned long long int64;

typedef struct
{
	void* eip;
	void* esp;
} hlu_stack_frame;

EXTERNC void* get_ebp();
EXTERNC void* get_esp();

EXTERNC void debug_break();


EXTERNC extern void* call_jit_c2hl;
EXTERNC extern void* call_jit_hl2c;

EXTERNC void init_trace();
