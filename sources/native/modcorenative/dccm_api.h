
#ifndef DCCM_API_MANAGED_API_DEF_ONLY


//
typedef struct
{
	int hasValue;
	int size;
	char data[0]; //Note: This may contain managed pointers.
} *PDCCM_EVENT_RESULT;

typedef void** PEVENT_DATA; //Note: This may contain managed pointers.

typedef void (*event_reveiver_handler)(const char* evName, PEVENT_DATA pArg, PDCCM_EVENT_RESULT pResult);



struct tagDCCM_MAPIS
{
#define DCCM_API_MANAGED_API_DEF_ONLY
#define DCCM_MANAGED_API(name, ret, ...) ret (*name)(__VA_ARGS__)
#include "dccm_api.h"
#undef DCCM_API_MANAGED_API_DEF_ONLY
};



typedef struct tagDCCM_MAPIS DCCM_MAPIS;
typedef struct tagDCCM_MAPIS* PDCCM_MAPIS;

typedef PDCCM_MAPIS(*try_load_dccm_fn)(char_t* gamePath, const void** args, int argc, const char** err);


#else //

#ifndef DCCM_MANAGED_API
#define DCCM_MANAGED_API(name, ret, ...) 
#endif


DCCM_MANAGED_API(add_event_receiver, void, event_reveiver_handler handler);
DCCM_MANAGED_API(remove_event_receiver, void, event_reveiver_handler handler);

DCCM_MANAGED_API(print, void, const char* log);

#endif
