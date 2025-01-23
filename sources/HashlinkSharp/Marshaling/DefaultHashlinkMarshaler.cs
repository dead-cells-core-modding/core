using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using Hashlink.Proxy.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Marshaling
{
    public unsafe class DefaultHashlinkMarshaler : IHashlinkMarshaler
    {
        public static IHashlinkMarshaler Instance { get; } = new DefaultHashlinkMarshaler(false);
        public static IHashlinkMarshaler IgnoreCustomMarshalerInstance { get; } = new DefaultHashlinkMarshaler(true);

        private readonly bool ignoreCustomMarshaler;

        protected DefaultHashlinkMarshaler(bool ignoreCustomMarshaler)
        {
            this.ignoreCustomMarshaler = ignoreCustomMarshaler;
        }
        public virtual object? TryReadData(void* target, HL_type.TypeKind? typeKind)
        {
            if(typeKind == null)
            {
                return null;
            }
            if(typeKind == HL_type.TypeKind.HBOOL)
            {
                return *(byte*)target == 1;
            }
            else if(typeKind == HL_type.TypeKind.HUI8)
            {
                return *(byte*)target;
            }
            else if(typeKind == HL_type.TypeKind.HUI16)
            {
                return *(ushort*)target;
            }
            else if(typeKind == HL_type.TypeKind.HI32)
            {
                return *(int*)target;
            }
            else if(typeKind == HL_type.TypeKind.HI64)
            {
                return *(long*)target;
            }
            else if(typeKind == HL_type.TypeKind.HF32)
            {
                return *(float*)target;
            }
            else if(typeKind == HL_type.TypeKind.HF64)
            {
                return *(double*)target;
            }
            else if(typeKind?.IsPointer() ?? false)
            {
                return HashlinkMarshal.ConvertHashlinkObject(*(void**)target);
            }
            else
            {
                return null;
            }
        }

        public virtual bool TryWriteData(void* target, object? value, HL_type.TypeKind? type)
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
            if(type is null || value is null)
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
                if (value is float floatVal)
                {
                    *(double*)target = floatVal;
                }
                else
                {
                    *(double*)target = Utils.ForceUnbox<double>(value);
                }
            }
            else if (type is HL_type.TypeKind.HBOOL)
            {
                *(byte*)target = (byte)(Utils.ForceUnbox<bool>(value) ? 1 : 0);
            }
            else if (type is HL_type.TypeKind.HBYTES)
            {
                *(nint*)target = Utils.ForceUnbox<nint>(value);
            }
            else
            {
                return false;
            }
            return true;
        }

        public virtual HashlinkObj? TryConvertHashlinkObject(void* target)
        {
            HL_type* type = *(HL_type**)target;
            HL_type.TypeKind kind = type->kind;


            if (kind == HL_type.TypeKind.HUI8)
            {
                return new HashlinkTypedValue<byte>(target);
            }
            else if (kind == HL_type.TypeKind.HUI16)
            {
                return new HashlinkTypedValue<ushort>(target);
            }
            else if (kind == HL_type.TypeKind.HI32)
            {
                return new HashlinkTypedValue<int>(target);
            }
            else if (kind == HL_type.TypeKind.HI64)
            {
                return new HashlinkTypedValue<long>(target);
            }
            else if (kind == HL_type.TypeKind.HF32)
            {
                return new HashlinkTypedValue<float>(target);
            }
            else if (kind == HL_type.TypeKind.HF64)
            {
                return new HashlinkTypedValue<double>(target);
            }
            else if (kind == HL_type.TypeKind.HBOOL)
            {
                return new HashlinkTypedValue<bool>(target);
            }
            else if (kind == HL_type.TypeKind.HVIRTUAL)
            {
                return new HashlinkVirtual(target);
            }
            else if (kind == HL_type.TypeKind.HOBJ)
            {
                return new HashlinkObject(target);
            }
            else if(kind == HL_type.TypeKind.HFUN)
            {
                return new HashlinkClosure(target);
            }
            else
            {
                throw new InvalidOperationException($"Unrecognized type {kind}");
            }
        }
    }
}
