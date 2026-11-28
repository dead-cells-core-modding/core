using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Reflection.Types;
using static Hashlink.HL_type;

namespace Hashlink.Reflection
{
    public unsafe class HashlinkGlobal
    {
        public HashlinkType Type {
            get;
        }
        public int Index {
            get;
        }
        private nint globalPtr;
        private HashlinkObj? cachedGlobalValue;

        public unsafe HashlinkGlobal( HashlinkModule module, HashlinkType type, int index )
        {
            Type = type;
            Index = index;

            globalPtr = (nint)module.NativeModule->globals_data +
                module.NativeModule->globals_indexes[Index];

        }
        public object? Value => Utils.TryGetFromPointerWithCache(*(nint*)globalPtr, ref cachedGlobalValue);
        public override string ToString()
        {
            return Value?.ToString() ?? $"G:[{Type}]{Index}";
        }
    }
}
