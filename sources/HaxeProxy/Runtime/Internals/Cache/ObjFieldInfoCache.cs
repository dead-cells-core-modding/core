using Hashlink.Reflection.Types;

namespace HaxeProxy.Runtime.Internals.Cache
{
    /// <summary>
    /// Caches information used to access a field of a Hashlink object.
    /// </summary>
    public struct ObjFieldInfoCache
    {
        /// <summary>
        /// Indicates whether the field is the object's constructor.
        /// </summary>
        public bool isConstructor;
        /// <summary>
        /// Indicates whether the cache has been populated.
        /// </summary>
        public bool hasCache;
        /// <summary>
        /// The cached field type.
        /// </summary>
        public HashlinkType? field;
        /// <summary>
        /// The byte offset of the field within the object.
        /// </summary>
        public nint offset;
    }
}
