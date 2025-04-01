using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

#nullable disable

namespace Hashlink.UnsafeUtilities
{
    internal class DelegateInfo
    {
        internal static FieldInfo FI_self = typeof(DelegateInfo).GetField(nameof(self));
        internal static FieldInfo FI_invokePtr = typeof(DelegateInfo).GetField(nameof(invokePtr));

        public Delegate self;
        public nint invokePtr;

        public DelegateInfo( Delegate target )
        {
            self = target;
            invokePtr = target.GetType().GetMethod("Invoke").MethodHandle.GetFunctionPointer();
        }
    }
}
