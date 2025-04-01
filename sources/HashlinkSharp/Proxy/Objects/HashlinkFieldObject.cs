using Hashlink.Marshaling;
using Hashlink.Proxy.Clousre;
using Hashlink.Reflection.Types;

namespace Hashlink.Proxy.Objects
{
    public abstract unsafe class HashlinkFieldObject<T>( HashlinkObjPtr objPtr ) : HashlinkTypedObj<T>(objPtr),
        IHashlinkFieldObject
        where T : unmanaged
    {
        public virtual bool HasField( int hashedName )
        {
            return hl_obj_has_field((HL_vdynamic*)HashlinkPointer, hashedName);
        }
        public virtual bool HasField( string name )
        {
            fixed (char* pname = name)
            {
                return HasField(hl_hash_gen(pname, false));
            }
        }
        
        public virtual object? GetFieldValue( int hashedName )
        {
            var ptr = hl_obj_lookup((HL_vdynamic*)HashlinkPointer, hashedName, out var ftype);
            if (ptr == null)
            {
                ptr = hl_obj_lookup_extra((HL_vdynamic*)HashlinkPointer, hashedName);
                return ptr != null
                    ? (object?)HashlinkMarshal.ConvertHashlinkObject(ptr)
                    : throw new MissingFieldException(new string(hl_type_str(NativeType)), new string(hl_field_name(hashedName)));
            }
            return HashlinkMarshal.ReadData(ptr, HashlinkMarshal.GetHashlinkType(ftype));
        }
        public virtual object? GetFieldValue( string name )
        {
            fixed (char* pname = name)
            {
                return GetFieldValue(hl_hash_gen(pname, false));
            }
        }

        public virtual void SetFieldValue( int hashedName, object? value )
        {
            var ptr = hl_obj_lookup((HL_vdynamic*)HashlinkPointer, hashedName, out var ftype);
            if (ptr == null)
            {
                throw new MissingFieldException(new string(hl_type_str(NativeType)), new string(hl_field_name(hashedName)));
            }
            HashlinkMarshal.WriteData(ptr, value, HashlinkMarshal.GetHashlinkType(ftype));
        }
        public virtual void SetFieldValue( string name, object? value )
        {
            fixed (char* pname = name)
            {
                SetFieldValue(hl_hash_gen(pname, false), value);
            }
        }
    }
}
