using Hashlink.Marshaling;

namespace Hashlink.Proxy
{
    public abstract unsafe class HashlinkObj : IHashlinkPointer
    {
        public HashlinkObj( HashlinkObjPtr objPtr )
        {
            var ptr = objPtr.Pointer;
            Handle = HashlinkObjHandle.GetHandle((void*)ptr);
            if (Handle != null)
            {
                Handle.Target = this;
            }
            HashlinkPointer = ptr;
            Type = *(HL_type**)ptr;
            TypeKind = Type->kind;
        }
        public HashlinkObjHandle? Handle
        {
            get;
        }
        public HL_type.TypeKind TypeKind
        {
            get;
        }
        public HL_type* Type
        {
            get;
        }
        public nint HashlinkPointer
        {
            get;
        }
    }
}
