namespace Hashlink.Reflection.Types
{
    public unsafe class HashlinkAbstractType(HashlinkModule module, HL_type* type) : HashlinkSpecialType<char>(module, type)
    {
        private string? cachedName;
        public override string? Name => cachedName ??= new(TypeData);
    }
}
