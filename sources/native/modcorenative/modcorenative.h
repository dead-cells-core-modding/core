
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

typedef struct {
	int count;
	const char** names;
	void** ptr;
} DCCM_API_INFOS;

#include "dccm_api.h"
