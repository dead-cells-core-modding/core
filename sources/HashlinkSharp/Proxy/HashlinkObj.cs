using Hashlink.Marshaling;
using Hashlink.Reflection.Types;

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
            if (!HashlinkMarshal.IsHashlinkObject((void*)ptr))
            {
                throw new InvalidOperationException();
            }
            HashlinkPointer = ptr;
            NativeType = *(HL_type**)ptr;
            Type = HashlinkMarshal.Module.GetMemberFrom<HashlinkType>(NativeType);
            TypeKind = NativeType->kind;
        }
        public override string? ToString()
        {
            return new(hl_to_string((HL_vdynamic*)HashlinkPointer));
        }
        public HashlinkObjHandle? Handle
        {
            get;
        }
        public TypeKind TypeKind
        {
            get;
        }
        public HashlinkType Type
        {
            get;
        }
        public HL_type* NativeType
        {
            get;
        }
        public nint HashlinkPointer
        {
            get;
        }
    }
}
