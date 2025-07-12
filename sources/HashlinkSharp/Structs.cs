using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace Hashlink
{
    [Flags]
    public enum HL_GC_Flags
    {
        GC_PROFILE = 1,
        GC_DUMP_MEM = 2,
        GC_NO_THREADS = 4,
        GC_FORCE_MAJOR = 8,
        GC_PROFILE_MEM = 16
    }
    [Flags]
    public enum HL_Alloc_Flags
    {
        MEM_KIND_DYNAMIC = 0,
        MEM_KIND_RAW = 1,
        MEM_KIND_NOPTR = 2,
        MEM_KIND_FINALIZER = 3,
        MEM_ALIGN_DOUBLE = 128,
        MEM_ZERO = 256
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HLEV_native_resolve_event
    {
        public byte* libName;
        public byte* functionName;
        public void* result;
    }
    public enum TypeKind
    {
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
        // ---------
        HLAST = 23,
        _H_FORCE_INT = 0x7FFFFFFF
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_type
    {
        public string TypeName
        {
            get
            {
                fixed (HL_type* ptr = &this)
                {
                    return new string(hl_type_str(ptr));
                }
            }
        }
        
        public TypeKind kind;

        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct TypeData
        {
            [FieldOffset(0)]
            public char* abs_name;
            [FieldOffset(0)]
            public HL_type_func* func;
            [FieldOffset(0)]
            public HL_type_obj* obj;
            [FieldOffset(0)]
            public HL_type_enum* tenum;
            [FieldOffset(0)]
            public HL_type_virtual* virt;
            [FieldOffset(0)]
            public HL_type* tparam;
        }

        public TypeData data;
        public void** vobj_proto;
        public int* mark_bits;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_vvirtual
    {
        public HL_type* t;
        public HL_vdynamic* value;
        public HL_vvirtual* next;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_type_virtual
    {
        public HL_obj_field* fields;
        public int nfields;
        // runtime
        public int dataSize;
        public int* indexes;
        public HL_field_lookup* lookup;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_vdynobj
    {
        public HL_type* t;
        public HL_field_lookup* lookup;
        public char* raw_data;
        public void** values;
        public int nfields;
        public int raw_size;
        public int nvalues;
        public HL_vvirtual* virtuals;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_enum
    {
        public HL_type* t;
        public int index;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_enum_construct
    {
        public char* name;
        public int nparams;
        public HL_type** @params;
        public int size;
        public bool hasptr;
        public int* offsets;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_type_enum
    {
        public char* name;
        public int nconstructs;
        public HL_enum_construct* constructs;
        public void** global_value;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_obj_field
    {
        public char* name;
        public HL_type* t;
        public int hashed_name;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_obj_proto
    {
        public char* name;
        public int findex;
        public int pindex;
        public int hashed_name;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_module_context
    {
        public HL_alloc_block* alloc;
        public void** functions_ptrs;
        public HL_type** functions_types;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_field_lookup
    {
        public HL_type* t;
        public int hashed_name;
        public int field_index; // negative or zero : index in methods
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_runtime_binding
    {
        public void* ptr;
        public HL_type* closure;
        public int fid;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_runtime_obj
    {
        public HL_type* t;
        // absolute
        public int nfields;
        public int nproto;
        public int size;
        public int nmethods;
        public int nbindings;
        public bool hasPtr;
        public void** methods;
        public int* fields_indexes;
        public HL_runtime_binding* bindings;
        public HL_runtime_obj* parent;

        public delegate* unmanaged[Cdecl]< HL_vdynamic*, char* > toStringFun;
        public delegate* unmanaged[Cdecl]< HL_vdynamic*, HL_vdynamic*, int > compareFun;
        public delegate* unmanaged[Cdecl]< HL_vdynamic*, HL_type*, HL_vdynamic* > castFun;
        public delegate* unmanaged[Cdecl]< HL_vdynamic*, int, HL_vdynamic* > getFieldFun;

        // relative
        public int nlookup;
        public HL_field_lookup* lookup;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_type_obj
    {
        public int nfields;
        public int nproto;
        public int nbindings;
        public char* name;
        public HL_type* super;
        public HL_obj_field* fields;
        public HL_obj_proto* proto;
        public int* bindings;
        public void** global_value;
        public HL_module_context* m;
        public HL_runtime_obj* rt;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_type_func
    {
        public HL_type** args;
        public HL_type* ret;
        public int nargs;
        // storage for closure
        public HL_type* parent;
        [StructLayout(LayoutKind.Sequential)]
        public struct ClosureType
        {
            public TypeKind kind;
            public void* p;
        }
        public ClosureType closure_type;
        [StructLayout(LayoutKind.Sequential)]
        public struct Closure
        {
            public HL_type** args;
            public HL_type* ret;
            public int nargs;
            public HL_type* parent;
        }
        public Closure closure;
    }
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct HL_vdynamic
    {
        [FieldOffset(0)]
        public HL_type* type;

        [StructLayout(LayoutKind.Explicit)]
        public struct Value
        {
            [FieldOffset(0)]
            public bool @bool;
            [FieldOffset(0)]
            public byte ui8;
            [FieldOffset(0)]
            public ushort ui16;
            [FieldOffset(0)]
            public int @int;
            [FieldOffset(0)]
            public float @float;
            [FieldOffset(0)]
            public double @double;
            [FieldOffset(0)]
            public byte* bytes;
            [FieldOffset(0)]
            public void* ptr;
            [FieldOffset(0)]
            public long i64;
        }

        [FieldOffset(8)]
        public Value val;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_vstring
    {
        public HL_type* type;
        public char* bytes;
        public int length;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_vclosure
    {
        public HL_type* type;
        public void* fun;
        public int hasValue;
        public int __padding;
        public void* value;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_debug_infos
    {
        public void* offsets;
        public int start;
        public bool large;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_module
    {
        public HL_code* code;
        public int codesize;
        public int* globals_indexes;
        public byte* globals_data;
        public void** functions_ptrs;
        public int* functions_indexes;
        public int* functions_signs;
        public int* functions_hashs;
        public void* jit_code;
        public HL_debug_infos* jit_debug;

        [StructLayout(LayoutKind.Sequential)]
        public struct Context
        {
            public HL_alloc_block* alloc;
            public void** functions_ptrs;
            public HL_type** functions_types;
        }

        public Context ctx;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_native
    {
        public byte* lib;
        public byte* name;
        public HL_type* type;
        public int findex;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_function
    {
        public int findex;
        public int nregs;
        public int nops;
        public int @ref;
        public HL_type* type;
        public HL_type** regs;
        public HL_opcode* ops;
        public int* debug;
        public HL_type_obj* obj;

        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct FieldAndRef
        {
            [FieldOffset(0)]
            public char* field;
            [FieldOffset(0)]
            public HL_function* @ref;
        }

        public FieldAndRef field;
        //public HL_function* @refPtr;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_constant
    {
        public int global;
        public int nfields;
        public int* fields;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_alloc_block
    {
        public int size;
        public HL_alloc_block* next;
        public byte* p;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_code
    {
        public int version;
        public int nints;
        public int nfloats;
        public int nstrings;
        public int nbytes;
        public int ntypes;
        public int nglobals;
        public int nnatives;
        public int nfunctions;
        public int nconstants;
        public int entrypoint;
        public int ndebugfiles;
        public bool hasdebug;
        public int* ints;
        public double* floats;
        public byte** strings;
        public int* strings_lens;
        public byte* bytes;
        public int* bytes_pos;
        public byte** debugfiles;
        public int* debugfiles_lens;
        public char** ustrings;
        public HL_type* types;
        public HL_type** globals;
        public HL_native* natives;
        public HL_function* functions;
        public HL_constant* constants;
        public HL_alloc_block* alloc;
        public HL_alloc_block* falloc;
    }

    [StructLayout(LayoutKind.Explicit, Size = 256)]
    public struct C_jmpbuf
    {

    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_trap_ctx
    {
        public C_jmpbuf buf;
        public HL_trap_ctx* prev;
        public HL_vdynamic* tcheck;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_thread_info
    {
        public int thread_id;
        // gc vars
        public volatile int gc_blocking;
        public void* stack_top;
        public void* stack_cur;
        // exception handling
        public HL_trap_ctx* trap_current;
        public HL_trap_ctx* trap_uncaught;
        public HL_vclosure* exc_handler;
        public HL_vdynamic* exc_value;
        public int exc_flags;
        public int exc_stack_count;
        // extra
        public C_jmpbuf gc_regs;
        [InlineArray(0x100)]
        public struct EXC_STACK_ARRAY
        {
            public nint ptr;
        }
        public EXC_STACK_ARRAY exc_stack_trace; //As void* [0x100]
        public EXC_STACK_ARRAY exc_stack_ptrs;
    }
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_array
    {
        public HL_type* t;
        public HL_type* at;
        public int size;
        public int __pad; // force align on 16 bytes for double
    }
}
