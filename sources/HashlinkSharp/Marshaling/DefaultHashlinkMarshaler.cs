using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using Hashlink.Proxy.Values;
using Hashlink.Reflection.Types;
using Hashlink.Reflection.Types.Special;
using System.Runtime.CompilerServices;

namespace Hashlink.Marshaling
{
    public unsafe class DefaultHashlinkMarshaler : IHashlinkMarshaler
    {
        public static IHashlinkMarshaler Instance { get; } = new DefaultHashlinkMarshaler(false);
        public static IHashlinkMarshaler IgnoreCustomMarshalerInstance { get; } = new DefaultHashlinkMarshaler(true);

        private readonly bool ignoreCustomMarshaler;

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

            if (type is null)
            {
                if (value is not null)
                {
                    type = HashlinkMarshal.GetHashlinkType(value.GetType());
                }
            }
            var typeKind = type?.TypeKind;
            if (typeKind is null || value is null)
            {
                return false;
            }


            if (value is Delegate del && type is HashlinkFuncType ft)
            {
                *(nint*)target = new HashlinkClosure(ft, del).HashlinkPointer;
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
                HashlinkMarshal.WriteData(&dptr->val, value, vt);
                *(nint*)target = (nint)dptr;
            }
            else if (typeKind is TypeKind.HNULL)
            {
                var vt = ((HashlinkNullType)type!).ValueType;
                var dptr = hl_alloc_dynamic(
                    vt.NativeType
                    );
                HashlinkMarshal.WriteData(&dptr->val, value, vt);
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

            return kind switch
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
        }
    }
}
