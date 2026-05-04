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
                    return (HashlinkObj?) HashlinkMarshal.ConvertHashlinkObject(virt->value);
                }
            }
            return null;
        }


        // 此版本调用了 hl_dyn_getp/hl_dyn_setp，这些函数假定内存布局为 HL_vdynamic，与 HL_vvirtual 不兼容。

        // public override object? GetFieldValue( int hashedName )
        // {
        //     var ptr = hl_obj_lookup((HL_vdynamic*)TypedRef, hashedName, out var ftype);
        //     if (ptr == null)
        //     {
        //         ptr = hl_obj_lookup_extra((HL_vdynamic*)TypedRef, hashedName);
        //         return ptr != null
        //             ? HashlinkMarshal.ConvertHashlinkObject(ptr)
        //             : throw new MissingFieldException(Type.Name, new string(hl_field_name(hashedName)));
        //     }
        //     return HashlinkMarshal.ReadData(ptr, HashlinkMarshal.GetHashlinkType(ftype));
        // }
        // public override void SetFieldValue( int hashedName, object? value )
        // {
        //     var ptr = hl_obj_lookup((HL_vdynamic*)TypedRef, hashedName, out var ftype);
        //     if (ptr == null)
        //     {
        //         if (!hl_obj_has_field((HL_vdynamic*)TypedRef, hashedName))
        //             throw new MissingFieldException(Type.Name, new string(hl_field_name(hashedName)));
        //         nint val = 0;
        //         HashlinkMarshal.WriteDataDyn(&val, value);
        //         hl_obj_set_field((HL_vdynamic*)TypedRef, hashedName, (HL_vdynamic*)val);
        //         return;
        //     }
        //     HashlinkMarshal.WriteData(ptr, value, HashlinkMarshal.GetHashlinkType(ftype));
        // }
    }
}
