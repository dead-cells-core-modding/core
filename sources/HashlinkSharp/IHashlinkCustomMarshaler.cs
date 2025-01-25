namespace Hashlink
{
    public unsafe interface IHashlinkCustomMarshaler
    {
        public bool TryWriteData( void* target, HL_type.TypeKind? typeKind );
    }
}
