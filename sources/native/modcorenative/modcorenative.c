
#include <hl.h>

typedef struct {
	void* _longjmp;
	void* _setjmp;
} unsafe_helpers_t;

EXPORT void get_unsafe_helpers(unsafe_helpers_t* helpers) {
	helpers->_longjmp = longjmp;
	helpers->_setjmp = setjmp;
}


