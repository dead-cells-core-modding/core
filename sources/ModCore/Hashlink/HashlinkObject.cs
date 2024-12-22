using Hashlink;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Hashlink
{
    public unsafe class HashlinkObject : DynamicObject, IDisposable
    {
        private static readonly ConcurrentDictionary<nint, WeakReference<HashlinkObject>> hlobj2obj = [];

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
        public static nint ToHashlink(HashlinkObject obj)
        {
            return (nint)obj.HashlinkValue;
        }
        public static HashlinkObject FromHashlink(HL_vdynamic* v)
        {
            if(!hlobj2obj.TryGetValue((nint)v, out var r) || !r.TryGetTarget(out var obj))
            {
                obj = new HashlinkObject(v);
                hlobj2obj[(nint)v] = new(obj);
            }
            
            return obj;
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
                if(hlobj2obj.TryRemove((nint)hl_vdy, out _))
                {
                    HashlinkNative.hl_remove_root(hl_vdy);
                }
                hl_vdy = null;
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            
            return base.TryGetMember(binder, out result);
        }
    }
}
