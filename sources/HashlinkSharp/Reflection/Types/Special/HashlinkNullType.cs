namespace Hashlink.Reflection.Types.Special
{
    public unsafe class HashlinkNullType( HashlinkModule module, HL_type* type ) : HashlinkSpecialType<HL_type>(module, type)
    {
        private HashlinkType? cachedRefType;

        public HashlinkType ValueType => cachedRefType ??= GetMemberFrom<HashlinkType>(TypeData);
    }
}
