using Hashlink.Reflection.Types;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkVirtual( HashlinkObjPtr objPtr ) : HashlinkFieldObject<HL_vvirtual>(objPtr)
    {
        public HashlinkVirtual( HashlinkVirtualType type ) : this(HashlinkObjPtr.GetUnsafe(hl_alloc_virtual(type.NativeType)))
        {

        }
    }
}
