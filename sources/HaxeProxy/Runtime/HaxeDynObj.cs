using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;

namespace HaxeProxy.Runtime
{
    public sealed unsafe class HaxeDynObj : HaxeProxyBase
    {
        public HaxeDynObj( ) : base(new HashlinkDynObj())
        {
        }

        private HaxeDynObj( HashlinkObj obj ) : base(HashlinkMarshal.ConvertHashlinkObject<HashlinkObj>
            (HashlinkNative.hl_make_dyn(
            (void*)obj.HashlinkPointer, obj.NativeType))!)
        {
        }

        public HaxeDynObj( HaxeObject obj ) : this(obj.HashlinkObj)
        {
        }

        public HaxeDynObj( HaxeVirtual obj ) : this(obj.HashlinkObj)
        {
        }
        public HaxeDynObj( HaxeDynObj obj ) : this(obj.HashlinkObj)
        {
        }


        public dynamic AsDynamic() => HashlinkObj.AsDynamic();

        
    }
}
