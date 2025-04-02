using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using Hashlink.Proxy;
using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Hashlink.Marshaling;
using System.Diagnostics;

namespace Hashlink.Wrapper
{
    internal unsafe class WrapperHelper
    {
        public static void ThrowNetException( Exception ex )
        {
            if (Debugger.IsAttached)
            {
                Debugger.Log(4, null, "Uncaught .NET Exception: " + ex.ToString());
                Debugger.Break();
            }
            if (ex is HashlinkError err)
            {
                hl_throw((HL_vdynamic*) err.Error);
            }
            hl_throw((HL_vdynamic*)new HashlinkNETExceptionObj( ex ).HashlinkPointer);
        }
        public static object? GetObjectFromPtr( nint ptr )
        {
            return HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.GetUnsafe(ptr), null);
        }
        public static nint AsPointer( object obj, int typeIdx )
        {
            return AsPointerWithType(obj, HashlinkMarshal.Module.Types[typeIdx]);
        }
        public static nint AsPointerWithType( object obj, HashlinkType type )
        {
            if (!type.IsPointer)
            {
                throw new InvalidOperationException();
            }
            
            nint result = 0;
            HashlinkMarshal.WriteData(&result, obj, type);
            if (result != 0 && 
                hl_is_gc_ptr((void*)result) &&
                !hl_ptr_is_alive((void*)result))
            {
                throw new InvalidProgramException();
            }
            return result;
        }
    }
}
