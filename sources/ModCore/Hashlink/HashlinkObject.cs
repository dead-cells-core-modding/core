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
    public unsafe class HashlinkObject : DynamicObject
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
            [FieldOffset(0)]
            public HL_vvirtual vvirtual;
            [FieldOffset(0)]
            public HL_vclosure vclosure;
            [FieldOffset(0)]
            public HL_enum venum;
            [FieldOffset(0)]
            public HL_vdynobj vdynobj;
            //Common Field
            [FieldOffset(0)]
            public HL_type* type;
        }
        private readonly HashlinkObjRef @ref;
        private ObjectBox* hl_vbox;
        private readonly HL_type* hl_type = null;
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
                else if(IsRef)
                {
                    return AsDynamic->val.ptr;
                }
                else if(IsEnum)
                {
                    return &AsEnum->index;
                }
                else if(IsAbstract)
                {
                    return &AsDynamic->val.ptr;
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
        public HL_vclosure* AsClosure => (HL_vclosure*)hl_vbox;
        public HL_array* AsArray => (HL_array*)hl_vbox;
        public HL_vdynamic* AsDynamic => (HL_vdynamic*)hl_vbox;
        public HL_vstring* AsString => (HL_vstring*)hl_vbox;
        public HL_vdynobj* AsDynObj => (HL_vdynobj*)hl_vbox;
        public HL_enum* AsEnum => (HL_enum*)hl_vbox;
        public HL_vvirtual* AsVirtual => (HL_vvirtual*)hl_vbox;
        public HL_type* HashlinkType => hl_type;
        public HL_type* RefType => IsRef ? hl_type->data.tparam : throw new InvalidOperationException();
        public object? RefValue
        {
            get
            {
                if(!IsRef)
                {
                    throw new InvalidOperationException();
                }
                return HashlinkUtils.GetData(ValuePointer, RefType);
            }
            set
            {
                HashlinkUtils.SetData(ValuePointer, RefType, value);
            }
        }
        public bool IsInvalid => hl_vbox == null;
        public bool IsRef => hl_type->kind == HL_type.TypeKind.HREF;
        public bool IsString => hl_type == HashlinkUtils.HLType_String;
        public bool IsArray => hl_type->kind == HL_type.TypeKind.HARRAY;
        public bool IsEnum => hl_type->kind == HL_type.TypeKind.HENUM;
        public bool IsVirtual => hl_type->kind == HL_type.TypeKind.HVIRTUAL;
        public bool IsDynObj => hl_type->kind == HL_type.TypeKind.HDYNOBJ;
        public bool IsClosure => hl_type->kind == HL_type.TypeKind.HFUN;
        public bool IsAbstract => hl_type->kind == HL_type.TypeKind.HABSTRACT;
        public bool IsDynamic => !IsString && (
            hl_type->kind == HL_type.TypeKind.HOBJ ||
            !hl_type->kind.IsPointer()
            );
        public HashlinkObject(HL_type* type)
        {
            hl_type = type;
            if (IsEnum)
            {
                hl_vbox = (ObjectBox*) hl_alloc_enum(type);
            }
            else if (IsVirtual)
            {
                hl_vbox = (ObjectBox*)hl_alloc_virtual(type);
            }
            else if(IsDynObj)
            {
                hl_vbox = (ObjectBox*)hl_alloc_dynobj();
            }
            else if (IsDynamic)
            {
                hl_vbox = (ObjectBox*) hl_alloc_dynamic(type);
                hl_vbox->vdynamic.val.ptr = hl_alloc_obj(type);
            }
            else if(IsString)
            {
                hl_vbox = (ObjectBox*)hl_alloc_dynamic(type);
                AsString->type = type;
            }
            else if(IsRef)
            {
                hl_vbox = (ObjectBox*)hl_gc_alloc_gen(type,
                    sizeof(HL_vdynamic) + hl_type_size(type->data.tparam),
                    HL_Alloc_Flags.MEM_KIND_DYNAMIC);
                hl_vbox->type = type;
                AsDynamic->val.ptr = AsDynamic + 1;
            }
            else if(IsClosure)
            {
                hl_vbox = (ObjectBox*)hl_alloc_closure_ptr(type, null, null);
            }
            else
            {
                throw new NotSupportedException($"Unknown type kind '{type->kind}'");
            }
            @ref = HashlinkObjRef.RegisterRef(this);
            hl_add_root(hl_vbox);
        }
        public static HashlinkObject CreateArray(HL_type* type, int len)
        {
            var array = hl_alloc_array(type, len);

            return FromHashlink((HL_vdynamic*) array);
        }
        protected HashlinkObject(ObjectBox* v, HL_type* type)
        {
            if(!HashlinkUtils.IsValidHLObject(v))
            {
                throw new InvalidOperationException();
            }
            hl_vbox = v;
            if (type == null)
            {
                type = v->type;
            }
            hl_type = type;
            if (v != null)
            {
                hl_add_root(hl_vbox);
            }
            @ref = HashlinkObjRef.RegisterRef(this);
        }
        public static HashlinkObject FromHashlink(HL_vdynobj* v)
        {
            return FromHashlink((ObjectBox*)v);
        }
        public static HashlinkObject FromHashlink(HL_vvirtual* v)
        {
            return FromHashlink((ObjectBox*)v);
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
        public static HashlinkObject FromHashlinkInternal(void* v)
        {
            if (!HashlinkUtils.IsValidHLObject(v))
            {
                throw new InvalidOperationException();
            }

            return new HashlinkObject((ObjectBox*) v, null);
        }
        public static HashlinkObject FromHashlink(ObjectBox* v)
        {
            var oref = HashlinkObjRef.GetRef((nint)v);
            return oref.cachedObj;
        }
        public dynamic Dynamic => this;
        
        ~HashlinkObject()
        {
            if (hl_vbox != null)
            {
                hl_remove_root(hl_vbox);
                hl_vbox = null;
            }
        }

        private void* GetFieldPtr(int hash, out HL_type* type)
        {
            var result = hl_obj_lookup(&hl_vbox->vdynamic, hash, out type);
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
            var data = (nint)ValuePointer + hl_type_size(array->at) * idx;
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
            var data = (nint)ValuePointer + hl_type_size(array->at) * idx;
            result = HashlinkUtils.GetData((void*)data, array->at);
            return true;
        }
        [StackTraceHidden]
        public override bool TryConvert(ConvertBinder binder, out object? result)
        {
            if (binder.Type == typeof(HashlinkFunc))
            {
                if (!IsClosure)
                {
                    result = null;
                    return false;
                }
                result = new HashlinkFunc(HashlinkUtils.GetFunction(hl_type->data.func), AsClosure->fun);
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
            if (IsRef)
            {
                result = RefValue;
            }
            else
            {
                result = HashlinkUtils.GetData(ValuePointer, hl_vbox->type);
            }
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
                    result = FromHashlink(hl_obj_get_field(AsDynamic, hash));
                }
                return true;
            }
            else //Function
            {
                if (type == null)
                {
                    var f = HashlinkUtils.GetFunction(hl_type, name);
                    if (f != null)
                    {
                        type = f->type;
                    }
                }
                if (type != null)
                {
                    var robj = new HashlinkObject(type);
                    result = robj;
                    var func = HashlinkUtils.GetFunction(type->data.func);
                    robj.AsClosure->fun = HashlinkUtils.GetFunctionNativePtr(func);
                    robj.AsClosure->hasValue = HashlinkUtils.HasThis(func) ? 1 : 0;
                    if(robj.AsClosure->hasValue != 0)
                    {
                        robj.AsClosure->value = hl_vbox;
                    }
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
            if (IsClosure)
            {
                if (AsClosure->hasValue > 0)
                {
                    return func.CallDynamic([(nint)AsClosure->value, .. args]);
                }
                else
                {
                    return func.CallDynamic(args);
                }
            }
            if (func.HasThis)
            {
                return func.CallDynamic([(nint)hl_vbox, .. args]);
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
            if(type == null)
            {
                var f = HashlinkUtils.GetFunction(hl_type, binder.Name);
                if (f == null)
                {
                    result = null;
                    return false;
                }
                type = f->type;
            }
            if(type->kind != HL_type.TypeKind.HFUN)
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
            if(!IsClosure)
            {
                result = null;
                return false;
            }
            result = InvokeFunction(new(HashlinkUtils.GetFunction(hl_type->data.func), AsClosure->fun), args);
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
