using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using Hashlink.Proxy.Values;
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
        public virtual object? TryReadData( void* target, TypeKind? typeKind )
        {
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
                case TypeKind.HABSTRACT:
                    return (object?)*(nint*)target;
                case TypeKind.HREF:
                    return (object?)*(nint*)target;
                default:
                    if (typeKind?.IsPointer() ?? false)
                    {
                        return (object?)HashlinkMarshal.ConvertHashlinkObject(*(void**)target);
                    }
                    else
                    {
                        return null;
                    }
            }
        }

        public virtual bool TryWriteData( void* target, object? value, TypeKind? type )
        {
            if (!ignoreCustomMarshaler && value is IHashlinkCustomMarshaler customMarshaler)
            {
                return customMarshaler.TryWriteData(target, type);
            }

            if (value is IHashlinkPointer hlptr)
            {
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
                    type = HashlinkMarshal.GetTypeKind(value.GetType());
                }
            }
            if (type is null || value is null)
            {
                return false;
            }


            if (type is TypeKind.HUI8)
            {
                *(byte*)target = Utils.ForceUnbox<byte>(value);
            }
            else if (type is TypeKind.HUI16)
            {
                *(ushort*)target = Utils.ForceUnbox<ushort>(value);
            }
            else if (type is TypeKind.HI32)
            {
                *(int*)target = Utils.ForceUnbox<int>(value);
            }
            else if (type is TypeKind.HI64)
            {
                *(long*)target = Utils.ForceUnbox<long>(value);
            }
            else if (type is TypeKind.HF32)
            {
                *(float*)target = Utils.ForceUnbox<float>(value);
            }
            else if (type is TypeKind.HF64)
            {
                *(double*)target = value is float floatVal ? (double)floatVal : Utils.ForceUnbox<double>(value);
            }
            else if (type is TypeKind.HBOOL)
            {
                *(byte*)target = (byte)(Utils.ForceUnbox<bool>(value) ? 1 : 0);
            }
            else if (type is TypeKind.HREF)
            {
                *(nint*)target = (nint)value;
            }
            else if (type is TypeKind.HABSTRACT)
            {
                *(nint*)target = (nint)value;
            }
            else if (type is TypeKind.HBYTES)
            {
                if (value is string str)
                {
                    return TryWriteData(target, new HashlinkBytes(str).Value, type);
                }
                else
                {
                    *(nint*)target = Utils.ForceUnbox<nint>(value);
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        public virtual HashlinkObj? TryConvertHashlinkObject( void* target )
        {
            var ptr = HashlinkObjPtr.Get(target);

            var kind = ptr.TypeKind;

            return kind switch
            {
                TypeKind.HUI8 => new HashlinkTypedValue<byte>(ptr),
                TypeKind.HUI16 => new HashlinkTypedValue<ushort>(ptr),
                TypeKind.HI32 => new HashlinkTypedValue<int>(ptr),
                TypeKind.HI64 => new HashlinkTypedValue<long>(ptr),
                TypeKind.HF32 => new HashlinkTypedValue<float>(ptr),
                TypeKind.HF64 => new HashlinkTypedValue<double>(ptr),
                TypeKind.HBOOL => new HashlinkTypedValue<bool>(ptr),
                TypeKind.HBYTES => new HashlinkBytes(ptr),
                TypeKind.HVIRTUAL => new HashlinkVirtual(ptr),
                TypeKind.HOBJ => ptr.Type == NETExcepetionError.ErrorType ? new HashlinkNETExceptionObj(ptr) : new HashlinkObject(ptr),
                TypeKind.HABSTRACT => new HashlinkAbstract(ptr),
                TypeKind.HFUN => new HashlinkClosure(ptr),
                TypeKind.HREF => new HashlinkRef(ptr),
                TypeKind.HENUM => new HashlinkEnum(ptr),
                _ => throw new InvalidOperationException($"Unrecognized type {kind}")
            };
        }
    }
}
