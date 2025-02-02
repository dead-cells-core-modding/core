using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using Haxe.Marshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Haxe
{
    public unsafe class HaxeClosure( HashlinkClosure closure ) : HaxeSpecializedObjectBase<HashlinkClosure>(closure)
    {
        public HashlinkFunc Function => HashlinkObject.Function;
        private object? cached_this;
        public HaxeObject? This
        {
            get
            {
                if (HashlinkObject.BindingThis == null)
                {
                    return null;
                }
                return (HaxeObject)HaxeMarshal.ConvertHashlinkObj(
                    HashlinkMarshal.ConvertHashlinkObject((void*)HashlinkObject.BindingThis!.Value)
                    );
            }
            set
            {
                if (value is HaxeObjectBase hobj)
                {
                    HashlinkObject.BindingThis = hobj.HashlinkObject.HashlinkPointer;
                }
                else
                {
                    HashlinkObject.BindingThis = null;
                }
                cached_this = value;
            }
        }
    }
}
