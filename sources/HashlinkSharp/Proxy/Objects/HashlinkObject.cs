using Hashlink.Reflection.Types;
using System.Diagnostics;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkObject( HashlinkObjPtr objPtr ) : HashlinkFieldObject<HL_vdynamic>(objPtr)
    {
        public HashlinkObject( HashlinkObjectType objType ) : this(HashlinkObjPtr.Get(
            hl_alloc_obj(objType.NativeType))
            )
        {
            Debug.Assert(Handle != null);
        }
        public HL_runtime_obj* RuntimeObj => NativeType->data.obj->rt;
        
    }
}
