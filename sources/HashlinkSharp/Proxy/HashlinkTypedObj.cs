namespace Hashlink.Proxy
{
    public abstract unsafe class HashlinkTypedObj<T>( HashlinkObjPtr objPtr ) : HashlinkObj(objPtr) where T : unmanaged
    {
        public T* TypedRef => (T*)HashlinkPointer;
    }
}
