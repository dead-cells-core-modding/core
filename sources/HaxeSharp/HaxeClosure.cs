using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
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
    public unsafe class HaxeClosure( HashlinkClosure closure ) : HaxeSpecializedObjectBase<HashlinkClosure>(closure)
    {
        private object? cached_this;
        public HaxeObject? This
        {
            get
            {
                if (HashlinkObject.BindingThis == null)
                {
                    return null;
                }
                return (HaxeObject?)HaxeMarshal.ConvertHashlinkObj(
                    HashlinkObject.BindingThis
                    );
            }
            
        }
        public override bool TryInvoke( InvokeBinder binder, object?[]? args, out object? result )
        {
            result = HaxeMarshal.PostProcessValue(HashlinkObject.DynamicInvoke(args ?? []));
            return true;
        }
    }
}
