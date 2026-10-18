using ModCore.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Hashlink.Marshaling
{
    internal static class HashlinkValueDynObjPoolInst
    {
        public static readonly HashlinkValueDynObjPool<bool> boolPool = new(0, 1, idx => idx == 1);
        public static readonly HashlinkValueDynObjPool<int> i32Pool = new(-128, 128, idx => idx);
        public static readonly HashlinkValueDynObjPool<ushort> ui16Pool = new(0, 256, idx => (ushort)idx);
        public static readonly HashlinkValueDynObjPool<byte> u8Pool = new(byte.MinValue, byte.MaxValue, idx => (byte)idx);
    }
    internal unsafe class HashlinkValueDynObjPool<T> where T : unmanaged
    {
        private readonly int startValue;
        private readonly HL_vdynamic[] values;
        public HashlinkValueDynObjPool(int start, int end, Func<int, object> factory)
        {
            startValue = start;

            var dyn_type = HashlinkMarshal.GetHashlinkType(typeof(T));

            Debug.Assert(dyn_type != null);

            int idx = 0;

            values = GC.AllocateArray<HL_vdynamic>(end - start + 1, true);

            for (int i = start; i <= end; i++)
            {
                values[idx].type = dyn_type.NativeType;
                HashlinkMarshal.WriteData(Unsafe.AsPointer(ref values[idx].val), factory(i), dyn_type);
                idx++;
            }
        }
        public bool TryGetValue( int idx, out nint result )
        {
            var i = idx - startValue;
            if (i >= values.Length || i < 0)
            {
                result = 0;
                return false;
            }

            result = (nint) Unsafe.AsPointer(ref values[i]);
            return true;
        }
    }
}
