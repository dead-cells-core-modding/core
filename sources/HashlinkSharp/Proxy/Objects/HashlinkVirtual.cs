using Hashlink.Marshaling;
using Hashlink.Reflection.Types;
using System.Diagnostics;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkVirtual( HashlinkObjPtr objPtr ) : HashlinkFieldObject<HL_vvirtual>(objPtr)
    {
        public HashlinkVirtual( HashlinkVirtualType type ) : this(HashlinkObjPtr.Get(hl_alloc_virtual(type.NativeType)))
        {
            Debug.Assert(Handle != null);
        }

        public HashlinkObj? GetValue()
        {
            var virt = TypedRef;
            while (virt != null)
            {
                if (virt->value != null)
                {
                    return (HashlinkObj?) HashlinkMarshal.ConvertHashlinkObject(virt->value);
                }
            }
            return null;
        }
    }
}
