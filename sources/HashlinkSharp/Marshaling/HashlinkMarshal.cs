using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection;
using ModCore;

namespace Hashlink.Marshaling
{
    public static unsafe class HashlinkMarshal
    {
        public static HashlinkModule Module
        {
            get; private set;
        } = null!;
        internal static void Initialize( HL_module* module )
        {
            Module = new(module);
        }
        public static TypeKind? GetTypeKind( Type type )
        {
            if (type == typeof(int) || type == typeof(uint))
            {
                return TypeKind.HI32;
            }
            else if (type == typeof(long) || type == typeof(ulong))
            {
                return TypeKind.HI64;
            }
            else if (type == typeof(float))
            {
                return TypeKind.HF32;
            }
            else if (type == typeof(double))
            {
                return TypeKind.HF64;
            }
            else if (type == typeof(byte) || type == typeof(sbyte))
            {
                return TypeKind.HUI8;
            }
            else if (type == typeof(bool))
            {
                return TypeKind.HBOOL;
            }
            else if (type == typeof(short) || type == typeof(ushort))
            {
                return TypeKind.HUI16;
            }
            else if (type == typeof(void))
            {
                return TypeKind.HVOID;
            }
            return null;
        }

        public static IHashlinkMarshaler DefaultMarshaler { get; set; } = DefaultHashlinkMarshaler.Instance;

        public static bool IsPointer( this TypeKind type )
        {
            return type >= TypeKind.HBYTES;
        }

        public static void WriteData(
            void* target,
            object? val,
            TypeKind? type,
            IHashlinkMarshaler? marshaler = null )
        {
            ArgumentNullException.ThrowIfNull(target, nameof(target));

            marshaler ??= DefaultHashlinkMarshaler.Instance;

            if (!marshaler.TryWriteData(target, val, type))
            {
                throw new InvalidOperationException("Unable to marshal the specified object");
            }
        }
        public static object? ReadData(
            void* target,
            TypeKind? type,
            IHashlinkMarshaler? marshaler = null
            )
        {
            ArgumentNullException.ThrowIfNull(target, nameof(target));

            marshaler ??= DefaultHashlinkMarshaler.Instance;

            return marshaler.TryReadData(target, type);
        }

        public static bool IsHashlinkObject( void* ptr )
        {
            if (!mcn_memory_readable(ptr))
            {
                return false;
            }
            var type = ((HL_vdynamic*)ptr)->type;
            return mcn_memory_readable(type) && type->kind is > 0 and <= TypeKind.HLAST;
        }

        public static bool IsAllocatedHashlinkObject( void* ptr )
        {
            return hl_is_gc_ptr(ptr) && IsHashlinkObject(ptr);
        }
        public static HashlinkObj? ConvertHashlinkObject( HashlinkObjPtr target,
            IHashlinkMarshaler? marshaler = null )
        {
            return ConvertHashlinkObject((void*)target.Pointer, marshaler);
        }
        public static HashlinkObj? ConvertHashlinkObject( void* target,
            IHashlinkMarshaler? marshaler = null )
        {
            if (target == null)
            {
                return null;
            }
            marshaler ??= DefaultHashlinkMarshaler.Instance;
            var handle = HashlinkObjHandle.GetHandle(target);
            return handle != null && handle.Target != null
                ? handle.Target
                : marshaler.TryConvertHashlinkObject(target) ?? throw new InvalidOperationException();
        }
        public static T? ConvertHashlinkObject<T>( HashlinkObjPtr target,
           IHashlinkMarshaler? marshaler = null ) where T : HashlinkObj
        {
            return (T?)ConvertHashlinkObject((void*)target.Pointer, marshaler);
        }
        public static T? ConvertHashlinkObject<T>( void* target,
           IHashlinkMarshaler? marshaler = null ) where T : HashlinkObj
        {
            return (T?)ConvertHashlinkObject(target, marshaler);
        }


    }
}
