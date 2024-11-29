
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

//From https://github.com/motion-twin/hashlink/blob/master/src/hl.h
namespace Hashlink
{

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct HL_type
    {
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
            // ---------
            HLAST = 21,
            _H_FORCE_INT = 0x7FFFFFFF
        }

        public TypeKind kind;

        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct TypeData
        {
            [FieldOffset(0)]
            public char* abs_name;

        }

        public TypeData data;
        public void** vobj_proto;
        public uint* mark_bits;
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
        public nint hasValue;
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
        public HL_type* type;
        public HL_type** regs;
        public HL_opcode* ops;
        public int* debug;
        public void* obj;
        public char* field;
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
        public fixed byte exc_stack_trace[1]; //As void* [0x100]
    }
    [StructLayout (LayoutKind.Sequential)]
    public unsafe struct HL_array
    {
        public HL_type* t;
        public HL_type* at;
        public int size;
        public int __pad; // force align on 16 bytes for double
    }
}
