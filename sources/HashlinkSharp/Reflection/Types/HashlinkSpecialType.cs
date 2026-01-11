namespace Hashlink.Reflection.Types
{
    public unsafe class HashlinkSpecialType<T>( HashlinkModule module, HL_type* type) : HashlinkType(module, type)
        where T : unmanaged
    {
        public T* TypeData => (T*)NativeType->data.obj;
    }
}
