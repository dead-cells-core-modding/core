using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime
{
    [StructLayout(LayoutKind.Sequential)]
    public ref struct Ref<T>(ref T value)
    {
        private static T? defaultTarget = default;
        public static Ref<T> Null => new();
        public static Ref<T> DontCare => new(ref defaultTarget!);
        public ref T value = ref value;
    }
}
