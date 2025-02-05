using Hashlink.Marshaling;
using Hashlink.Reflection.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.Values
{
    public unsafe class HashlinkUnboxRef( HashlinkRefType type, void* target ) :  IHashlinkPointer
    {
        public HashlinkRefType Type => type;
        public HashlinkType TargetType => Type.RefType;
        public object? RefValue
        {
            get
            {
                return HashlinkMarshal.ReadData(Value, TargetType);
            }
            set
            {
                HashlinkMarshal.WriteData(Value, value, TargetType);
            }
        }
        public void* Value
        {
            get; set;
        } = target;
        nint IHashlinkPointer.HashlinkPointer => (nint)Value;
        public override string? ToString()
        {
            return Type.ToString();
        }
    }
}
