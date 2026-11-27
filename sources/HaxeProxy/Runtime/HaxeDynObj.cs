using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;
using HaxeProxy.Runtime.Internals;
using System.Dynamic;

namespace HaxeProxy.Runtime
{
    /// <summary>
    /// Represents a dynamic Haxe object whose fields are accessed by name at runtime.
    /// </summary>
    public sealed unsafe class HaxeDynObj : HaxeProxyBase
    {
        /// <summary>
        /// Initializes a new empty dynamic Haxe object.
        /// </summary>
        public HaxeDynObj() : base(new HashlinkDynObj())
        {
        }

        private HaxeDynObj( HashlinkObj obj ) : base(HashlinkMarshal.ConvertHashlinkObject<HashlinkDynObj>
            (HashlinkNative.hl_make_dyn(
            (void*)obj.HashlinkPointer, obj.NativeType))!)
        {
        }
        /// <summary>
        /// Initializes a new dynamic Haxe object by copying the fields of another dynamic object.
        /// </summary>
        /// <param name="obj">The dynamic object to copy.</param>
        public HaxeDynObj( HaxeDynObj obj ) : this(((HashlinkDynObj)obj.HashlinkObj).Copy())
        {
        }


        /// <summary>
        /// Returns a dynamic view over this object for dynamic member access.
        /// </summary>
        /// <returns>A dynamic wrapper around this object.</returns>
        public dynamic AsDynamic() => HashlinkObj.AsDynamic();

        /// <inheritdoc/>
        public override bool TryGetMember( GetMemberBinder binder, out object? result )
        {
            result = HaxeProxyHelper.GetProxy<object>(((HashlinkDynObj)HashlinkObj).GetFieldValue(binder.Name));
            return true;
        }
        /// <inheritdoc/>
        public override bool TrySetMember( SetMemberBinder binder, object? value )
        {
            ((HashlinkDynObj)HashlinkObj).SetFieldValue(binder.Name, value);
            return true;
        }
        /// <inheritdoc/>
        public override bool TryGetIndex( GetIndexBinder binder, object[] indexes, out object? result )
        {
            result = HaxeProxyHelper.GetProxy<object>(((HashlinkDynObj)HashlinkObj).GetFieldValue(indexes[0].ToString()!));
            return true;
        }
        /// <inheritdoc/>
        public override bool TrySetIndex( SetIndexBinder binder, object[] indexes, object? value )
        {
            ((HashlinkDynObj)HashlinkObj).SetFieldValue(indexes[0].ToString()!, value);
            return true;
        }
    }
}
