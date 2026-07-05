using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using HaxeProxy.Runtime.Internals;
using System.Diagnostics;
using System.Dynamic;

namespace HaxeProxy.Runtime
{
    public abstract unsafe class HaxeProxyBase :
        DynamicObject,
        IExtraData,
        IExtraDataItem,
        IHashlinkPointer
    {
        protected HaxeProxyBase( HashlinkObj obj )
        {
            Debug.Assert(obj != null);
            HashlinkObj = obj;
            if (!createByManager)
            {
                IExtraData ied = obj;
                if (ied.GetOrCreateData<HaxeProxyBase>(_ => this) != this)
                {
                    throw new InvalidOperationException();
                }
                HaxeProxyManager.CheckCustomProxy(this, obj);
            }
            AfterBinding();
        }
        public HashlinkObj HashlinkObj {
            get;
        }

        public nint HashlinkPointer => ((IHashlinkPointer)HashlinkObj).HashlinkPointer;


        internal bool createByManager;

        static object IExtraDataItem.Create( HashlinkObj obj )
        {
            return HaxeProxyManager.CreateProxy(obj);
        }

        public override string ToString()
        {
            return HashlinkObj.ToString();
        }
        protected virtual void AfterBinding()
        {
        }

        public override bool TryConvert( ConvertBinder binder, out object? result )
        {
            if (binder.Type.IsAssignableTo(typeof(HashlinkObj)))
            {
                result = HashlinkObj;
                return true;
            }
            if (binder.Type == typeof(string))
            {
                result = ToString();
                return true;
            }
            return base.TryConvert(binder, out result);
        }
        public virtual T ToVirtual<T>() where T : HaxeVirtual
        {
            var tid = HaxeProxyManager.type2typeId[typeof(T)];
            var vt = HashlinkMarshal.Module.PreferTypes[tid];
            var result = (HashlinkVirtual)HashlinkMarshal.ConvertHashlinkObject(
                HashlinkNative.hl_to_virtual(vt.NativeType, (HL_vdynamic*)HashlinkPointer)
                )!;
            return result.AsHaxe<T>();
        }
        public virtual T AsObject<T>() where T : HaxeObject
        {
            if (this is T result)
            {
                return result;
            }
            if (this is HaxeVirtual)
            {
                return ((HashlinkVirtual)HashlinkObj).GetValue()?.AsHaxe<T>()
                    ?? throw new InvalidCastException();
            }
            throw new InvalidCastException();
        }
        T IExtraData.GetOrCreateData<T>( Func<HashlinkObj, object> factory ) where T : class
        {
            return ((IExtraData)HashlinkObj).GetOrCreateData<T>(factory);
        }
    }
}
