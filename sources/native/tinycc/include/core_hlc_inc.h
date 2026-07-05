
#ifndef HLC_INC_INCLUDE
#define HLC_INC_INCLUDE

#define EXPORT

#undef stdin
#undef stdout
#undef stderr
#undef EOF
#undef TRUE
#undef FALSE
#undef __SIGN
#undef INT_MAX
#undef INT_MIN
#undef INT16_MAX
#undef BIG_ENDIAN
#undef LITTLE_ENDIAN
#undef OVERFLOW
#undef UNDERFLOW
#undef DOMAIN
#undef __valid


#define hl_invalid_comparison 0xAABBCCDD

#define HL_EXC_MAX_STACK	0x100
#define HL_EXC_RETHROW		1
#define HL_EXC_CATCH_ALL	2
#define HL_EXC_IS_THROW		4
#define HL_THREAD_INVISIBLE	16
#define HL_THREAD_PROFILER_PAUSED 32
#define HL_EXC_KILL			64
#define HL_TREAD_TRACK_SHIFT 16

#define HL_TRACK_ALLOC		1
#define HL_TRACK_CAST		2
#define HL_TRACK_DYNFIELD	4
#define HL_TRACK_DYNCALL	8
#define HL_TRACK_MASK		(HL_TRACK_ALLOC | HL_TRACK_CAST | HL_TRACK_DYNFIELD | HL_TRACK_DYNCALL)

#define HL_MAX_EXTRA_STACK 64

#define HL_API 

#define NULL ((void*)0)

#define HL__ENUM_CONSTRUCT__	hl_type *t; int index;
#define HL__ENUM_INDEX__(v)		((venum*)(v))->index

#define hl_trap(ctx,r,label) { hl_thread_info *__tinf = hl_get_thread(); ctx.tcheck = NULL; ctx.prev = __tinf->trap_current; __tinf->trap_current = &ctx; if( hlc_setjmp(ctx.buf) ) { r = __tinf->exc_value; goto label; } }
#define hl_endtrap(ctx)	hl_get_thread()->trap_current = ctx.prev
#define hl_aptr(a,t)	((t*)(((varray*)(a))+1))

#define USTR(str) L##str

#define true 1
#define false 0

typedef unsigned short uchar;
typedef char bool;
typedef long long int64;
typedef uchar pchar;

typedef struct _hl_mutex hl_mutex;
typedef struct _hl_trap_ctx hl_trap_ctx;
typedef struct _hl_module_ctx hl_module_ctx;

#ifdef _WIN32
typedef char jmp_buf[256];
#else
typedef char jmp_buf[200];
#endif




typedef enum {
	HVOID = 0,
	HUI8 = 1,
	HUI16 = 2,
	HI32 = 3,
	HI64 = 4,
	HF32 = 5,
	HF64 = 6,
	HBOOL = 7,
	HBYTES = 8,
	HDYN = 9,
	HFUN = 10,
	HOBJ = 11,
	HARRAY = 12,
	HTYPE = 13,
	HREF = 14,
	HVIRTUAL = 15,
	HDYNOBJ = 16,
	HABSTRACT = 17,
	HENUM = 18,
	HNULL = 19,
	HMETHOD = 20,
	HSTRUCT = 21,
	HPACKED = 22,
	HGUID = 23,
	// ---------
	HLAST = 24,
	_H_FORCE_INT = 0x7FFFFFFF
} hl_type_kind;

typedef struct hl_type hl_type;
typedef struct hl_runtime_obj hl_runtime_obj;
typedef struct hl_alloc_block hl_alloc_block;
typedef struct { hl_alloc_block* cur; } hl_alloc;
typedef struct _hl_field_lookup hl_field_lookup;

typedef struct {
	hl_alloc alloc;
	void** functions_ptrs;
	hl_type** functions_types;
} hl_module_context;

typedef struct {
	hl_type** args;
	hl_type* ret;
	int nargs;
	// storage for closure
	hl_type* parent;
	struct {
		hl_type_kind kind;
		void* p;
	} closure_type;
	struct {
		hl_type** args;
		hl_type* ret;
		int nargs;
		hl_type* parent;
	} closure;
} hl_type_fun;

typedef struct {
	const uchar* name;
	hl_type* t;
	int hashed_name;
} hl_obj_field;

typedef struct {
	const uchar* name;
	int findex;
	int pindex;
	int hashed_name;
} hl_obj_proto;

typedef struct {
	int nfields;
	int nproto;
	int nbindings;
	const uchar* name;
	hl_type* super;
	hl_obj_field* fields;
	hl_obj_proto* proto;
	int* bindings;
	void** global_value;
	hl_module_context* m;
	hl_runtime_obj* rt;
} hl_type_obj;

typedef struct {
	hl_obj_field* fields;
	int nfields;
	// runtime
	int dataSize;
	int* indexes;
	hl_field_lookup* lookup;
} hl_type_virtual;

typedef struct {
	const uchar* name;
	int nparams;
	hl_type** params;
	int size;
	bool hasptr;
	int* offsets;
} hl_enum_construct;

typedef struct {
	const uchar* name;
	int nconstructs;
	hl_enum_construct* constructs;
	void** global_value;
} hl_type_enum;

struct hl_type {
	hl_type_kind kind;
	union {
		const uchar* abs_name;
		hl_type_fun* fun;
		hl_type_obj* obj;
		hl_type_enum* tenum;
		hl_type_virtual* virt;
		hl_type* tparam;
	};
	void** vobj_proto;
	unsigned int* mark_bits;
};


typedef unsigned char vbyte;

typedef struct {
	hl_type* t;
#	ifndef HL_64
	int __pad; // force align on 16 bytes for double
#	endif
	union {
		bool b;
		unsigned char ui8;
		unsigned short ui16;
		int i;
		float f;
		double d;
		vbyte* bytes;
		void* ptr;
		int64 i64;
	} v;
} vdynamic;

typedef struct {
	hl_type* t;
	/* fields data */
} vobj;

typedef struct _vvirtual vvirtual;
struct _vvirtual {
	hl_type* t;
	vdynamic* value;
	vvirtual* next;
};

#define hl_vfields(v) ((void**)(((vvirtual*)(v))+1))

typedef struct {
	hl_type* t;
	hl_type* at;
	int size;
	int __pad; // force align on 16 bytes for double
} varray;

typedef struct _vclosure {
	hl_type* t;
	void* fun;
	int hasValue;
#	ifdef HL_64
	int stackCount;
#	endif
	void* value;
} vclosure;

typedef struct {
	vclosure cl;
	vclosure* wrappedFun;
} vclosure_wrapper;

struct _hl_field_lookup {
	hl_type* t;
	int hashed_name;
	int field_index; // negative or zero : index in methods
};

typedef struct {
	void* ptr;
	hl_type* closure;
	int fid;
} hl_runtime_binding;

struct hl_runtime_obj {
	hl_type* t;
	// absolute
	int nfields;
	int nproto;
	int size;
	int nmethods;
	int nbindings;
	unsigned char pad_size;
	unsigned char largest_field;
	bool hasPtr;
	void** methods;
	int* fields_indexes;
	hl_runtime_binding* bindings;
	hl_runtime_obj* parent;
	const uchar* (*toStringFun)(vdynamic* obj);
	int (*compareFun)(vdynamic* a, vdynamic* b);
	vdynamic* (*castFun)(vdynamic* obj, hl_type* t);
	vdynamic* (*getFieldFun)(vdynamic* obj, int hfield);
	// relative
	int nlookup;
	int ninterfaces;
	hl_field_lookup* lookup;
	int* interfaces;
};

typedef struct {
	hl_type* t;
	hl_field_lookup* lookup;
	char* raw_data;
	void** values;
	int nfields;
	int raw_size;
	int nvalues;
	vvirtual* virtuals;
} vdynobj;

#define HL_DYNOBJ_INDEX_SHIFT 17
#define HL_DYNOBJ_INDEX_MASK ((1 << HL_DYNOBJ_INDEX_SHIFT) - 1)

typedef struct _venum {
	hl_type* t;
	int index;
} venum;

struct _hl_trap_ctx {
	jmp_buf buf;
	hl_trap_ctx* prev;
	vdynamic* tcheck;
};

typedef struct {
	int thread_id;
	// gc vars
	volatile int gc_blocking;
	void* stack_top;
	void* stack_cur;
	// exception handling
	hl_trap_ctx* trap_current;
	hl_trap_ctx* trap_uncaught;
	vclosure* exc_handler;
	vdynamic* exc_value;
	int flags;
	int exc_stack_count;
	// extra
	char thread_name[128];
	jmp_buf gc_regs;
	void* exc_stack_trace[HL_EXC_MAX_STACK];
	void* extra_stack_data[HL_MAX_EXTRA_STACK];
	int extra_stack_size;
#ifdef HL_MAC
	thread_t mach_thread_id;
	pthread_t pthread_id;
#endif
} hl_thread_info;


typedef void* (*hl_resolve_native_library_func)(const char* lib, const char* entry);
EXPORT hl_resolve_native_library_func hl_resolve_native_library = NULL;


typedef hl_thread_info* (*hl_get_thread_func)();
EXPORT hl_get_thread_func hl_get_thread = NULL;

typedef int (*hlc_setjmp_func)(void* data);
EXPORT int (*hlc_setjmp)(void* data) = NULL;

hl_type hlt_array = { HARRAY };
hl_type hlt_bytes = { HBYTES };
hl_type hlt_dynobj = { HDYNOBJ };
hl_type hlt_dyn = { HDYN };
hl_type hlt_i32 = { HI32 };
hl_type hlt_i64 = { HI64 };
hl_type hlt_f32 = { HF32 };
hl_type hlt_f64 = { HF64 };
hl_type hlt_void = { HVOID };
hl_type hlt_bool = { HBOOL };
hl_type hlt_abstract = { HABSTRACT, {USTR("<abstract>")} };

extern hl_type* hl_instance_types[];
extern void* hl_functions_ptrs[];
extern hl_type* hl_functions_types[];

EXPORT void hlc_init_types(hl_module_context* ctx);
EXPORT void hlc_init_hashes();
EXPORT void hlc_init_roots();

#endif