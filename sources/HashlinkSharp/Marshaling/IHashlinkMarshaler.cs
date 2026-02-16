using Hashlink.Reflection.Types;

namespace Hashlink.Marshaling
{
    public unsafe interface IHashlinkMarshaler
    {
        HashlinkType? GetHashlinkType( Type type );
        object? TryConvertHashlinkObject( void* target );
        object? TryReadData( void* target, HashlinkType? typeKind );
        bool TryWriteData( void* target, object? value, HashlinkType? typeKind );
    }
}
