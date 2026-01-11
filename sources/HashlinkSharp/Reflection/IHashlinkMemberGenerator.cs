using Hashlink.Reflection.Members;

namespace Hashlink.Reflection
{
    public unsafe interface IHashlinkMemberGenerator
    {
        abstract static HashlinkMember GenerateFromPointer( HashlinkModule module, void* ptr );
    }
}
