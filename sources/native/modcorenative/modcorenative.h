
#pragma once

#include <iostream>

#if WIN32
#include <Windows.h>
#endif

#define NETHOST_USE_AS_STATIC
#include "hostfxr.h"
#include "nethost.h"
#include "coreclr_delegates.h"

typedef void(*real_hl_global_init)(void);