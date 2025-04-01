using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

#nullable disable

namespace Hashlink.UnsafeUtilities
{
    internal class ClosureInfo
    {
        internal static FieldInfo FI_first = typeof(DelegateInfo).GetField(nameof(first));
        internal static FieldInfo FI_target = typeof(DelegateInfo).GetField(nameof(target));
        public object first;
        public DelegateInfo target;
    }
}
