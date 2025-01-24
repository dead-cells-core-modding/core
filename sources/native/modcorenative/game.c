/*
 * Copyright (C)2015-2016 Haxe Foundation
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */
#include "modcorenative.h"

#ifdef HL_WIN
#	include <locale.h>
typedef uchar pchar;
#define pprintf(str,file)	uprintf(USTR(str),file)
#define pfopen(file,ext) _wfopen(file,USTR(ext))
#define pcompare wcscmp
#define ptoi(s)	wcstol(s,NULL,10)
#define PSTR(x) USTR(x)
#else
#	include <sys/stat.h>
typedef char pchar;
#define pprintf printf
#define pfopen fopen
#define pcompare strcmp
#define ptoi atoi
#define PSTR(x) x
#endif

typedef struct {
	hl_code *code;
	hl_module *m;
	vdynamic *ret;
	vclosure c;
	pchar *file;
	int file_time;
} main_context;

static int pfiletime( pchar *file )	{
#ifdef HL_WIN
	struct _stat32 st;
	_wstat32(file,&st);
	return (int)st.st_mtime;
#else
	struct stat st;
	stat(file,&st);
	return (int)st.st_mtime;
#endif
}


#ifdef HL_VCC
// this allows some runtime detection to switch to high performance mode
__declspec(dllexport) DWORD NvOptimusEnablement = 1;
__declspec(dllexport) int AmdPowerXpressRequestHighPerformance = 1;
#endif

#if defined(HL_LINUX) || defined(HL_MAC)
#include <signal.h>
static void handle_signal( int signum ) {
	signal(signum, SIG_DFL);
	printf("SIGNAL %d\n",signum);
	hl_dump_stack();
	fflush(stdout);
	raise(signum);
}
static void setup_handler() {
	struct sigaction act;
	act.sa_sigaction = NULL;
	act.sa_handler = handle_signal;
	act.sa_flags = 0;
	sigemptyset(&act.sa_mask);
	sigaction(SIGSEGV,&act,NULL);
	sigaction(SIGTERM,&act,NULL);
}
#else
static void setup_handler() {
}
#endif

EXTERNC EXPORT void(*csapi_LogPrint)(const char* source, int level, const char* msg);
EXTERNC EXPORT void(*csapi_OnHLEvent)(int eventId, void* data);

void log_printf_handler(const char* source, int level, const char* format, va_list args) {
	char* buf = malloc(4096);
	vsprintf(buf, format, args);
	csapi_LogPrint(source, level, buf);
	free(buf);
}

EXTERNC EXPORT int hlu_start_game(hl_code* code) {
	char *error_msg = NULL;
	bool isExc = false;
	main_context ctx;

	hl_event_set_handler(csapi_OnHLEvent);
	hl_log_set_handler(log_printf_handler);
	hl_sys_init((void**)"", 0, "hlboot.dat");
	hl_register_thread(&ctx);
	ctx.file = NULL;
	ctx.code = code;
	if( ctx.code == NULL ) {
		if( error_msg ) printf("%s\n", error_msg);
		return 1;
	}
	ctx.m = hl_module_alloc(ctx.code);
	if( ctx.m == NULL )
		return 2;
	if( !hl_module_init(ctx.m,FALSE) )
		return 3;

	hl_event(HL_EV_VM_READY, &ctx);

	hl_code_free(ctx.code);
	ctx.c.t = ctx.code->functions[ctx.m->functions_indexes[ctx.m->code->entrypoint]].type;
	ctx.c.fun = ctx.m->functions_ptrs[ctx.m->code->entrypoint];
	ctx.c.hasValue = 0;
	setup_handler();
	/*ctx.ret = hl_dyn_call_safe(&ctx.c, NULL, 0, &isExc);
	if( isExc ) {
		varray *a = hl_exception_stack();
		int i;
		uprintf(USTR("Uncaught exception: %s\n"), hl_to_string(ctx.ret));
		for(i=0;i<a->size;i++)
			uprintf(USTR("Called from %s\n"), hl_aptr(a,uchar*)[i]);
		hl_global_free();
		return 1;
	}
	hl_module_free(ctx.m);*/
	hl_event(HL_EV_START_GAME, &ctx);
	hl_free(&ctx.code->alloc);
	hl_global_free();
	return 0;
}

