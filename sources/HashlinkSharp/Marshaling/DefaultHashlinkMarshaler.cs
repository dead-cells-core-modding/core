using Hashlink.Events.Interfaces;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using Hashlink.Proxy.Values;
using Hashlink.Reflection.Types;
using Hashlink.Reflection.Types.Special;
using ModCore.Collections;
using ModCore.Events;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hashlink.Marshaling
{
    public unsafe class DefaultHashlinkMarshaler : IHashlinkMarshaler
    {
        public static IHashlinkMarshaler Instance { get; } = new DefaultHashlinkMarshaler(false);
        public static IHashlinkMarshaler IgnoreCustomMarshalerInstance { get; } = new DefaultHashlinkMarshaler(true);

        private readonly bool ignoreCustomMarshaler;


        private readonly ConcurrentDictionary<Type, HashlinkFuncType> customDelegateFuncType = [];
        private struct CustomFuncType
        {
            public HL_type type;
            public HL_type_func func;
        }
        private readonly PinnedArrayList<CustomFuncType> customDelegateFuncList = new();

        private HashlinkFuncType CreateCustomDelegateFuncType( Type delType )
        {
            ref CustomFuncType f = ref customDelegateFuncList.Add(new());
            f.type.kind = TypeKind.HFUN;
            f.type.data.func = (HL_type_func*)Unsafe.AsPointer(ref f.func);

            var delInvoke = delType.GetMethod("Invoke");

            Debug.Assert(delInvoke != null);

            var p = delInvoke.GetParameters();

            f.func.ret = GetHashlinkTypeNoNull(delInvoke.ReturnType).NativeType;

            var args = (HL_type**)NativeMemory.AllocZeroed((nuint)(p.Length * sizeof(HL_type*)));

            for (int i = 0; i < p.Length; i++)
            {
                args[i] = GetHashlinkTypeNoNull(p[i].ParameterType).NativeType;
            }

            f.func.args = args;
            f.func.nargs = p.Length;

            return HashlinkMarshal.Module.GetMemberFrom<HashlinkFuncType>(Unsafe.AsPointer(ref f.type));
        }

        private static void WriteDynamicValue( HL_vdynamic* dyn, object val, HashlinkType type )
        {
            var b = (byte*)&dyn->val;
            HashlinkMarshal.WriteData(b, val, type);
        }

        protected DefaultHashlinkMarshaler( bool ignoreCustomMarshaler )
        {
            this.ignoreCustomMarshaler = ignoreCustomMarshaler;
        }
        public virtual object? TryReadData( void* target, HashlinkType? type )
        {
            var typeKind = type?.TypeKind;
            switch (typeKind)
            {
                case null:
                    return null;
                case TypeKind.HBOOL:
                    return (object?)(*(byte*)target == 1);
                case TypeKind.HUI8:
                    return (object?)*(byte*)target;
                case TypeKind.HUI16:
                    return (object?)*(ushort*)target;
                case TypeKind.HI32:
                    return (object?)*(int*)target;
                case TypeKind.HI64:
                    return (object?)*(long*)target;
                case TypeKind.HF32:
                    return (object?)*(float*)target;
                case TypeKind.HF64:
                    return (object?)*(double*)target;
                case TypeKind.HTYPE:
                case TypeKind.HABSTRACT:
                case TypeKind.HBYTES:
                    return (object?)*(nint*)target;
                case TypeKind.HREF:
                    return (object?)*(nint*)target;
                case TypeKind.HNULL:
                default:
                    if (type?.IsPointer ?? false)
                    {
                        return HashlinkMarshal.ConvertHashlinkObject(*(void**)target);
                    }
                    else
                    {
                        return null;
                    }
            }
        }

        public virtual bool TryWriteData( void* target, object? value, HashlinkType? type )
        {

            if (!ignoreCustomMarshaler && value is IHashlinkCustomMarshaler customMarshaler)
            {
                return customMarshaler.TryWriteData(target, type);
            }

            if (value is IHashlinkPointer hlptr)
            {
                HashlinkMarshal.MarkUsed(hlptr);
                Unsafe.WriteUnaligned(target, hlptr.HashlinkPointer);
                return true;
            }


            if (value is null)
            {
                Unsafe.WriteUnaligned(target, (nint)0);
                return true;
            }

            HashlinkType? valType = null;

            if (value is not null)
            {
                valType = HashlinkMarshal.GetHashlinkType(value.GetType(), this);
            }

            if (type is null)
            {
                type = valType;
            }
            else if (valType != null && type.TypeKind == TypeKind.HDYN && !valType.IsValueType)
            {
                type = valType;
            }

            var typeKind = type?.TypeKind;
            if (typeKind is null || value is null)
            {
                return false;
            }

            if (value is Delegate del && type is HashlinkFuncType ft)
            {
                var cl = new HashlinkClosure(ft, del);
                * (nint*)target = cl.HashlinkPointer;
                Debug.Assert(HashlinkMarshal.ConvertHashlinkObject(HashlinkObjPtr.Get(*(nint*)target)) == cl);
                return true;
            }
            else if (value is string str && typeKind is not TypeKind.HBYTES)
            {
                *(nint*)target = new HashlinkString(str).HashlinkPointer;
                return true;
            }

            if (typeKind is TypeKind.HUI8)
            {
                *(byte*)target = Utils.ForceUnbox<byte>(value);
            }
            else if (typeKind is TypeKind.HUI16)
            {
                *(ushort*)target = Utils.ForceUnbox<ushort>(value);
            }
            else if (typeKind is TypeKind.HI32)
            {
                *(int*)target = Utils.ForceUnbox<int>(value);
            }
            else if (typeKind is TypeKind.HI64)
            {
                *(long*)target = Utils.ForceUnbox<long>(value);
            }
            else if (typeKind is TypeKind.HF32)
            {
                if (value is not float)
                {
                    *(float*)target = ((IConvertible)value).ToSingle(null);
                }
                else
                {
                    *(float*)target = (float)value;
                }
            }
            else if (typeKind is TypeKind.HF64)
            {
                if (value is not double)
                {
                    *(double*)target = ((IConvertible)value).ToDouble(null);
                }
                else
                {
                    *(double*)target = (double)value;
                }
            }
            else if (typeKind is TypeKind.HBOOL)
            {
                *(byte*)target = (byte)(Utils.ForceUnbox<bool>(value) ? 1 : 0);
            }
            else if (typeKind is TypeKind.HREF)
            {
                *(nint*)target = (nint)value;
            }
            else if (typeKind is TypeKind.HDYN)
            {
                var vt = HashlinkMarshal.GetHashlinkType(value.GetType()) ??
                    throw new InvalidOperationException();
                var dptr = hl_alloc_dynamic(
                    vt.NativeType
                    );

                WriteDynamicValue(dptr, value, vt);
                Debug.Assert(
                   value.Equals(HashlinkMarshal.ReadData(&dptr, HashlinkMarshal.Module.KnownTypes.Dynamic, null))
                   );

                *(nint*)target = (nint)dptr;
            }
            else if (typeKind is TypeKind.HNULL)
            {
                var vt = ((HashlinkNullType)type!).ValueType;
                var dptr = hl_alloc_dynamic(
                    vt.NativeType
                    );
               
                WriteDynamicValue(dptr, value, vt);
                Debug.Assert(
                    value.Equals(HashlinkMarshal.ReadData(&dptr, HashlinkMarshal.Module.KnownTypes.Dynamic, null))
                    );

                *(nint*)target = (nint)dptr;
            }
            else if (typeKind is TypeKind.HABSTRACT or TypeKind.HTYPE)
            {
                *(nint*)target = (nint)value;
            }
            else if (typeKind is TypeKind.HBYTES)
            {
                *(nint*)target = Utils.ForceUnbox<nint>(value);
            }
            else
            {
                return false;
            }
            return true;
        }

        private object GetObjectFromPtr( HashlinkObjPtr ptr )
        {
            if (ptr.Type == NETExcepetionError.ErrorType)
            {
                return new HashlinkNETExceptionObj(ptr);
            }
            else if (ptr.Type == HashlinkMarshal.Module.KnownTypes.String.NativeType)
            {
                return new HashlinkString(ptr);
            }
            return new HashlinkObject(ptr);
        }
        public virtual object? TryConvertHashlinkObject( void* target )
        {
            var ptr = HashlinkObjPtr.Get(target);

            var kind = ptr.TypeKind;

            var result = kind switch
            {
                <= TypeKind.HBYTES => HashlinkMarshal.ReadData(
                    &((HL_vdynamic*)target)->val, HashlinkMarshal.GetHashlinkType(ptr.Type)
                    ),
                TypeKind.HVIRTUAL => new HashlinkVirtual(ptr),
                TypeKind.HOBJ => GetObjectFromPtr(ptr),
                TypeKind.HABSTRACT => (nint)((HL_vdynamic*)target)->val.ptr,
                TypeKind.HFUN => new HashlinkClosure(ptr),
                TypeKind.HREF => (nint)((HL_vdynamic*)target)->val.ptr,
                TypeKind.HENUM => new HashlinkEnum(ptr),
                TypeKind.HARRAY => new HashlinkArray(ptr),
                TypeKind.HDYNOBJ => new HashlinkDynObj(ptr),
                TypeKind.HNULL or TypeKind.HDYN => HashlinkMarshal.ReadData(
                    &((HL_vdynamic*)target)->val, HashlinkMarshal.GetHashlinkType(ptr.Type->data.tparam)
                    ),

                _ => throw new InvalidOperationException($"Unrecognized type {kind}")
            };
            return result;
        }

        public HashlinkType GetHashlinkTypeNoNull( Type type )
        {
            return GetHashlinkType(type) ?? throw new InvalidOperationException();
        }

        public HashlinkType? GetHashlinkType( Type type )
        {
            var result = EventSystem.BroadcastEvent<IOnResolveHashlinkType, Type, HashlinkType>(type);
            if (result.HasValue)
            {
                return result.Value;
            }

            var kt = HashlinkMarshal.Module.KnownTypes;
            if (type == typeof(int) || type == typeof(uint))
            {
                return kt.I32;
            }
            else if (type == typeof(long) || type == typeof(ulong))
            {
                return kt.I64;
            }
            else if (type == typeof(float))
            {
                return kt.F32;
            }
            else if (type == typeof(double))
            {
                return kt.F64;
            }
            else if (type == typeof(byte) || type == typeof(sbyte))
            {
                return kt.I8;
            }
            else if (type == typeof(bool))
            {
                return kt.Bool;
            }
            else if (type == typeof(short) || type == typeof(ushort))
            {
                return kt.I16;
            }
            else if (type == typeof(void))
            {
                return kt.Void;
            }
            else if (type == typeof(nint))
            {
                return kt.Bytes;
            }
            else if (type.IsAssignableTo(typeof(Delegate)))
            {
                return customDelegateFuncType.GetOrAdd(type, CreateCustomDelegateFuncType);
            }
            else if (type.IsAssignableTo(typeof(IExtraDataItem)) ||
                type.IsAssignableTo(typeof(HashlinkObj)) ||
                type == typeof(object))
            {
                return kt.Dynamic;
            }
            return null;
        }
    }
}
