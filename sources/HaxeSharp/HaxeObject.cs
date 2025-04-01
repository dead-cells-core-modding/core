using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using Haxe.Marshaling;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haxe
{
    public class HaxeObject( HashlinkObject obj ) : HaxeSpecializedObjectBase<HashlinkObject>(obj)
    {
        public HaxeObject( HashlinkObjectType type ) : this(new HashlinkObject(type))
        {
        
        }
        public override bool TryGetMember( GetMemberBinder binder, out object? result )
        {
            result = HaxeMarshal.PostProcessValue(HashlinkObject.GetFieldValue(binder.Name));
            return true;
        }
        public override bool TryInvokeMember( InvokeMemberBinder binder, object?[]? args, out object? result )
        {
            /*var name = binder.Name;
            var func = HashlinkObject.GetFunction(name);
            if (func == null)
            {
                result = null;
                return false;
            }
            result = HaxeMarshal.PostProcessValue(func.CallDynamic( args));
            return true;*/
            throw new NotImplementedException();
        }
        public override bool TrySetMember( SetMemberBinder binder, object? value )
        {
            HashlinkObject.SetFieldValue(binder.Name, value);
            return true;
        }
       
    }
}
