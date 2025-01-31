using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
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
        
        public override bool TryGetMember( GetMemberBinder binder, out object? result )
        {
            result = HaxeMarshal.PostProcessValue(HashlinkObject.GetFieldValue(binder.Name));
            return true;
        }
        public override bool TrySetMember( SetMemberBinder binder, object? value )
        {
            HashlinkObject.SetFieldValue(binder.Name, value);
            return true;
        }
       
    }
}
