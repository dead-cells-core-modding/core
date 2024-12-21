
#pragma once


#if WIN32
#include <Windows.h>
#else
#endif

#define NETHOST_USE_AS_STATIC
#include "hostfxr.h"
#include "nethost.h"
#include "coreclr_delegates.h"
#include "hl.h"
#include "hlmodule.h"

typedef void(*real_hl_global_init)(void);

#ifdef __cplusplus
#define EXTERNC extern "C"
#else
#define EXTERNC
#endif

EXTERNC void* get_ebp();
