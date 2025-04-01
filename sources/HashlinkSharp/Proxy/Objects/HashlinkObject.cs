using Hashlink.Reflection.Types;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkObject( HashlinkObjPtr objPtr ) : HashlinkFieldObject<HL_vdynamic>(objPtr)
    {
        public HashlinkObject( HashlinkObjectType objType ) : this(HashlinkObjPtr.GetUnsafe(
            hl_alloc_obj(objType.NativeType))
            )
        {

        }
        public HL_runtime_obj* RuntimeObj => NativeType->data.obj->rt;
        
    }
}
