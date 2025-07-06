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
using System.Runtime.InteropServices;
using ModCore.Events;

namespace Hashlink.Wrapper
{
    internal unsafe partial class WrapperHelper
    {
        static WrapperHelper()
        {
            EventSystem.AddReceiver(new ExceptionEventHandler());
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
        
    }
}
