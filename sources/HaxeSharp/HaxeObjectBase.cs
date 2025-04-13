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
    [Obsolete]
    public class HaxeSpecializedObjectBase<THashlink>(THashlink obj ) : HaxeObjectBase(obj) where THashlink : HashlinkObj
    {
        public new THashlink HashlinkObject => (THashlink)((HaxeObjectBase)this).HashlinkObject;
    }
    [Obsolete]
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
        [Obsolete]
        public dynamic Chain => this;
        public dynamic Dynamic => this;
        public nint HashlinkPointer => ((IHashlinkPointer)HashlinkObject).HashlinkPointer;
        public override string? ToString()
        {
            return HashlinkObject.ToString();
        }
    }
}
