

#	ifdef _WIN32
#		define EXPORT __declspec( dllexport )
#	elif defined(__GNUC__) || defined(__clang__)
#		define EXPORT __attribute__((visibility("default")))
#	else
#		define EXPORT
#	endif

#define LIBTCCAPI EXPORT

#include "../3rd/tinycc/libtcc.c"
