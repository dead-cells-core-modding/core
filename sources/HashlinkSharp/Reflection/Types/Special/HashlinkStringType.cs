using Hashlink.Proxy;
using Hashlink.Proxy.Objects;

namespace Hashlink.Reflection.Types.Special
{
    internal unsafe class HashlinkStringType( HashlinkModule module, HL_type* type ) : HashlinkObjectType(module, type)
    {
        public override HashlinkObj CreateInstance()
        {
            return new HashlinkString();
        }
    }
}
