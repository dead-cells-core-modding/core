using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy
{
    public abstract unsafe class HashlinkObj : IHashlinkPointer
    {
        public HashlinkObj(void* objPtr)
        {
            
            HashlinkPointer = (nint)objPtr;
            Type = *(HL_type**)objPtr;
            TypeKind = Type->kind;
        }
        public HL_type.TypeKind TypeKind { get; }
        public HL_type* Type { get; }
        public nint HashlinkPointer { get; }
    }
}
