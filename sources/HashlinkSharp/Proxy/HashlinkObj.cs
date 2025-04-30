using Hashlink.Marshaling;
using Hashlink.Marshaling.ObjHandle;
using Hashlink.Reflection.Types;

namespace Hashlink.Proxy
{
    public abstract unsafe partial class HashlinkObj : IHashlinkPointer
    {
        

      
        public HashlinkObj( HashlinkObjPtr objPtr )
        {
            var ptr = objPtr.Pointer;
            Handle = HashlinkObjManager.GetHandle(ptr);
            if (Handle != null)
            {
                Handle.Target = this;
            }

            HashlinkPointer = ptr;
            NativeType = *(HL_type**)ptr;
            Type = HashlinkMarshal.Module.GetMemberFrom<HashlinkType>(NativeType);
        }
        public override string ToString()
        {
            return new string(hl_to_string((HL_vdynamic*)HashlinkPointer)) ?? Type.Name ?? "";
        }

        public HashlinkObjHandle? Handle
        {
            get; 
        }
        public TypeKind TypeKind => Type.TypeKind;
        public HashlinkType Type
        {
            get; 
        }
        public HL_type* NativeType
        {
            get; 
        }
        public virtual nint HashlinkPointer
        {
            get; 
        }
    }
}
