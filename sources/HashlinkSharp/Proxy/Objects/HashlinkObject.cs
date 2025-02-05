using Hashlink.Reflection.Types;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkObject( HashlinkObjPtr objPtr ) : HashlinkFieldObject<HL_vdynamic>(objPtr)
    {
        public HashlinkObject( HL_type* objType ) : this(HashlinkObjPtr.GetUnsafe(hl_alloc_obj(objType)))
        {

        }
        public HL_runtime_obj* RuntimeObj => NativeType->data.obj->rt;
        public override HashlinkFunc? GetFunction( string name )
        {
            var result = base.GetFunction(name);
            if (result != null)
            {
                return result;
            }
            var ot = (HashlinkObjectType?)Type;
            while (ot != null)
            {
                if (ot.TryFindProto(name, out var proto))
                {
                    var func = proto.CreateFunc(this);
                    func.BindingThis = HashlinkPointer;
                    return func;
                }
                ot = ot.Super;
            }
            return null;
        }
    }
}
