
#define DCCM_API_MANAGED_API_DEF_ONLY
#include "dccm_api.h"
#undef DCCM_API_MANAGED_API_DEF_ONLY

#ifndef TEST_MANAGED_API_DEF 
#define TEST_MANAGED_API_DEF DCCM_MANAGED_API(send_event, void, int a);
#endif

