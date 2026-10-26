

#include <hlsystem.h>
#include <hl.h>
#include <hlmodule.h>

EXPORT void* ptr_setjmp = setjmp;

EXPORT void modcorenative_unregister_hl_thread(hl_thread_info* t) {
	hl_remove_root(&t->exc_value);
	hl_remove_root(&t->exc_handler);

	hl_threads_info* gc_threads = hl_gc_threads_info();

	hl_mutex_acquire(gc_threads->global_lock);
	for (int i = 0; i < gc_threads->count; i++)
		if (gc_threads->threads[i] == t) {
			memmove(gc_threads->threads + i, gc_threads->threads + i + 1, sizeof(void*) * (gc_threads->count - i - 1));
			gc_threads->count--;
			break;
		}
	free(t);
	// don't use gc_global_lock(false)
	hl_mutex_release(gc_threads->global_lock);
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
