using Hashlink.Proxy;
using Hashlink.Reflection.Types;

namespace Hashlink.Marshaling
{
    public unsafe interface IHashlinkMarshaler
    {
        HashlinkObj? TryConvertHashlinkObject( void* target );
        object? TryReadData( void* target, HashlinkType? typeKind );
        bool TryWriteData( void* target, object? value, HashlinkType? typeKind );
    }
}
