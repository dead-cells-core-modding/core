using Hashlink.Reflection.Types;

namespace HaxeProxy.Runtime.Internals.Cache
{
    public struct ObjFieldInfoCache
    {
        public bool hasCache;
        public HashlinkType? field;
        public nint offset;
    }
}
