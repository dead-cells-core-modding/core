using Hashlink.Proxy;
using Hashlink.Proxy.Objects;

namespace Hashlink.Reflection.Types
{
    public unsafe class HashlinkArrayType(HashlinkModule module, HL_type* type) : HashlinkSpecialType<HL_array>(module, type)
    {
        public HashlinkArray CreateInstance( HashlinkType itemType, int length )
        {
            return new HashlinkArray(itemType, length);
        }
        public override HashlinkObj CreateInstance()
        {
            return CreateInstance(Module.KnownTypes.Dynamic, 0);
        }
    }
}
