using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
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
    public class HaxeSpecializedObjectBase<THashlink>(THashlink obj ) : HaxeObjectBase(obj) where THashlink : HashlinkObj
    {
        public new THashlink HashlinkObject => (THashlink)((HaxeObjectBase)this).HashlinkObject;
    }
    public class HaxeObjectBase: DynamicObject, IHashlinkPointer
    {
        public HaxeObjectBase(HashlinkObj obj)
        {
            if (!HaxeMarshal.objMapping.TryAdd(obj, this))
            {
                throw new InvalidOperationException();
            }
            HashlinkObject = obj;
        }
        public HashlinkObj HashlinkObject
        {
            get;
        }
        public HashlinkType Type => HashlinkObject.Type;

        public dynamic Chain => this;
        public nint HashlinkPointer => ((IHashlinkPointer)HashlinkObject).HashlinkPointer;
        public override string? ToString()
        {
            return HashlinkObject.ToString();
        }
    }
}
