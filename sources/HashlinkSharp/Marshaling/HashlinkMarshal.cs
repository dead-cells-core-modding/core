using Hashlink.Proxy;
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
    public static unsafe class HashlinkMarshal
    {
        public static HL_type.TypeKind? GetTypeKind(Type type)
        {
            if (type == typeof(int) || type == typeof(uint))
            {
                return HL_type.TypeKind.HI32;
            }
            else if (type == typeof(long) || type == typeof(ulong))
            {
                return HL_type.TypeKind.HI64;
            }
            else if (type == typeof(float))
            {
                return HL_type.TypeKind.HF32;
            }
            else if (type == typeof(double))
            {
                return HL_type.TypeKind.HF64;
            }
            else if (type == typeof(byte) || type == typeof(sbyte))
            {
                return HL_type.TypeKind.HUI8;
            }
            else if (type == typeof(bool))
            {
                return HL_type.TypeKind.HBOOL;
            }
            else if (type == typeof(short) || type == typeof(ushort))
            {
                return HL_type.TypeKind.HUI16;
            }
            else if (type == typeof(void))
            {
                return HL_type.TypeKind.HVOID;
            }
            return null;
        }

        public static IHashlinkMarshaler DefaultMarshaler { get; set; } = DefaultHashlinkMarshaler.Instance;

        public static bool IsPointer(this HL_type.TypeKind type)
        {
            return type >= HL_type.TypeKind.HBYTES;
        }
        
        public static void WriteData(
            void* target,
            object? val,
            HL_type.TypeKind? type,
            IHashlinkMarshaler? marshaler = null)
        {
            ArgumentNullException.ThrowIfNull(target, nameof(target));

            marshaler ??= DefaultHashlinkMarshaler.Instance;

            if(!marshaler.TryWriteData(target, val, type))
            {
                throw new InvalidOperationException("Unable to marshal the specified object");
            }
        }
        public static object? ReadData(
            void* target,
            HL_type.TypeKind? type,
            IHashlinkMarshaler? marshaler = null
            )
        {
            ArgumentNullException.ThrowIfNull(target, nameof(target));

            marshaler ??= DefaultHashlinkMarshaler.Instance;

            return marshaler.TryReadData(target, type);
        }


        public static HashlinkObj ConvertHashlinkObject(void* target)
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
            else if(kind == HL_type.TypeKind.HI32)
            {
                return new HashlinkTypedValue<int>(target);
            }
            else if(kind == HL_type.TypeKind.HI64)
            {
                return new HashlinkTypedValue<long>(target);
            }
            else if(kind == HL_type.TypeKind.HF32)
            {
                return new HashlinkTypedValue<float>(target);
            }
            else if(kind == HL_type.TypeKind.HF64)
            {
                return new HashlinkTypedValue<double>(target);
            }
            else if(kind == HL_type.TypeKind.HBOOL)
            {
                return new HashlinkTypedValue<bool>(target);
            }
            else if(kind == HL_type.TypeKind.HVIRTUAL)
            {
                return new HashlinkVirtual(target);
            }
            else if(kind == HL_type.TypeKind.HOBJ)
            {
                return new HashlinkObject(target);
            }
            else
            {
                throw new InvalidOperationException($"Unrecognized type {kind}");
            }
        }
    }
}
