using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink
{
    internal unsafe static class Utils
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct BoxedStruct
        {
            [FieldOffset(0)]
            public byte first;
        }

        public static uint ReverseBits(uint x)
        {
            x = (((x & 0xaaaaaaaa) >> 1) | ((x & 0x55555555) << 1));
            x = (((x & 0xcccccccc) >> 2) | ((x & 0x33333333) << 2));
            x = (((x & 0xf0f0f0f0) >> 4) | ((x & 0x0f0f0f0f) << 4));
            x = (((x & 0xff00ff00) >> 8) | ((x & 0x00ff00ff) << 8));

            return ((x >> 16) | (x << 16));
        }

        public static nint BoxedObjectValueOffset { get; }

        public static ref T ForceUnbox<T>(object obj) where T : struct
        {
            return ref Unsafe.Unbox<T>(obj);
        }

        static Utils()
        {
            {
                object boxed = new BoxedStruct();
                ref BoxedStruct refBoxed = ref Unsafe.Unbox<BoxedStruct>(boxed);
                fixed (byte* ptr = &refBoxed.first)
                {
                    BoxedObjectValueOffset = (nint)ptr - (nint)Unsafe.AsPointer(ref refBoxed);
                }
            }
        }
    }
}
