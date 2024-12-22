
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace Hashlink
{
    public static unsafe partial class HashlinkNative
    {
        public static class InternalTypes
        {
            private static readonly nint hLibhl = NativeLibrary.Load("libhl");
            public static readonly HL_type* hlt_void = GetType();
            public static readonly HL_type* hlt_i32 = GetType();
            public static readonly HL_type* hlt_i64 = GetType();
            public static readonly HL_type* hlt_f64 = GetType();
            public static readonly HL_type* hlt_f32 = GetType();
            public static readonly HL_type* hlt_dyn = GetType();
            public static readonly HL_type* hlt_array = GetType();
            public static readonly HL_type* hlt_bytes = GetType();
            public static readonly HL_type* hlt_dynobj = GetType();
            public static readonly HL_type* hlt_bool = GetType();
            public static readonly HL_type* hlt_abstract = GetType();
            private static HL_type* GetType([CallerMemberName] string name = "")
            {
                return (HL_type*)NativeLibrary.GetExport(hLibhl, name);
            }

            static InternalTypes()
            {
                NativeLibrary.Free(hLibhl);
                hLibhl = 0;
            }
        }
        [LibraryImport("libhl")]
        public static partial void hl_global_init();
        [LibraryImport("libhl")]
        public static partial HL_thread_info* hl_get_thread();

        [LibraryImport("libhl")]
        public static partial HL_array* hl_alloc_array(HL_type* at, int size);

        [LibraryImport("libhl")]
        public static partial HL_vdynamic* hl_dyn_call_safe(HL_vclosure* c, HL_vdynamic** args, int nargs, bool* isException);

        [LibraryImport("libhl", StringMarshalling = StringMarshalling.Utf16)]
        public static partial string hl_to_string(HL_vdynamic* v);
        [LibraryImport("libhl")]
        public static partial void hl_throw(HL_vdynamic* v);
        [LibraryImport("libhl", StringMarshalling = StringMarshalling.Utf16)]
        public static partial string hl_type_str(HL_type* type);

        [LibraryImport("libhl")]
        public static partial void hl_add_root(void* ptr);
        [LibraryImport("libhl")]
        public static partial void hl_remove_root(void* ptr);
        [LibraryImport("libhl")]
        public static partial HL_vdynamic* hl_alloc_dynamic(HL_type* t);
        [LibraryImport("libhl")]
        public static partial HL_vdynamic* hl_alloc_obj(HL_type* type);
        [LibraryImport("libhl")]
        public static partial HL_enum* hl_alloc_enum(HL_type* type);
        [LibraryImport("libhl")]
        public static partial HL_field_lookup* hl_lookup_find(HL_field_lookup* l, int size, int hash);

        [LibraryImport("libhl")]
        public static partial long hl_dyn_geti64(HL_vdynamic* d, int hfield, HL_type* t);
        [LibraryImport("libhl")]
        public static partial void hl_dyn_seti64(HL_vdynamic* d, int hfield, HL_type* t, long val);

        [LibraryImport("libhl")]
        public static partial int hl_dyn_geti(HL_vdynamic* d, int hfield, HL_type* t);
        [LibraryImport("libhl")]
        public static partial void hl_dyn_seti(HL_vdynamic* d, int hfield, HL_type* t, int val);

        [LibraryImport("libhl")]
        public static partial void* hl_dyn_getp(HL_vdynamic* d, int hfield, HL_type* t);
        [LibraryImport("libhl")]
        public static partial void hl_dyn_setp(HL_vdynamic* d, int hfield, HL_type* t, void* val);

        [LibraryImport("libhl")]
        public static partial float hl_dyn_getf(HL_vdynamic* d, int hfield, HL_type* t);
        [LibraryImport("libhl")]
        public static partial void hl_dyn_setf(HL_vdynamic* d, int hfield, HL_type* t, float val);

        [LibraryImport("libhl")]
        public static partial double hl_dyn_getd(HL_vdynamic* d, int hfield, HL_type* t);
        [LibraryImport("libhl")]
        public static partial void hl_dyn_setd(HL_vdynamic* d, int hfield, HL_type* t, double val);

        [LibraryImport("libhl")]
        public static partial char* hl_resolve_symbol(void* addr, char* @out, out int outSize);

        [LibraryImport("libhl")]
        public static partial void* hl_obj_lookup(HL_vdynamic* d, int hfield, out HL_type* t);
        [LibraryImport("libhl")]
        public static partial HL_vdynamic* hl_obj_lookup_extra(HL_vdynamic* d, int hfield);
    }
}
