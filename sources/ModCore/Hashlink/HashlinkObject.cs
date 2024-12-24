using Hashlink;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ModCore.Hashlink
{
    public unsafe class HashlinkObject : DynamicObject, IDisposable
    {
        private HL_vdynamic* hl_vdy;
        private HL_type* hl_type = null;

        public HL_vdynamic* HashlinkValue => hl_vdy;
        public HL_type* HashlinkType => hl_type;
        public bool IsInvalid => hl_vdy == null;

        public HashlinkObject(HL_type* type)
        {
            hl_type = type;

            hl_vdy = HashlinkNative.hl_alloc_dynamic(type);

            if(type->kind == HL_type.TypeKind.HOBJ)
            {
                hl_vdy->val.ptr = HashlinkNative.hl_alloc_obj(type);
            }
            else if(type->kind == HL_type.TypeKind.HENUM)
            {
                hl_vdy->val.ptr = HashlinkNative.hl_alloc_enum(type);
            }
            else
            {
                throw new NotSupportedException($"Unknown type kind '{type->kind}'");
            }
            HashlinkNative.hl_add_root(hl_vdy);
        }
        private HashlinkObject(HL_vdynamic* v)
        {
            hl_vdy = v;
            hl_type = v->type;
            HashlinkNative.hl_add_root(hl_vdy);
        }
        public static HL_vdynamic* ToHashlink(HashlinkObject obj)
        {
            return obj.HashlinkValue;
        }
        public static HashlinkObject FromHashlink(HL_vdynamic* v)
        {
            return new HashlinkObject(v);
        }

        
        ~HashlinkObject()
        {
            Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if(hl_vdy != null)
            {
                HashlinkNative.hl_remove_root(hl_vdy);
                hl_vdy = null;
            }
        }

        private void* GetFieldPtr(int hash, out HL_type* type)
        {
            return HashlinkNative.hl_obj_lookup(hl_vdy, hash, out type);
        }

        public override bool TryConvert(ConvertBinder binder, out object? result)
        {
            result = HashlinkUtils.GetData(&hl_vdy->val, hl_vdy->type);
            return result != null;
        }
        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            var hash = HashlinkUtils.HLHash(binder.Name);
            var ptr = GetFieldPtr(hash, out var type);
            if(ptr != null)
            {
                if (!type->kind.IsPointer())
                {
                    result = HashlinkUtils.GetData(ptr, type);
                }
                else
                {
                    result = FromHashlink(HashlinkNative.hl_obj_get_field(hl_vdy, hash));
                }
                return true;
            }
            result = null;
            return false;
        }
        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            var hash = HashlinkUtils.HLHash(binder.Name);
            var ptr = GetFieldPtr(hash, out var type);
            if(ptr == null)
            {
                return false;
            }
            if (value == null)
            {
                *(nint*)ptr = 0;
                return true;
            }
            var t = value.GetType();
            if(t.IsPrimitive || t.IsPointer || value is HashlinkObject)
            {
                HashlinkUtils.SetData(ptr, type, value);
                return true;
            }
            return false;
        }
    }
}
