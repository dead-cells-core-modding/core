using Hashlink.Proxy.Clousre;
using System.Dynamic;

namespace Hashlink.Proxy.DynamicAccess
{
    [Obsolete]
    internal class HashlinkClosureDynamicAccess(HashlinkClosure cl) : HashlinkObjDynamicAccess(cl)
    {
        public override bool TryInvoke( InvokeBinder binder, object?[]? args, out object? result )
        {
            result = DynamicAccessUtils.AsDynamic(cl.DynamicInvoke(args ?? []));
            return true;
        }
        public override bool TryConvert( ConvertBinder binder, out object? result )
        {
            if (binder.Type.IsAssignableTo(typeof(Delegate)))
            {
                result = cl.CreateDelegate(binder.Type);
                return true;
            }
            return base.TryConvert(binder, out result);
        }
    }
}
