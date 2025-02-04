using Hashlink.Proxy;

namespace Hashlink.Marshaling
{
    public unsafe interface IHashlinkMarshaler
    {
        HashlinkObj? TryConvertHashlinkObject( void* target );
        object? TryReadData( void* target, TypeKind? typeKind );
        bool TryWriteData( void* target, object? value, TypeKind? typeKind );
    }
}
