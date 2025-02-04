using Hashlink.Reflection.Types;

namespace Hashlink
{
    public unsafe interface IHashlinkCustomMarshaler
    {
        public bool TryWriteData( void* target, HashlinkType? typeKind );
    }
}
