using Hashlink.Marshaling;
using Hashlink.Reflection.Members.Enum;
using Hashlink.Reflection.Types;
using System.Diagnostics;

namespace Hashlink.Proxy.Values
{
    public unsafe class HashlinkEnum( HashlinkObjPtr objPtr ) : HashlinkTypedObj<HL_enum>(objPtr)
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<(nint, int), nint> _singletons = new();

        public HashlinkEnum( HashlinkEnumType type, int index ) :
            this(HashlinkObjPtr.Get(GetOrCreateEnum(type, index)))
        {
            Debug.Assert(Handle != null);
        }

        private static nint GetOrCreateEnum( HashlinkEnumType type, int index )
        {
            var key = ((nint)type.NativeType, index);
            return _singletons.TryGetValue(key, out var cached)
                ? cached
                : (nint)hl_alloc_enum(type.NativeType, index);
        }

        // DefaultHashlinkMarshaler 从游戏读取枚举时调用。
        // 游戏中的枚举是单例——将其缓存，以便后续的 new Align.Center() 操作能复用原生代码用于身份验证的同一指针。
        internal static HashlinkEnum CacheAndCreate( HashlinkObjPtr ptr )
        {
            var e = new HashlinkEnum(ptr);
            if (e.Handle != null)
                _singletons[((nint)e.EnumType.NativeType, e.Index)] = e.HashlinkPointer;
            return e;
        }
        public HashlinkEnumType EnumType => (HashlinkEnumType)Type;
        public HashlinkEnumConstruct CurrentConstruct => EnumType.Constructs[Index];

        public byte* ParamsData => (byte*)(TypedRef + 1);

        public object? this[int paramId] {
            get {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(paramId, CurrentConstruct.ParamsCount);
                return HashlinkMarshal.ReadData(ParamsData + CurrentConstruct.ParamOffsets[paramId],
                     CurrentConstruct.Params[paramId]);
            }
            set {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(paramId, CurrentConstruct.ParamsCount);
                HashlinkMarshal.WriteData(ParamsData + CurrentConstruct.ParamOffsets[paramId],
                    value,
                    CurrentConstruct.Params[paramId]);
            }
        }
        public int Index {
            get => TypedRef->index;
        }
    }
}
