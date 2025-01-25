
#include "../modcorenative.h"

typedef struct {
	int fid;
	long long ipStart;
	long long ipEnd;
	long long namePtr;
	long long fullNamePtr;
} VSDebug_FuncInfo;
typedef struct {
	int jited;
	int funcCount;
	long long jitStart;
	long long jitEnd;
	long long functions;
} VSDebug_Data;

EXTERNC EXPORT VSDebug_Data mcn_vs_debug_hl_func_table = {0};

EXTERNC void vsd_init(hl_module* module) {

	int count = module->code->nfunctions;

	mcn_vs_debug_hl_func_table.jitStart = module->jit_code;
	mcn_vs_debug_hl_func_table.jitEnd = (long long)module->jit_code + module->codesize;

	VSDebug_FuncInfo* funcInfos = (VSDebug_FuncInfo*)malloc(sizeof(VSDebug_FuncInfo) * (count + 1));
	mcn_vs_debug_hl_func_table.functions = funcInfos;
	mcn_vs_debug_hl_func_table.funcCount = count;
	
	for (int i = 0; i < count; i++) {
		hl_function* f = module->code->functions + i;
		VSDebug_FuncInfo* info = funcInfos + i;
		info->fid = f->findex;
		info->ipStart = module->functions_ptrs[f->findex];
		hl_debug_infos* di = module->jit_debug + f->findex;
		//info->ipEnd = mcn_vs_debug_hl_func_table.jitStart + (
		//	di->large ? ((unsigned int*)di->offsets)[f->nops - 1] : ((unsigned short*)di->offsets)[f->nops - 1]
		//	);
		info->ipEnd = info->ipStart + 128;
		int outSize = 256;
		wchar_t* buf = (wchar_t*)malloc(sizeof(wchar_t) * outSize);
		memset(buf, 0, sizeof(wchar_t) * outSize);
		lstrcpynW(buf, hl_type_str(f->type), 256);
		info->namePtr = buf;
		info->fullNamePtr = info->namePtr;
	
	}

	mcn_vs_debug_hl_func_table.jited = 1;
}
