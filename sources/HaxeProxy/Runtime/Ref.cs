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
    /// <summary>
    /// A wrapper for a .NET reference
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    [StructLayout(LayoutKind.Sequential)]
    public unsafe ref struct Ref<T>(ref T value)
    {
        private static T? defaultTarget = default;

        /// <summary>
        /// Get a null reference
        /// </summary>
        public static Ref<T> Null => new();
        /// <summary>
        /// Get a reference to the shared storage.
        /// This typically means you don't care what gets returned inside.
        /// </summary>
        public static Ref<T> DontCare
        {
            get
            {
                defaultTarget = default;
                return From(ref defaultTarget!);
            }
        }
        /// <summary>
        /// Create a ref from a .NET ref
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static Ref<T> From( ref T val ) => new(ref val);
        /// <summary>
        /// Create a ref from a .NET ref
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static Ref<T> Out([UnscopedRef] out T? val )
        {
            val = default;
            return new(ref val!);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        [Obsolete]
        public static Ref<T> In( [UnscopedRef] in T val )
        {
#pragma warning disable CS8500
            fixed (void* ptr = &val)
            {
                return new(ref Unsafe.AsRef<T>(ptr));
            }
#pragma warning restore CS8500
        }
        /// <summary>
        /// The value of the ref
        /// </summary>
        public ref T value = ref value;
    }
}
