using Hashlink.Proxy.Objects;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hashlink.Marshaling
{
    internal static unsafe class NETExcepetionError
    {
        public static HL_type* ErrorType
        {
            get;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static char* ExceptionToString( HL_vdynamic* vdy )
        {
            var ex = HashlinkMarshal.ConvertHashlinkObject<HashlinkNETExceptionObj>(vdy);
            var str = ex.ToString()!;

            var result = (char*)hl_gc_alloc_gen(InternalTypes.hlt_bytes, (str.Length * 2) + 2,
                HL_Alloc_Flags.MEM_KIND_NOPTR | HL_Alloc_Flags.MEM_ZERO);
            fixed (char* src = str)
            {
                Buffer.MemoryCopy(src, result, (str.Length * 2) + 2, (str.Length * 2) + 2);
            }
            return result;
        }

        private static HL_type* GenerateErrorType()
        {
            var type = (HL_type*)NativeMemory.AllocZeroed((nuint)sizeof(HL_type));
            type->kind = HL_type.TypeKind.HOBJ;
            type->mark_bits = null;
            var tobj = (HL_type_obj*)NativeMemory.AllocZeroed((nuint)sizeof(HL_type_obj));
            type->data.obj = tobj;
            tobj->name = (char*)Marshal.StringToHGlobalUni("dotnet.exception");
            tobj->nbindings = 0;
            tobj->nfields = 0;
            tobj->nproto = 0;
            var rtobj = (HL_runtime_obj*)NativeMemory.AllocZeroed((nuint)sizeof(HL_runtime_obj));
            tobj->rt = rtobj;
            rtobj->nfields = 0;
            rtobj->hasPtr = false;
            rtobj->nmethods = 0;
            rtobj->methods = (void**)NativeMemory.AllocZeroed((nuint)sizeof(void*));
            rtobj->nlookup = 0;
            rtobj->nbindings = 0;
            rtobj->size = 64;
            rtobj->t = type;
            rtobj->toStringFun = &ExceptionToString;
            return type;
        }
        static NETExcepetionError()
        {
            ErrorType = GenerateErrorType();
        }
    }
}
