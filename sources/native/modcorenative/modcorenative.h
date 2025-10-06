
#ifdef _MSC_VER
#define DCCM_EXPORT __declspec(dllexport)
#define DCCM_IMPORT __declspec(dllimport)
#else
#error TODO
#endif

#ifndef EXTERNC
#ifdef __cplusplus
#define EXTERNC extern "C"
#else
#define EXTERNC
#endif
#endif

#ifdef DCCM_NATIVE_LIB
#define DCCM_API EXTERNC DCCM_EXPORT
#else
#define DCCM_API EXTERNC DCCM_IMPORT
#endif

#ifndef DCCM_MANAGED_API
#define DCCM_MANAGED_API(name, ret, ...)  \
	typedef ret (*type_dccm_##name)(__VA_ARGS__);\
	DCCM_API extern type_dccm_##name dccm_##name;
#endif

#include "managed_apis.h"