using System.Reflection;

#nullable disable

namespace Hashlink.UnsafeUtilities
{
    internal class ClosureInfo
    {
        internal static FieldInfo FI_first = typeof(ClosureInfo).GetField(nameof(first));
        internal static FieldInfo FI_target = typeof(ClosureInfo).GetField(nameof(target));
        public object first;
        public DelegateInfo target;
    }
}
