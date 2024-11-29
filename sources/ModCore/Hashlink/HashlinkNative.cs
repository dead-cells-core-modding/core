using SharpDisasm.Udis86;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace ModCore.Hashlink
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
        public static partial HL_thread_info* hl_get_thread();

        [LibraryImport("libhl")]
        public static partial HL_array* hl_alloc_array(HL_type* at, int size);
    }
}
