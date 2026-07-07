using Hashlink.Marshaling;
using Hashlink.Reflection.Types;
using System.Diagnostics;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkVirtual( HashlinkObjPtr objPtr ) : HashlinkFieldObject<HL_vvirtual>(objPtr)
    {
        public HashlinkVirtual( HashlinkVirtualType type ) : this(HashlinkObjPtr.Get(hl_alloc_virtual(EnsureThreadRegistered(type.NativeType))))
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
        public override object? GetFieldValue( int hashedName )
        {
            var ptr = hl_obj_lookup((HL_vdynamic*)HashlinkPointer, hashedName, out var ftype);
            if (ptr == null)
            {
                ptr = hl_obj_lookup_extra((HL_vdynamic*)HashlinkPointer, hashedName);
                return ptr != null
                    ? HashlinkMarshal.ConvertHashlinkObject(ptr)
                    : HashlinkMarshal.ConvertHashlinkObject(
                        hl_dyn_getp((HL_vdynamic*)TypedRef, hashedName, InternalTypes.hlt_dyn));
            }
            return HashlinkMarshal.ReadData(ptr, HashlinkMarshal.GetHashlinkType(ftype));
        }
        public override void SetFieldValue( int hashedName, object? value )
        {

            var ptr = hl_obj_lookup((HL_vdynamic*)HashlinkPointer, hashedName, out var ftype);
            if (ptr == null)
            {
                if (!hl_obj_has_field((HL_vdynamic*)HashlinkPointer, hashedName))
                {
                    hl_dyn_setp((HL_vdynamic*)TypedRef, hashedName, InternalTypes.hlt_dyn, (void*)HashlinkMarshal.GetDyn(value));
                    return;
                }
                nint val = 0;
                HashlinkMarshal.WriteDataDyn(&val, value);
                hl_obj_set_field((HL_vdynamic*)HashlinkPointer, hashedName, (HL_vdynamic*)val);
                return;
            }
            HashlinkMarshal.WriteData(ptr, value, HashlinkMarshal.GetHashlinkType(ftype));
        }
    }
}
