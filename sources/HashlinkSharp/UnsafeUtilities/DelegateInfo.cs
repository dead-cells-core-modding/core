using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

#nullable disable

namespace Hashlink.UnsafeUtilities
{
    public class DelegateInfo
    {
        public static readonly FieldInfo FI_self = typeof(DelegateInfo).GetField(nameof(self));
        public static readonly FieldInfo FI_invokePtr = typeof(DelegateInfo).GetField(nameof(invokePtr));

        public object self;
        public nint invokePtr;
        public DelegateInfo( object self, nint invokePtr)
        {
            this.self = self;
            this.invokePtr = invokePtr;
        }


        public DelegateInfo( Delegate target )
        {
            if (target.HasSingleTarget && target.Target != null)
            {
                self = target.Target;
                invokePtr = target.Method.MethodHandle.GetFunctionPointer();
            }
            else
            {
                self = target;
                invokePtr = target.GetType().GetMethod("Invoke").MethodHandle.GetFunctionPointer();
            }
        }
    }
}
