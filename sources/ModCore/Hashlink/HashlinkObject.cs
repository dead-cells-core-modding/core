using Hashlink;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Hashlink
{
    public unsafe class HashlinkObject : IDisposable 
    {
        private static readonly ConcurrentDictionary<Type, nint> type2hltype = [];
        private static readonly ConcurrentDictionary<nint, HashlinkObject> hlobj2obj = [];
        private static readonly ConcurrentDictionary<nint, Type> hltype2type = [];

        private void* hl_obj;
        private HL_type* hl_type = null;

        public void* HashlinkValue => hl_obj;
        public HL_type* HashlinkType => GetHashlinkType();

        private static nint GetHashlinkFromType(Type type)
        {
            if(type2hltype.TryGetValue(type, out var result))
            {
                return result;
            }
            var attr = type.GetCustomAttribute<HashlinkMetadataRef>();
            if(attr == null)
            {
                if(type.BaseType != null)
                {
                    result = GetHashlinkFromType(type.BaseType);
                }
                else
                {
                    result = nint.Zero;
                }
            }
            else
            {
                result = (nint)HashlinkUtils.FindTypeFromName(attr.Name);
            }
            type2hltype[type] = result;

            return result;
        }
        public HL_type* GetHashlinkType()
        {
            if (hl_type != null)
            {
                return hl_type;
            }
            return hl_type = (HL_type*)GetHashlinkFromType(GetType());
        }

        public HashlinkObject()
        {
            
        }
        internal void BindHashlinkObject(HL_vdynamic* v)
        {
            if(hl_obj != null)
            {
                return;
            }
            hl_obj = v;
            hl_type = v->type;
            HashlinkNative.hl_add_root(hl_obj);
        }
        public static nint ToHashlink(HashlinkObject obj)
        {
            return (nint)obj.HashlinkValue;
        }
        public static HashlinkObject FromHashlink(HL_vdynamic* v)
        {
            if(!hlobj2obj.TryGetValue((nint)v, out var obj))
            {
                obj = new HashlinkObject();
                obj.BindHashlinkObject(v);
                hlobj2obj[(nint)v] = obj;
            }
            
            return obj;
        }
        protected void Create()
        {
            if(hl_obj == null)
            {
                var type = GetHashlinkType();
                if(type->kind == HL_type.TypeKind.HOBJ)
                {
                    hl_obj = HashlinkNative.hl_alloc_obj(type);
                }
                else if(type->kind == HL_type.TypeKind.HENUM)
                {
                    hl_obj = HashlinkNative.hl_alloc_enum(type);
                }
                HashlinkNative.hl_add_root(hl_obj);
            }
        }

        
        ~HashlinkObject()
        {
            Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if(hl_obj != null)
            {
                if(hlobj2obj.TryRemove((nint)hl_obj, out _))
                {
                    HashlinkNative.hl_remove_root(hl_obj);
                }
                hl_obj = null;
            }
        }

        public bool IsInvalid => hl_obj == null;
    }
}
