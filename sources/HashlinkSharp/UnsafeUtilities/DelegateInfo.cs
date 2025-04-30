using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.UnsafeUtilities
{
    public class DelegateInfo
    {
        private static readonly ConcurrentDictionary<Type, nint> cachedInvokePtr = [];

        public static readonly FieldInfo FI_self = typeof(DelegateInfo).GetField(nameof(self))!;
        public static readonly FieldInfo FI_invokePtr = typeof(DelegateInfo).GetField(nameof(invokePtr))!;

        public object? self;
        public MethodInfo? method;
        public nint invokePtr;
        public DelegateInfo( nint invokePtr)
        {
            this.invokePtr = invokePtr;
        }

        public DelegateInfo( object self, MethodInfo method )
        {
            this.self = self;
            this.method = method;
            if (method is DynamicMethod dm)
            {
                invokePtr = dm.GetDynamicMethodHandle().GetFunctionPointer();
            }
            else
            {
                invokePtr = method.MethodHandle.GetFunctionPointer();
            }
        }

        public DelegateInfo( Delegate target )
        {
            method = target.Method;
            if (target.HasSingleTarget && 
                target.Target != null)
            {
                self = target.Target;
                invokePtr = target.Method is DynamicMethod dm ? dm.GetDynamicMethodHandle().GetFunctionPointer() :
                    target.Method.MethodHandle.GetFunctionPointer();
            }
            else
            {
                self = target;
                invokePtr = cachedInvokePtr.GetOrAdd(target.GetType(),
                    type => type.GetMethod("Invoke")!.MethodHandle.GetFunctionPointer());
            }
        }

        public Delegate CreateDelegate( Type type )
        {
            if (self is Delegate d)
            {
                return d.CreateAdaptDelegate(type);
            }
            if (method != null)
            {
                return Delegate.CreateDelegate(type, self, method);
            }
            if (self != null)
            {
                throw new NotSupportedException();
            }
            return Marshal.GetDelegateForFunctionPointer(invokePtr, type);
        }
        public T CreateDelegate<T>() where T : Delegate
        {
            return (T)CreateDelegate(typeof(T));
        }
    }
}
