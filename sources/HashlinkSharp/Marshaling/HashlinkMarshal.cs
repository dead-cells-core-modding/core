using Hashlink.Marshaling.ObjHandle;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection;
using Hashlink.Reflection.Members;
using Hashlink.Reflection.Types;
using HashlinkNET.Bytecode;
using System.Diagnostics;

namespace Hashlink.Marshaling
{
    public static unsafe class HashlinkMarshal
    {
        public static HlCode? Code {
            get; private set;
        } = null!;
        public static HashlinkModule Module {
            get; private set;
        } = null!;
        internal static void Initialize( HL_module* module, HlCode? code )
        {
            Module = new(module);
            Code = code;

            if (!CheckPlatform())
            {
                throw new PlatformNotSupportedException();
            }
        }

        private static bool CheckPlatform()
        {
            var knownTypes = Module.KnownTypes;
            if (knownTypes.Bool.SizeOf != 1)
            {
                return false;
            }
            if (knownTypes.I32.SizeOf != 4)
            {
                return false;
            }
            if (knownTypes.Dynamic.SizeOf != nint.Size)
            {
                return false;
            }


            {
                int v = 114514;
                var obj = hl_make_dyn(&v, knownTypes.I32.NativeType);
                if (obj->val.@int != v)
                {
                    return false;
                }

                var obj_ptr = HashlinkObjPtr.Get(obj);
                var obj_size = obj_ptr.GetMemSize();
                var expect_size = sizeof(HL_vdynamic) + nint.Size;
                if (obj_ptr.GetMemSize() != expect_size)
                {
                    return false;
                }

                var obj2 = ConvertHashlinkObject(HashlinkObjPtr.Get(obj));
                if (v != (int)obj2!)
                {
                    return false;
                }
            }

            return true;
        }

        public static HashlinkFunction FindFunction( string typeName, string funcName )
        {
            var type = (HashlinkObjectType)Module.GetTypeByName(typeName);
            return type.FindProto(funcName)?.Function ??
                type.Bindings.First(x => x.Name == funcName).Function;
        }

        public static HashlinkType GetHashlinkType( HL_type* type )
        {
            if (Module.TryGetTypeByPointer((nint)type, out var known))
            {
                return known;
            }
            return Module.GetMemberFrom<HashlinkType>(type);
        }
        public static HashlinkType? GetHashlinkType( Type type, IHashlinkMarshaler? marshaler = null )
        {
            marshaler ??= DefaultMarshaler;

            return marshaler.GetHashlinkType(type);
        }

        public static IHashlinkMarshaler DefaultMarshaler { get; set; } = DefaultHashlinkMarshaler.Instance;

        public static Dictionary<TypeKind, Type> PrimitiveTypes {
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
            [TypeKind.HREF] = typeof(nint),
            [TypeKind.HTYPE] = typeof(nint),
            [TypeKind.HABSTRACT] = typeof(nint)
        };

        public static bool IsRawPointer( this TypeKind type )
        {
            return type == TypeKind.HBYTES || type == TypeKind.HSTRUCT || type == TypeKind.HREF
                || type == TypeKind.HABSTRACT || type == TypeKind.HTYPE;
        }
        public static bool IsValueType( this TypeKind type )
        {
            return type < TypeKind.HBYTES || type == TypeKind.HGUID;
        }

        public static bool IsManagedPointer( this TypeKind type )
        {
            return type == TypeKind.HOBJ || type == TypeKind.HDYN || type == TypeKind.HDYNOBJ
                || type == TypeKind.HARRAY || type == TypeKind.HENUM || type == TypeKind.HFUN ||
                type == TypeKind.HNULL || type == TypeKind.HVIRTUAL || type == TypeKind.HMETHOD;
        }

        public static bool IsBox( this TypeKind type )
        {
            return type.IsValueType() || type.IsRawPointer();
        }

        public static bool IsPointer( this TypeKind type )
        {
            return type.IsManagedPointer() || type.IsRawPointer();
        }

        public static HashlinkObject? GetGlobal( string name )
        {
            return ((HashlinkObjectType)Module.GetTypeByName(name)).GlobalValue;
        }

        public static void WriteDataDyn(
           void* target,
           object? val,
           IHashlinkMarshaler? marshaler = null )
        {
            ArgumentNullException.ThrowIfNull(target, nameof(target));
            WriteData(target, val, Module.KnownTypes.Dynamic, marshaler);
        }

        public static nint GetDyn(
           object? val,
           IHashlinkMarshaler? marshaler = null )
        {
            nint target = 0;

            WriteData(&target, val, Module.KnownTypes.Dynamic, marshaler);
            return target;
        }

        public static void WriteData(
            void* target,
            object? val,
            HashlinkType type,
            IHashlinkMarshaler? marshaler = null )
        {
            ArgumentNullException.ThrowIfNull(target, nameof(target));

            marshaler ??= DefaultMarshaler;

            if (!marshaler.TryWriteData(target, val, type))
            {
                throw new InvalidOperationException($"Unable to marshal the specified object: {val?.GetType()?.FullName} -> {type}");
            }
        }

        public static object? ReadData(
            void* target,
            HashlinkType type,
            IHashlinkMarshaler? marshaler = null
            )
        {
            ArgumentNullException.ThrowIfNull(target, nameof(target));

            marshaler ??= DefaultMarshaler;

            return marshaler.TryReadData(target, type);
        }

        public static bool IsAllocatedHashlinkObject( void* ptr )
        {
            return hl_is_gc_ptr(ptr);
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
            EnsureThreadRegistered();

            marshaler ??= DefaultMarshaler;
            var handle = HashlinkObjManager.GetHandle((nint)target);
            return (handle != null && handle.Target != null)
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
            _ = HashlinkObjManager.GetHandle(ptr.HashlinkPointer);
        }

        public static void MarkStateful( IHashlinkPointer ptr )
        {
            var handle = HashlinkObjManager.GetHandle(ptr.HashlinkPointer);
            handle?.IsStateful = true;
        }


        public static bool EnsureThreadRegistered()
        {
            var result = HashlinkThread.EnsureThreadRegistered();
            if (result)
            {
                hl_blocking(1);
            }
            return result;
        }
    }
}
