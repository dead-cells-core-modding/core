
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace Hashlink
{
    public static unsafe partial class HashlinkNative
    {
        private const string LIBHL = "libhl";

        public static class InternalTypes
        {
            private static readonly nint hLibhl = NativeLibrary.Load(LIBHL);

            private static readonly Dictionary<Type, nint> net2hltype = [];
            private static readonly Dictionary<HL_type.TypeKind, Type> hltype2net = [];

            public static readonly HL_type* hlt_void = GetType(typeof(void));
            public static readonly HL_type* hlt_i32 = GetType(typeof(int));
            public static readonly HL_type* hlt_i64 = GetType(typeof(long));
            public static readonly HL_type* hlt_f64 = GetType(typeof(double));
            public static readonly HL_type* hlt_f32 = GetType(typeof(float));
            public static readonly HL_type* hlt_dyn = GetType(null);
            public static readonly HL_type* hlt_array = GetType(null);
            public static readonly HL_type* hlt_bytes = GetType(typeof(byte*));
            public static readonly HL_type* hlt_dynobj = GetType(null);
            public static readonly HL_type* hlt_bool = GetType(typeof(bool));
            public static readonly HL_type* hlt_abstract = GetType(null);
            private static HL_type* GetType(Type? type, [CallerMemberName] string name = "")
            {
                var ptr = (HL_type*)NativeLibrary.GetExport(hLibhl, name);
                if (type != null)
                {
                    net2hltype[type] = (nint)ptr;
                    hltype2net[ptr->kind] = type;
                }
                return ptr;
            }

            public static Type? GetFrom(HL_type.TypeKind type)
            {
                return hltype2net.TryGetValue(type, out var result) ? result : null;
            }
            public static HL_type* GetFrom(Type type)
            {
                return net2hltype.TryGetValue(type, out var result) ? (HL_type*)result : null;
            }

            static InternalTypes()
            {
                NativeLibrary.Free(hLibhl);
                hLibhl = 0;
            }
        }
        [LibraryImport(LIBHL)]
        public static partial void hl_global_init();
        [LibraryImport(LIBHL)]
        public static partial HL_thread_info* hl_get_thread();

        [LibraryImport(LIBHL)]
        public static partial HL_array* hl_alloc_array(HL_type* at, int size);

        [LibraryImport(LIBHL)]
        public static partial HL_vdynamic* hl_dyn_call_safe(HL_vclosure* c, HL_vdynamic** args, int nargs, bool* isException);

        [LibraryImport(LIBHL, StringMarshalling = StringMarshalling.Utf16)]
        public static partial string hl_to_string(HL_vdynamic* v);
        [LibraryImport(LIBHL)]
        public static partial void hl_throw(HL_vdynamic* v);


        [LibraryImport(LIBHL)]
        public static partial void hl_add_root(void* ptr);
        [LibraryImport(LIBHL)]
        public static partial void hl_remove_root(void* ptr);
        [LibraryImport(LIBHL)]
        public static partial HL_vdynobj* hl_alloc_dynobj();
        [LibraryImport(LIBHL)]
        public static partial HL_vdynamic* hl_alloc_dynamic(HL_type* t);
        [LibraryImport(LIBHL)]
        public static partial HL_vdynamic* hl_alloc_obj(HL_type* type);
        [LibraryImport(LIBHL)]
        public static partial HL_enum* hl_alloc_enum(HL_type* type);
        [LibraryImport(LIBHL)]
        public static partial HL_field_lookup* hl_lookup_find(HL_field_lookup* l, int size, int hash);

        [LibraryImport(LIBHL)]
        public static partial long hl_dyn_geti64(HL_vdynamic* d, int hfield, HL_type* t);
        [LibraryImport(LIBHL)]
        public static partial void hl_dyn_seti64(HL_vdynamic* d, int hfield, HL_type* t, long val);

        [LibraryImport(LIBHL)]
        public static partial int hl_dyn_geti(HL_vdynamic* d, int hfield, HL_type* t);
        [LibraryImport(LIBHL)]
        public static partial void hl_dyn_seti(HL_vdynamic* d, int hfield, HL_type* t, int val);

        [LibraryImport(LIBHL)]
        public static partial void* hl_dyn_getp(HL_vdynamic* d, int hfield, HL_type* t);
        [LibraryImport(LIBHL)]
        public static partial void hl_dyn_setp(HL_vdynamic* d, int hfield, HL_type* t, void* val);

        [LibraryImport(LIBHL)]
        public static partial float hl_dyn_getf(HL_vdynamic* d, int hfield, HL_type* t);
        [LibraryImport(LIBHL)]
        public static partial void hl_dyn_setf(HL_vdynamic* d, int hfield, HL_type* t, float val);

        [LibraryImport(LIBHL)]
        public static partial double hl_dyn_getd(HL_vdynamic* d, int hfield, HL_type* t);
        [LibraryImport(LIBHL)]
        public static partial void hl_dyn_setd(HL_vdynamic* d, int hfield, HL_type* t, double val);

        [LibraryImport(LIBHL)]
        public static partial char* hl_resolve_symbol(void* addr, char* @out, ref int outSize);

        [LibraryImport(LIBHL)]
        public static partial void* hl_obj_lookup(HL_vdynamic* d, int hfield, out HL_type* t);
        [LibraryImport(LIBHL)]
        public static partial HL_vdynamic* hl_obj_lookup_extra(HL_vdynamic* d, int hfield);
        [LibraryImport(LIBHL)]
        public static partial int hl_hash_gen(char* name, [MarshalAs(UnmanagedType.Bool)] bool cache_name);
        [LibraryImport(LIBHL)]
        public static partial HL_vdynamic* hl_make_dyn(void* data, HL_type* t);
        [LibraryImport(LIBHL)]
        public static partial HL_vdynamic* hl_obj_get_field(HL_vdynamic* obj, int hfield);
        [LibraryImport(LIBHL)]
        public static partial void hl_obj_set_field(HL_vdynamic* obj, int hfield, HL_vdynamic* v);
        [LibraryImport(LIBHL)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool hl_obj_has_field(HL_vdynamic* obj, int hfield);
        [LibraryImport(LIBHL)]
        public static partial char* hl_type_str(HL_type* t);
        [LibraryImport(LIBHL)] 
        public static partial void* hl_gc_alloc_gen(HL_type* t, int size, HL_Alloc_Flags flags);
        [LibraryImport(LIBHL)] 
        public static partial int hl_type_size(HL_type* t);
        [LibraryImport(LIBHL)]
        public static partial HL_vvirtual* hl_alloc_virtual(HL_type* t);
        [LibraryImport(LIBHL)]
        public static partial HL_vclosure* hl_alloc_closure_ptr(HL_type* fullt, void* fvalue, void* v);
        [LibraryImport(LIBHL)]
        public static partial void* hl_code_read(void* data, int size, byte** errorMsg);
        [LibraryImport(LIBHL)]
        public static partial void* callback_c2hl(void* f, HL_type* t, void** args, HL_vdynamic* ret);
    }
}
