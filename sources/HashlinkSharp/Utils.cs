using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hashlink
{
    internal static unsafe class Utils
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct BoxedStruct
        {
            [FieldOffset(0)]
            public byte first;
        }

        public static uint ReverseBits( uint x )
        {
            x = ((x & 0xaaaaaaaa) >> 1) | ((x & 0x55555555) << 1);
            x = ((x & 0xcccccccc) >> 2) | ((x & 0x33333333) << 2);
            x = ((x & 0xf0f0f0f0) >> 4) | ((x & 0x0f0f0f0f) << 4);
            x = ((x & 0xff00ff00) >> 8) | ((x & 0x00ff00ff) << 8);

            return (x >> 16) | (x << 16);
        }

        public static nint BoxedObjectValueOffset
        {
            get;
        }

        public static bool InAllocBlock( HL_alloc_block* first, void* ptr )
        {
            while (first != null)
            {
                if (ptr >= first && ptr <= first->p)
                {
                    return true;
                }
                first = first->next;
            }
            return false;
        }

        public static ref T ForceUnbox<T>( object obj ) where T : struct
        {
            return ref Unsafe.Unbox<T>(obj);
        }

        static Utils()
        {
            {
                object boxed = new BoxedStruct();
                ref var refBoxed = ref Unsafe.Unbox<BoxedStruct>(boxed);
                fixed (byte* ptr = &refBoxed.first)
                {
                    BoxedObjectValueOffset = (nint)ptr - (nint)Unsafe.AsPointer(ref refBoxed);
                }
            }
        }
    }
}
