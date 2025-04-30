using Hashlink;
using Hashlink.Proxy;
using HaxeProxy.Runtime.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime
{
    public abstract class HaxeProxyBase : 
        IExtendData,
        IExtendDataItem,
        IHashlinkPointer
    {
        protected HaxeProxyBase( HashlinkObj obj )
        {
            HashlinkObj = obj;
            if (!createByManager)
            {
                IExtendData ied = obj;
                if (ied.GetOrCreateData<HaxeProxyBase>(_ => this) != this)
                {
                    throw new InvalidOperationException();
                }
            }
            AfterBinding();
        }
        public HashlinkObj HashlinkObj
        {
            get;
        }

        public nint HashlinkPointer => ((IHashlinkPointer)HashlinkObj).HashlinkPointer;

        internal bool createByManager;

        static object IExtendDataItem.Create( HashlinkObj obj )
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

        T IExtendData.GetOrCreateData<T>( Func<HashlinkObj, object> factory ) where T : class
        {
            return ((IExtendData)HashlinkObj).GetOrCreateData<T>(factory);
        }
    }
}
