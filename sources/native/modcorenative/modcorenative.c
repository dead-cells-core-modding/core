
#include <hlsystem.h>
#include <hl.h>

typedef struct {
	void* _longjmp;
	void* _setjmp;
} unsafe_helpers_t;

EXPORT void get_unsafe_helpers(unsafe_helpers_t* helpers) {
	helpers->_longjmp = longjmp;
	helpers->_setjmp = setjmp;
}

#ifdef WIN32

static LPWSTR createdumpCommand;

void GenerateMiniDump() {
	if (createdumpCommand != NULL) {

		PROCESS_INFORMATION processInfo = {};
		STARTUPINFOA startupInfo = {};

		if (CreateProcessW(
			NULL,
			createdumpCommand,
			NULL,
			NULL,
			FALSE,
			0,
			NULL,
			NULL,
			&startupInfo,
			&processInfo
		)) {

			WaitForSingleObject(processInfo.dwProcessId, -1);

			CloseHandle(processInfo.hThread);
			CloseHandle(processInfo.hProcess);
		}
	}
}

LONG PvectoredExceptionHandler(
	EXCEPTION_POINTERS* ExceptionInfo
) {
	auto err = ExceptionInfo->ExceptionRecord->ExceptionCode;

	if (err == EXCEPTION_ARRAY_BOUNDS_EXCEEDED ||
		err == EXCEPTION_ILLEGAL_INSTRUCTION) {
		fprintf_s(stderr, "[DCCMDBG][VEH] %x %p\n", err, ExceptionInfo->ExceptionRecord->ExceptionAddress);
		GenerateMiniDump();
	}
	else if (err == EXCEPTION_ACCESS_VIOLATION) {
		fprintf_s(stderr, "[DCCMDBG][VEH][AC] %p %p\n", ExceptionInfo->ExceptionRecord->ExceptionAddress,
			(void*)ExceptionInfo->ExceptionRecord->ExceptionInformation[1]);
		GenerateMiniDump();
	}

	return 1;
}

EXPORT void init_veh(LPWSTR createdumpCmd) {
	createdumpCommand = createdumpCmd;

	AddVectoredExceptionHandler(1, PvectoredExceptionHandler);
}

#endif
