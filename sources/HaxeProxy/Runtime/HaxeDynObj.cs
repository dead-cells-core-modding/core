using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;
using HaxeProxy.Runtime.Internals;
using System.Dynamic;

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

        public override bool TryGetMember( GetMemberBinder binder, out object? result )
        {
            result =  HaxeProxyHelper.GetProxy<object>(((HashlinkDynObj)HashlinkObj).GetFieldValue(binder.Name));
            return true;
        }
        public override bool TrySetMember( SetMemberBinder binder, object? value )
        {
            ((HashlinkDynObj)HashlinkObj).SetFieldValue(binder.Name, value);
            return true;
        }
        public override bool TryGetIndex( GetIndexBinder binder, object[] indexes, out object? result )
        {
            result = HaxeProxyHelper.GetProxy<object>(((HashlinkDynObj)HashlinkObj).GetFieldValue(indexes[0].ToString()!));
            return true;
        }
        public override bool TrySetIndex( SetIndexBinder binder, object[] indexes, object? value )
        {
            ((HashlinkDynObj)HashlinkObj).SetFieldValue(indexes[0].ToString()!, value);
            return true;
        }
    }
}
