using Hashlink.Marshaling;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.DynamicAccess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Proxy.Objects
{
    public unsafe class HashlinkDynObj(HashlinkObjPtr ptr) : HashlinkFieldObject<HL_vdynamic>(ptr)
    {
        public HashlinkDynObj() : this(HashlinkObjPtr.Get(hl_alloc_dynobj()))
        {
            Debug.Assert(Handle != null);
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

        public override bool TryGetMember( GetMemberBinder binder, out object? result )
        {
            result = DynamicAccessUtils.AsDynamic(GetFieldValue(binder.Name));
            return true;
        }
        public override bool TryInvokeMember( InvokeMemberBinder binder, object?[]? args, out object? result )
        {
            var name = binder.Name;
            var func = GetFieldValue(name);
            if (func == null)
            {
                result = null;
                return false;
            }
            result = DynamicAccessUtils.AsDynamic(((HashlinkClosure)func).DynamicInvoke(args));
            return true;
        }
        public override bool TrySetMember( SetMemberBinder binder, object? value )
        {
            SetFieldValue(binder.Name, value);
            return true;
        }
    }
}
