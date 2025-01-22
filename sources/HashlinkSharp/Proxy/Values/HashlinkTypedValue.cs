using Hashlink.Marshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.Values
{
    public unsafe class HashlinkTypedValue<TValue>(void* val) : HashlinkValue(val)
        where TValue : unmanaged
    {
        public TValue TypedValue
        {
            get
            {
                return *(TValue*)&TypedRef->val.i64;
            }
            set
            {
                *(TValue*)&TypedRef->val.i64 = value;
            }
        }

        public override object Value { get => TypedValue; set => TypedValue = (TValue)value; }
    }
}
