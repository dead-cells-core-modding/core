using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.UnsafeUtilities
{
    internal unsafe class MemoryBlock<T> : IDisposable where T : unmanaged
    {
        private readonly List<IMemoryOwner<T>> pages = [];
        private IMemoryOwner<T>? lastPage;
        private int pos;
        public T* Alloc( int size )
        {
            T* ptr = null;
            size += (sizeof(int) + sizeof(T) - 1) / sizeof(T);
            if (lastPage != null &&
                lastPage.Memory.Length - pos > size)
            {
                ptr = (T*) Unsafe.AsPointer(ref lastPage.Memory.Span[pos..size].GetPinnableReference());
            }
            if (ptr == null)
            {
                var newSize = (lastPage?.Memory.Length ?? 64) << 1;
                lastPage = MemoryPool<T>.Shared.Rent(newSize);
                pages.Add(lastPage);
                pos = size;
            }
            ptr++;
            *((int*)ptr - 1) = size;
            return ptr;
        }
        public T* Expand( T* old, int size )
        {
            var os = GetSize(old);
            var n = Alloc(os + size);
            Buffer.MemoryCopy(old, n, os * sizeof(T), os * sizeof(T));
            return n;
        }
        public int GetSize( T* ptr )
        {
            return *((int*)ptr - 1);
        }
        public void Dispose()
        {
            foreach (var page in pages)
            {
                page.Dispose();
            }
            pages.Clear();
        }
    }
}
