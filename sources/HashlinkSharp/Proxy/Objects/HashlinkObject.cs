namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkObject( HashlinkObjPtr objPtr ) : HashlinkFieldObject<HL_vdynamic>(objPtr)
    {
        public HashlinkObject( HL_type* objType ) : this(HashlinkObjPtr.GetUnsafe(hl_alloc_obj(objType)))
        {

        }
        public HL_runtime_obj* RuntimeObj => Type->data.obj->rt;
    }
}
