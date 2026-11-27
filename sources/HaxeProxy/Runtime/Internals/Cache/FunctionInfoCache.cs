using Hashlink.Reflection.Members;
using Hashlink.UnsafeUtilities;

namespace HaxeProxy.Runtime.Internals.Cache
{
    /// <summary>
    /// Caches information used to call a Hashlink function.
    /// </summary>
    public struct FunctionInfoCache
    {
        /// <summary>
        /// The cached Hashlink function.
        /// </summary>
        public HashlinkFunction? function;
        /// <summary>
        /// The cached direct-call entry point information.
        /// </summary>
        public DelegateInfo? directEntry;
    }
}
