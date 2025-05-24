using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.DynamicAccess
{
    internal class HashlinkObjectDynamicAccess(IHashlinkFieldObject obj) : HashlinkObjDynamicAccess((HashlinkObj)obj)
    {
        public override bool TryGetMember( GetMemberBinder binder, out object? result )
        {
            result = DynamicAccessUtils.AsDynamic(obj.GetFieldValue(binder.Name));
            return true;
        }
        public override bool TryInvokeMember( InvokeMemberBinder binder, object?[]? args, out object? result )
        {
            var name = binder.Name;
            var func = obj.GetFieldValue(name);
            if (func == null)
            {
                result = null;
                return false;
            }
            result = DynamicAccessUtils.AsDynamic(((HashlinkClosure)func).DynamicInvoke(args));
            return true;
        }
        public override bool TrySetMember( SetMemberBinder binder, object? value )
        {
            obj.SetFieldValue(binder.Name, value);
            return true;
        }
    }
}
