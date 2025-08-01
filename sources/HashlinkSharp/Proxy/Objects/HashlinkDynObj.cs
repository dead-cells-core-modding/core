using Hashlink.Marshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkDynObj(HashlinkObjPtr ptr) : HashlinkFieldObject<HL_vdynamic>(ptr)
    {
        public HashlinkDynObj() : this(HashlinkObjPtr.Get(hl_alloc_dynobj()))
        {
        
        }

        public override object? GetFieldValue( int hashedName )
        {
            return HashlinkMarshal.ConvertHashlinkObject(
                hl_dyn_getp(TypedRef, hashedName, InternalTypes.hlt_dyn));
        }
        public override void SetFieldValue( int hashedName, object? value )
        {
            nint v;
            HashlinkMarshal.WriteData(&v, value, HashlinkMarshal.Module.KnownTypes.Dynamic);
            hl_dyn_setp(TypedRef, hashedName, InternalTypes.hlt_dyn, (void*)v);
        }
    }
}
