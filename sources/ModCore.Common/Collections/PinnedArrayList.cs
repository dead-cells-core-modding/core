using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ModCore.Collections
{
    internal unsafe class PinnedArrayList<T> where T : struct
    {
        private const int BLOCK_SIZE = 4096;
        private readonly List<T[]> arrays = [];
        private int count = 0;
        private T[]? currentArray = null;
        private int currentIndex = 0;

        public int Count => count;

        public bool IsReadOnly => false;

        public ref T Add(T item)
        {
            if (currentArray == null ||
                    currentIndex >= currentArray.Length)
            {
                currentArray = GC.AllocateArray<T>(BLOCK_SIZE, true);
                arrays.Add(currentArray);
                currentIndex = 0;
            }
            count++;
            currentArray[currentIndex++] = item;
            return ref currentArray[currentIndex - 1];
        }

        public nint GetPointer( int index )
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, count);
            return (nint)Unsafe.AsPointer(ref arrays[index / BLOCK_SIZE][index % BLOCK_SIZE]);
        }

        public int GetIndex( nint ptr )
        {
            var bs = BLOCK_SIZE * Unsafe.SizeOf<T>();
            for(int i = 0; i < arrays.Count; i++)
            {
                var v = arrays[i];
                var start = (nint)Unsafe.AsPointer(ref v[0]);
                if (ptr >= start && ptr < (start + bs))
                {
                    return (int)(i * BLOCK_SIZE + (ptr - start) / Unsafe.SizeOf<T>());
                }
            }
            return -1;
        }

        public bool Contain( nint ptr )
        {
            return GetIndex(ptr) != -1;
        }
        
    }
}
