using Hashlink;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ModCore.Hashlink
{
    public unsafe class HashlinkObject : DynamicObject, IDisposable
    {
        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct ObjectBox
        {
            [FieldOffset(0)]
            public HL_vdynamic vdynamic;
            [FieldOffset(0)]
            public HL_vstring vstring;
            [FieldOffset(0)]
            public HL_array varray;

            //Common Field
            [FieldOffset(0)]
            public HL_type* type;
        }
        private ObjectBox* hl_vbox;
        private readonly HL_type* hl_type = null;
        private HashlinkFunc? cached_func;
        public void* ValuePointer
        {
            get
            {
                if(IsArray)
                {
                    return &hl_vbox->varray + 1;
                }
                else if(IsString)
                {
                    return hl_vbox->vstring.bytes;
                }
                else
                {
                    if(hl_type->kind.IsPointer())
                    {
                        return hl_vbox->vdynamic.val.ptr;
                    }
                    else
                    {
                        return &hl_vbox->vdynamic.val.ptr;
                    }
                }
            }
        }
        public ObjectBox* HashlinkObj => hl_vbox;
        public HL_array* AsArray => (HL_array*)hl_vbox;
        public HL_vdynamic* AsDynamic => (HL_vdynamic*)hl_vbox;
        public HL_vstring* AsString => IsString ? (HL_vstring*)hl_vbox : throw new InvalidOperationException();
        public HL_type* HashlinkType => hl_type;
        public bool IsInvalid => hl_vbox == null;
        public bool IsString => hl_type == HashlinkUtils.HLType_String;
        public bool IsArray => hl_type->kind == HL_type.TypeKind.HARRAY;
        public bool IsDynamic => !IsString && !IsString;
        public HashlinkObject(HL_type* type)
        {
            hl_type = type;

            if (IsDynamic)
            {
                hl_vbox = (ObjectBox*) HashlinkNative.hl_alloc_dynamic(type);

                if (type->kind == HL_type.TypeKind.HOBJ)
                {
                    hl_vbox->vdynamic.val.ptr = HashlinkNative.hl_alloc_obj(type);
                }
                else if (type->kind == HL_type.TypeKind.HENUM)
                {
                    hl_vbox->vdynamic.val.ptr = HashlinkNative.hl_alloc_enum(type);
                }
                else if (type->kind == HL_type.TypeKind.HVIRTUAL)
                {
                    hl_vbox->vdynamic.val.ptr = HashlinkNative.hl_alloc_virtual(type);
                }
                else
                {
                    throw new NotSupportedException($"Unknown type kind '{type->kind}'");
                }
            }
            else
            {
                throw new NotSupportedException($"Unknown type kind '{type->kind}'");
            }
            HashlinkNative.hl_add_root(hl_vbox);
        }
        public static HashlinkObject CreateArray(HL_type* type, int len)
        {
            var array = HashlinkNative.hl_alloc_array(type, len);

            return FromHashlink((HL_vdynamic*) array);
        }
        private HashlinkObject(ObjectBox* v, HL_type* type)
        {
            hl_vbox = v;
            if (type == null)
            {
                type = v->type;
            }
            hl_type = type;
            if (v != null)
            {
                HashlinkNative.hl_add_root(hl_vbox);
            }
        }
        public static HashlinkObject FromHashlink(HL_array* v)
        {
            return FromHashlink((ObjectBox*)v);
        }
        public static HashlinkObject FromHashlink(HL_vstring* v)
        {
            return FromHashlink((ObjectBox*)v);
        }
        public static HashlinkObject FromHashlink(HL_vdynamic* v)
        {
            return FromHashlink((ObjectBox*)v);
        }
        public static HashlinkObject FromHashlink(ObjectBox* v)
        {
            return new HashlinkObject(v, null);
        }

        public dynamic Dynamic => this;
        
        ~HashlinkObject()
        {
            Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if(hl_vbox != null)
            {
                HashlinkNative.hl_remove_root(hl_vbox);
                hl_vbox = null;
            }
        }

        private void* GetFieldPtr(int hash, out HL_type* type)
        {
            var result = HashlinkNative.hl_obj_lookup(&hl_vbox->vdynamic, hash, out type);
            if(result == null &&
                hl_type->kind == HL_type.TypeKind.HOBJ)
            {
                //Find function
                for(int i = 0; i < hl_type->data.obj->nproto; i++)
                {
                    var p = hl_type->data.obj->proto + i;
                    if(p->hashed_name == hash)
                    {
                        var f = HashlinkUtils.Module->code->functions +
                        HashlinkUtils.Module->functions_indexes[p->findex];
                        type = f->type;
                        return null;
                    }

                }
            }
            return result;
        }
        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object? value)
        {
            if (indexes.Length != 1)
            {
                return false;
            }
            var index = indexes[0];
            if (index is string fName)
            {
                return SetMemberImpl(fName, value);
            }
            if (hl_type->kind != HL_type.TypeKind.HARRAY)
            {
                return false;
            }
            var idx = (int)index;
            var array = AsArray;
            if (array->size <= idx)
            {
                return false;
            }
            var data = (nint)ValuePointer + HashlinkNative.hl_type_size(array->at) * idx;
            HashlinkUtils.SetData((void*)data, array->at, value);
            return true;
        }
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object? result)
        {
            result = null;
            if (indexes.Length != 1)
            {
                return false;
            }
            var index = indexes[0];
            if(index is string fName)
            {
                return GetMemberImpl(fName, out result);
            }
            if(hl_type->kind != HL_type.TypeKind.HARRAY)
            {
                return false;
            }
            var idx = (int)index;
            var array = AsArray;
            if (array->size <= idx)
            {
                return false;
            }
            var data = (nint)ValuePointer + HashlinkNative.hl_type_size(array->at) * idx;
            result = HashlinkUtils.GetData((void*)data, array->at);
            return true;
        }
        [StackTraceHidden]
        public override bool TryConvert(ConvertBinder binder, out object? result)
        {
            if (binder.Type == typeof(HashlinkFunc))
            {
                if (hl_type->kind != HL_type.TypeKind.HFUN)
                {
                    result = null;
                    return false;
                }
                cached_func ??= new(HashlinkUtils.GetFunction(hl_type->data.func));
                result = cached_func;
                return true;
            }
            else if (binder.Type == typeof(string))
            {
                if(hl_type == HashlinkUtils.HLType_String)
                {
                    result = new string(AsString->bytes);
                    return true;
                }
                else
                {
                    result = ToString();
                    return true;
                }
            }
            result = HashlinkUtils.GetData(&AsDynamic->val, hl_vbox->type);
            return result != null;
        }
        private bool GetMemberImpl(string name, out object? result)
        {
            var hash = HashlinkUtils.HLHash(name);
            var ptr = GetFieldPtr(hash, out var type);
            if (ptr != null)
            {
                if (!type->kind.IsPointer())
                {
                    result = HashlinkUtils.GetData(ptr, type);
                }
                else
                {
                    result = FromHashlink(HashlinkNative.hl_obj_get_field(AsDynamic, hash));
                }
                return true;
            }
            else
            {
                if (type != null)
                {
                    result = new HashlinkObject(null, type);
                    return true;
                }
            }

            result = null;
            return false;
        }
        [StackTraceHidden]
        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            return GetMemberImpl(binder.Name, out result);
        }
        public override string ToString()
        {
            if(IsString)
            {
                return new string(AsString->bytes);
            }
            if(IsArray)
            {
                return $"[Array {HashlinkUtils.GetTypeString(AsArray->at)} (len {AsArray->size})]";
            }
            if(!hl_type->kind.IsPointer())
            {
                return HashlinkUtils.GetData(&AsDynamic->val.ptr, hl_type)?.ToString() ?? "null";
            }
            if(hl_type->kind == HL_type.TypeKind.HOBJ &&
                hl_type->data.obj->rt->toStringFun != null)
            {
                return new(hl_type->data.obj->rt->toStringFun(AsDynamic));
            }
            else
            {
                return HashlinkUtils.GetTypeString(hl_type);
            }
        }
        [StackTraceHidden]
        private object? InvokeFunction(HashlinkFunc func, object?[]? args)
        {
            if(func.HasThis)
            {
                if (IsString)
                {
                    return func.CallDynamic([(nint)(&hl_vbox->vstring.bytes), .. args]);
                }
                else
                {
                    return func.CallDynamic([(nint)ValuePointer, .. args]);
                }
            }
            else
            {
                return func.CallDynamic(args);
            }
        }
        [StackTraceHidden]
        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[]? args, out object? result)
        {
            var hash = HashlinkUtils.HLHash(binder.Name);
            var ptr = GetFieldPtr(hash, out var type);
            if(type == null || type->kind != HL_type.TypeKind.HFUN)
            {
                result = null;
                return false;
            }
            result = InvokeFunction(new HashlinkFunc(HashlinkUtils.GetFunction(type->data.func)), args);
            return true;
        }
        [StackTraceHidden]
        public override bool TryInvoke(InvokeBinder binder, object?[]? args, out object? result)
        {
            if(hl_type->kind != HL_type.TypeKind.HFUN)
            {
                result = null;
                return false;
            }
            cached_func ??= new(HashlinkUtils.GetFunction(hl_type->data.func));
            result = InvokeFunction(cached_func, args);
            return true;
        }
        private bool SetMemberImpl(string name, object? value)
        {
            var hash = HashlinkUtils.HLHash(name);
            var ptr = GetFieldPtr(hash, out var type);
            if (ptr == null)
            {
                return false;
            }
            if (value == null)
            {
                *(nint*)ptr = 0;
                return true;
            }
            var t = value.GetType();
            if (t.IsPrimitive || t.IsPointer ||
                value is HashlinkObject ||
                value is string)
            {
                HashlinkUtils.SetData(ptr, type, value);
                return true;
            }
            return false;
        }
        [StackTraceHidden]
        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            return SetMemberImpl(binder.Name, value);
        }
    }
}
