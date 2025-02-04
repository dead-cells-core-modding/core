namespace Hashlink
{
    public unsafe interface IHashlinkCustomMarshaler
    {
        public bool TryWriteData( void* target, TypeKind? typeKind );
    }
}
