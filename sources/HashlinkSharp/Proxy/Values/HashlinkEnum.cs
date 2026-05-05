using Hashlink.Marshaling;
using Hashlink.Reflection;
using Hashlink.Reflection.Members.Enum;
using Hashlink.Reflection.Types;
using System.Diagnostics;

namespace Hashlink.Proxy.Values
{
    public unsafe class HashlinkEnum( HashlinkObjPtr objPtr ) : HashlinkTypedObj<HL_enum>(objPtr)
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<(nint, int), nint> singletons = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<nint, bool> prePopulated = new();

        public HashlinkEnum( HashlinkEnumType type, int index ) :
            this(HashlinkObjPtr.Get(GetOrCreateEnum(type, index)))
        {
            Debug.Assert(Handle != null);
        }

        private static nint GetOrCreateEnum( HashlinkEnumType type, int index )
        {
            var key = ((nint)type.NativeType, index);
            if (singletons.TryGetValue(key, out var cached))
                return cached;

            // When the enumeration for this module is created for the first time
            // Pre-populate the singleton cache from the Globals module
            var modulePtr = (nint)type.Module.NativeModule;
            if (prePopulated.TryAdd(modulePtr, true))
                PrePopulateFromGlobals(type.Module);

            return singletons.TryGetValue(key, out cached)
                ? cached
                : (nint)hl_alloc_enum(type.NativeType, index);
        }

        private static void PrePopulateFromGlobals( HashlinkModule module )
        {
            foreach (var g in module.Globals)
            {
                if (g.Type.IsEnum && g.Value is HashlinkEnum e)
                    singletons[((nint)e.EnumType.NativeType, e.Index)] = e.HashlinkPointer;
            }
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
