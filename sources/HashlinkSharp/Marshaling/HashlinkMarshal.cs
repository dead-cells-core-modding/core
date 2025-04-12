using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection;
using Hashlink.Reflection.Members;
using Hashlink.Reflection.Types;
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

        public static HashlinkFunction FindFunction( string typeName, string funcName )
        {
            var type = (HashlinkObjectType)Module.GetTypeByName(typeName);
            return type.FindProto(funcName)?.Function ??
                type.Bindings.First(x => x.Name == funcName).Function;
        }
        public static HashlinkType GetHashlinkType( HL_type* type )
        {
            return Module.GetMemberFrom<HashlinkType>(type);
        }
        public static HashlinkType? GetHashlinkType( Type type )
        {
            var kt = Module.KnownTypes;
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
            return null;
        }

        public static IHashlinkMarshaler DefaultMarshaler { get; set; } = DefaultHashlinkMarshaler.Instance;

        public static Dictionary<TypeKind, Type> PrimitiveTypes
        {
            get;
        } = new()
        {
            [TypeKind.HI32] = typeof(int),
            [TypeKind.HI64] = typeof(long),
            [TypeKind.HUI16] = typeof(ushort),
            [TypeKind.HUI8] = typeof(byte),
            [TypeKind.HF32] = typeof(float),
            [TypeKind.HF64] = typeof(double),
            [TypeKind.HBYTES] = typeof(nint),
            [TypeKind.HBOOL] = typeof(bool),
            [TypeKind.HVOID] = typeof(void),
            [TypeKind.HREF] = typeof(nint)
        };

        public static bool IsValueType( this TypeKind type )
        {
            return type < TypeKind.HBYTES || type == TypeKind.HREF;
        }
        public static bool IsPointer( this TypeKind type )
        {
            return type >= TypeKind.HBYTES;
        }
        public static HashlinkObject? GetGlobal( string name )
        {
            return ((HashlinkObjectType)Module.GetTypeByName(name)).GlobalValue;
        }
        public static void WriteData(
            void* target,
            object? val,
            HashlinkType? type,
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
            HashlinkType? type,
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
        public static object? ConvertHashlinkObject( HashlinkObjPtr target,
            IHashlinkMarshaler? marshaler = null )
        {
            return ConvertHashlinkObject((void*)target.Pointer, marshaler);
        }
        public static object? ConvertHashlinkObject( void* target,
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
        public static void MarkUsed( IHashlinkPointer ptr )
        {
            HashlinkObjHandle.MarkUsed(ptr.HashlinkPointer);
        }
    }
}
