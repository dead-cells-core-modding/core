using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe ref struct Ref<T>(ref T value)
    {
        private static T? defaultTarget = default;
        public static Ref<T> Null => new();
        public static Ref<T> DontCare => new(ref defaultTarget!);
        public static Ref<T> From( ref T val ) => new(ref val);
        public static Ref<T> Out([UnscopedRef] out T? val )
        {
            val = default;
            return new(ref val!);
        }
        public static Ref<T> In(in T val )
        {
#pragma warning disable CS8500
            fixed (void* ptr = &val)
            {
                return new(ref Unsafe.AsRef<T>(ptr));
            }
#pragma warning restore CS8500
        }
        public ref T value = ref value;
    }
}
