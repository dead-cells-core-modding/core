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
using System.Runtime.CompilerServices;

namespace Hashlink.Wrapper
{
    internal unsafe class WrapperHelper
    {
        public struct ErrorHandle
        {
            public HL_trap_ctx trap_ctx;
            public nint ebp;
            public nint esp;

        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowNetException( Exception ex )
        {
            if (Debugger.IsAttached)
            {
                Debugger.Log(4, null, "Uncaught .NET Exception: " + ex.ToString());
                hl_dump_stack();
                Debugger.Break();
            }
            if (ex is HashlinkError err)
            {
                hl_throw((HL_vdynamic*) err.Error);
            }
            hl_throw((HL_vdynamic*)new HashlinkNETExceptionObj( ex ).HashlinkPointer);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object? GetObjectFromPtr( nint ptr )
        {
            return HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(ptr), null);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetObjectFrom<T>( object obj ) where T : class, IExtraDataItem
        {
            if (obj is T result)
            {
                return result;
            }
            if (obj is IExtraData ied)
            {
                return ied.GetData<T>();
            }
            return (T)(dynamic)obj;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint AsPointer( object obj, int typeIdx )
        {
            return AsPointerWithType(obj, HashlinkMarshal.Module.Types[typeIdx]);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint AsPointerWithType( object obj, HashlinkType type )
        {
            if (!type.IsPointer)
            {
                throw new InvalidOperationException();
            }
            
            nint result = 0;
            HashlinkMarshal.WriteData(&result, obj, type);
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InitErrorHandler(ref ErrorHandle handle)
        {
            var ti = hl_get_thread();
            if (ti == null)
            {
                return;
            }
            handle.trap_ctx.prev = ti->trap_current;
            handle.trap_ctx.tcheck = (HL_vdynamic*)0x4e455445;

            //ti->trap_current = (HL_trap_ctx*) Unsafe.AsPointer(ref handle.trap_ctx);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnInitErrorHandler(ref ErrorHandle handle)
        {
            var ti = hl_get_thread();
            if (ti == null)
            {
                return;
            }
            if (ti->trap_current == (HL_trap_ctx*)Unsafe.AsPointer(ref handle.trap_ctx))
            {
                ti->trap_current = handle.trap_ctx.prev;
            }
        }
    }
}
