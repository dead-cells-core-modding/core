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
        public virtual object? TryReadData( void* target, HL_type.TypeKind? typeKind )
        {
            return typeKind == null
                ? null
                : typeKind == HL_type.TypeKind.HBOOL
                ? *(byte*)target == 1
                : typeKind == HL_type.TypeKind.HUI8
                    ? *(byte*)target
                    : typeKind == HL_type.TypeKind.HUI16
                                    ? *(ushort*)target
                                    : typeKind == HL_type.TypeKind.HI32
                                                    ? *(int*)target
                                                    : typeKind == HL_type.TypeKind.HI64
                                                                    ? *(long*)target
                                                                    : typeKind == HL_type.TypeKind.HF32
                                                                                    ? *(float*)target
                                                                                    : typeKind == HL_type.TypeKind.HF64
                                                                                                    ? *(double*)target
                                                                                                    : typeKind?.IsPointer() ?? false ? HashlinkMarshal.ConvertHashlinkObject(*(void**)target) : (object?)null;
        }

        public virtual bool TryWriteData( void* target, object? value, HL_type.TypeKind? type )
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


            if (type is HL_type.TypeKind.HUI8)
            {
                *(byte*)target = Utils.ForceUnbox<byte>(value);
            }
            else if (type is HL_type.TypeKind.HUI16)
            {
                *(ushort*)target = Utils.ForceUnbox<ushort>(value);
            }
            else if (type is HL_type.TypeKind.HI32)
            {
                *(int*)target = Utils.ForceUnbox<int>(value);
            }
            else if (type is HL_type.TypeKind.HI64)
            {
                *(long*)target = Utils.ForceUnbox<long>(value);
            }
            else if (type is HL_type.TypeKind.HF32)
            {
                *(float*)target = Utils.ForceUnbox<float>(value);
            }
            else if (type is HL_type.TypeKind.HF64)
            {
                *(double*)target = value is float floatVal ? (double)floatVal : Utils.ForceUnbox<double>(value);
            }
            else if (type is HL_type.TypeKind.HBOOL)
            {
                *(byte*)target = (byte)(Utils.ForceUnbox<bool>(value) ? 1 : 0);
            }
            else if (type is HL_type.TypeKind.HBYTES)
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



            return kind == HL_type.TypeKind.HUI8
                ? new HashlinkTypedValue<byte>(ptr)
                : kind == HL_type.TypeKind.HUI16
                    ? new HashlinkTypedValue<ushort>(ptr)
                    : kind == HL_type.TypeKind.HI32
                                    ? new HashlinkTypedValue<int>(ptr)
                                    : kind == HL_type.TypeKind.HI64
                                                    ? new HashlinkTypedValue<long>(ptr)
                                                    : kind == HL_type.TypeKind.HF32
                                                                    ? new HashlinkTypedValue<float>(ptr)
                                                                    : kind == HL_type.TypeKind.HF64
                                                                                    ? new HashlinkTypedValue<double>(ptr)
                                                                                    : kind == HL_type.TypeKind.HBOOL
                                                                                                    ? new HashlinkTypedValue<bool>(ptr)
                                                                                                    : kind == HL_type.TypeKind.HBYTES
                                                                                                                    ? new HashlinkBytes(ptr)
                                                                                                                    : kind == HL_type.TypeKind.HVIRTUAL
                                                                                                                                    ? new HashlinkVirtual(ptr)
                                                                                                                                    : kind == HL_type.TypeKind.HOBJ
                                                                                                                                                    ? ptr.Type == NETExcepetionError.ErrorType ? new HashlinkNETExceptionObj(ptr) : new HashlinkObject(ptr)
                                                                                                                                                    : kind == HL_type.TypeKind.HFUN
                                                                                                                                                                    ? new HashlinkClosure(ptr)
                                                                                                                                                                    : kind == HL_type.TypeKind.HREF ? (HashlinkObj)new HashlinkRef(ptr) : throw new InvalidOperationException($"Unrecognized type {kind}");
        }
    }
}
