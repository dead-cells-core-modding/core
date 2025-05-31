using Hashlink;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Reflection.Types;
using Hashlink.UnsafeUtilities;
using Hashlink.Wrapper.Callbacks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Hooks
{
    internal unsafe class HookOrigClosure : HashlinkClosure
    {
        private static HL_vclosure value;

        private static HashlinkObjPtr GetLocalValuePtr( HashlinkFuncType funcType )
        {
            value.type = funcType.NativeType;
            return HashlinkObjPtr.Get(Unsafe.AsPointer(ref value));
        }
        public HookOrigClosure( HashlinkFuncType funcType, Delegate target ) : 
            base(GetLocalValuePtr(funcType))
        {
            callback = HlCallbackFactory.GetHlCallback(
                funcType
                );
            callback.Target = target.CreateAdaptDelegate();
        }
        public override nint HashlinkPointer => throw new NotSupportedException();
    }
}
