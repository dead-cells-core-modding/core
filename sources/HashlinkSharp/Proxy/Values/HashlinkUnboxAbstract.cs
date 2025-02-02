using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.Values
{
    public unsafe class HashlinkUnboxAbstract : HashlinkAbstract, IHashlinkPointer
    {
        public HashlinkUnboxAbstract( HL_type* type, void* target ) : base(HashlinkObjPtr.GetUnsafe(hl_alloc_dynamic(type)))
        {
            TypedRef->val.ptr = target;
        }

        nint IHashlinkPointer.HashlinkPointer => (nint)TypedRef->val.ptr;
    }
}
