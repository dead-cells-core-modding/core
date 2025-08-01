using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.DynamicAccess
{
    public abstract class HashlinkObjDynamicAccess( HashlinkObj obj ) : DynamicObject, 
        IHashlinkPointer, IExtraData, IExtraDataItem
    {
        public HashlinkObj HashlinkObject
        {
            get;
        } = obj;
        public HashlinkType Type => HashlinkObject.Type;
        public nint HashlinkPointer => ((IHashlinkPointer)HashlinkObject).HashlinkPointer;

        private static readonly MethodInfo iextenddata_getdata = typeof(IExtraData).GetMethod(nameof(IExtraData.GetData))!;
        public static object Create( HashlinkObj obj )
        {
            if (obj is HashlinkObject hobj)
            {
                return new HashlinkObjectDynamicAccess(hobj);
            }
            else if (obj is HashlinkArray harray)
            {
                return new HashlinkArrayDynamicAccess(harray);
            }
            else if (obj is HashlinkClosure hcl)
            {
                return new HashlinkClosureDynamicAccess(hcl);
            }
            else if (obj is HashlinkVirtual virt)
            {
                return new HashlinkObjectDynamicAccess(virt);
            }
            else if (obj is HashlinkDynObj dyn)
            {
                return new HashlinkObjectDynamicAccess(dyn);
            }
            throw new NotSupportedException();
        }

        public override bool TryConvert( ConvertBinder binder, out object? result )
        {
            if (binder.Type.IsAssignableTo(typeof(HashlinkObj)))
            {
                result = HashlinkObject;
                return true;
            }
            if (binder.Type == typeof(string))
            {
                result = ToString();
                return true;
            }
            if (binder.Type.IsAssignableTo(typeof(IExtraDataItem)))
            {
                var m = iextenddata_getdata.MakeGenericMethod(binder.Type);
                result = m.Invoke(HashlinkObject, null);
                return true;
            }
            return base.TryConvert( binder, out result );
        }

        public override string? ToString()
        {
            return HashlinkObject.ToString();
        }
        public dynamic AsDynamic => this;

        T IExtraData.GetOrCreateData<T>( Func<HashlinkObj, object> factory ) where T : class
        {
            return ((IExtraData)HashlinkObject).GetOrCreateData<T>(factory);
        }
    }
}
