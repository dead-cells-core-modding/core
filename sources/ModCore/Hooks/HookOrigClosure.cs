using Hashlink;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Reflection.Types;
using Hashlink.UnsafeUtilities;
using Hashlink.Wrapper.Callbacks;
using System.Runtime.CompilerServices;

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
        public override nint HashlinkPointer => 0;
    }
}
