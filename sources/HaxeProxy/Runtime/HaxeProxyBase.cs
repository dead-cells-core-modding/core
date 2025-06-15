using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using HaxeProxy.Runtime.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime
{
    public abstract unsafe class HaxeProxyBase : 
        IExtraData,
        IExtraDataItem,
        IHashlinkPointer
    {
        protected HaxeProxyBase( HashlinkObj obj )
        {
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
        public HashlinkObj HashlinkObj
        {
            get;
        }

        public nint HashlinkPointer => ((IHashlinkPointer)HashlinkObj).HashlinkPointer;

        internal bool createByManager;

        static object IExtraDataItem.Create( HashlinkObj obj )
        {
            return HaxeProxyManager.CreateProxy( obj );
        }

        public override string ToString()
        {
            return HashlinkObj.ToString();
        }
        protected virtual void AfterBinding()
        {
        }

        public T ToVirtual<T>() where T : HaxeVirtual
        {
            var tid = HaxeProxyManager.type2typeId[typeof(T)];
            var vt = HashlinkMarshal.Module.Types[tid];
            var result = (HashlinkVirtual) HashlinkMarshal.ConvertHashlinkObject(
                HashlinkNative.hl_to_virtual(vt.NativeType, (HL_vdynamic*)HashlinkPointer)
                )!;
            return result.AsHaxe<T>();
        }
        public T AsObject<T>() where T : HaxeObject
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
