using Hashlink.Marshaling;
using Hashlink.Reflection.Types;

namespace Hashlink.Reflection
{
    public unsafe class HashlinkGlobal
    {
        public HashlinkType Type
        {
            get;
        }
        public int Index
        {
            get;
        }
        private nint globalPtr;
        private object? cachedGlobalValue;
        public unsafe HashlinkGlobal( HashlinkModule module, HashlinkType type, int index )
        {
            Type = type;
            Index = index;
            globalPtr = (nint)module.NativeModule->globals_data +
                module.NativeModule->globals_indexes[Index];

        }
        public object? Value => cachedGlobalValue ??= HashlinkMarshal.ReadData((void**)globalPtr, Type);
        public override string ToString()
        {
            return Value?.ToString() ?? $"G:[{Type}]{Index}";
        }
    }
}
