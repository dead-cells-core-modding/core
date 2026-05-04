using Hashlink.Marshaling;
using Hashlink.Reflection.Types;
using System.Diagnostics;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkVirtual( HashlinkObjPtr objPtr ) : HashlinkFieldObject<HL_vvirtual>(objPtr)
    {
        public HashlinkVirtual( HashlinkVirtualType type ) : this(HashlinkObjPtr.Get(hl_alloc_virtual(type.NativeType)))
        {
            Debug.Assert(Handle != null);
        }

        public HashlinkObj? GetValue()
        {
            var virt = TypedRef;
            while (virt != null)
            {
                if (virt->value != null)
                {
                    return (HashlinkObj?)HashlinkMarshal.ConvertHashlinkObject(virt->value);
                }
            }
            return null;
        }
        
        // 这些函数假定内存布局为 HL_vdynamic，与 HL_vvirtual 不兼容。
        // public override object? GetFieldValue( int hashedName )
        // {
        //     return HashlinkMarshal.ConvertHashlinkObject(
        //         hl_dyn_getp((HL_vdynamic*)TypedRef, hashedName, InternalTypes.hlt_dyn));
        // }
        // public override void SetFieldValue( int hashedName, object? value )
        // {
        //     nint v;
        //     HashlinkMarshal.WriteDataDyn(&v, value);
        //     hl_dyn_setp((HL_vdynamic*)TypedRef, hashedName, InternalTypes.hlt_dyn, (void*)v);
        // }
    }
}
