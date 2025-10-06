#define DCCM_NATIVE_LIB
#include "modcorenative.h"

#undef DCCM_MANAGED_API

#define DCCM_MANAGED_API(name, ret, ...) DCCM_EXPORT type_dccm_##name dccm_##name = 0;
#include "managed_apis.h"

DCCM_EXPORT int dccm_internal_get_exposed_api(char** nameArray, void*** fixupArray) {
	int index = 0;

#undef DCCM_MANAGED_API
#define DCCM_MANAGED_API(name, ret, ...) \
	nameArray[index] = ""#name; \
	fixupArray[index++] = (void**)&dccm_##name;
#include "managed_apis.h"
#undef DCCM_MANAGED_API
	return index;
}
