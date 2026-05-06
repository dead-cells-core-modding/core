using Hashlink.Marshaling;
using Hashlink.Reflection;
using Hashlink.Reflection.Members.Enum;
using Hashlink.Reflection.Types;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Hashlink.Proxy.Values
{
    public unsafe class HashlinkEnum( HashlinkObjPtr objPtr ) : HashlinkTypedObj<HL_enum>(objPtr)
    {
        // Native parameterless enumeration singleton cache, where the key is (typePtr, constructIndex)
        private static readonly ConcurrentDictionary<(nint, int), nint> singletons = new();
        // Each module is pre-filled only once
        private static readonly ConcurrentDictionary<nint, bool> prePopulated = new();
        // Do not replace with a singleton when writing custom subclasses
        private static readonly ConcurrentDictionary<(nint, int), bool> customSubclassKeys = new();

        public HashlinkEnum( HashlinkEnumType type, int index ) :
            this(HashlinkObjPtr.Get(hl_alloc_enum(type.NativeType, index)))
        {
            Debug.Assert(Handle != null);

            if (prePopulated.TryAdd((nint)type.Module.NativeModule, true))
                PrePopulateSingletons(type.Module);
        }

        private static void PrePopulateSingletons( HashlinkModule module )
        {
            foreach (var g in module.Globals)
            {
                if (g.Type.IsEnum && g.Value is HashlinkEnum e && e.Handle != null)
                    singletons[((nint)e.EnumType.NativeType, e.Index)] = e.HashlinkPointer;
            }
        }

        // HaxeProxyBase is called when a custom enumeration subclass is detected
        internal static void MarkNoSingleton( HashlinkEnum e )
        {
            customSubclassKeys[((nint)e.EnumType.NativeType, e.Index)] = true;
        }

        // Call TryWriteData when writing to enumeration 
        // fields: replace with a singleton pointer to ensure the native code pointer is correct
        internal static nint ResolveWritePointer( nint enumPtr )
        {
            var e = (HL_enum*)enumPtr;
            var key = ((nint)e->t, e->index);
            return customSubclassKeys.ContainsKey(key) || !singletons.TryGetValue(key, out var s)
                ? enumPtr
                : s;
        }

        public HashlinkEnumType EnumType => (HashlinkEnumType)Type;
        public HashlinkEnumConstruct CurrentConstruct => EnumType.Constructs[Index];
        public byte* ParamsData => (byte*)(TypedRef + 1);
        public int Index => TypedRef->index;

        public object? this[int paramId]
        {
            get {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(paramId, CurrentConstruct.ParamsCount);
                return HashlinkMarshal.ReadData(ParamsData + CurrentConstruct.ParamOffsets[paramId],
                     CurrentConstruct.Params[paramId]);
            }
            set {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(paramId, CurrentConstruct.ParamsCount);
                HashlinkMarshal.WriteData(ParamsData + CurrentConstruct.ParamOffsets[paramId],
                    value, CurrentConstruct.Params[paramId]);
            }
        }
    }
}
