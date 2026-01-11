using Hashlink.Reflection.Members;
using Hashlink.UnsafeUtilities;

namespace HaxeProxy.Runtime.Internals.Cache
{
    public struct FunctionInfoCache
    {
        public HashlinkFunction? function;
        public DelegateInfo? directEntry;
        public DelegateInfo? hookRealEntry;
    }
}
