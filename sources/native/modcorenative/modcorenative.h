﻿
#pragma once

#include <stdlib.h>
#include <malloc.h>
#include <string.h>

#ifdef WIN32
#include <Windows.h>
#else
#include <unistd.h>
#include <dlfcn.h>
#endif

#include "hl.h"
#include "hlmodule.h"

typedef void(*real_hl_global_init)(void);

#ifdef __cplusplus
#define EXTERNC extern "C"
#else
#define EXTERNC
#endif

#ifdef HL_WIN
typedef unsigned long long int64;
#endif

typedef struct
{
	void* eip;
	void* esp;
} hlu_stack_frame;


EXTERNC extern void* call_jit_c2hl;
EXTERNC extern void* call_jit_hl2c;

EXTERNC void init_trace();

EXTERNC void asm_prepare_exception_handle();
EXTERNC void asm_return_from_exception(void* ptr);
