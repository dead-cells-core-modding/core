using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameRes.Core
{
    internal unsafe class UnmanagedMemoryManager(byte* ptr, int size, object? handle) : MemoryManager<byte>
    {
        
        public object? Handle { get; } = handle;
        public override Memory<byte> Memory => CreateMemory(0, size);
        public override Span<byte> GetSpan()
        {
            return new(ptr, size);
        }

        public override MemoryHandle Pin(int elementIndex = 0)
        {
            return new(ptr + elementIndex);
        }

        public override void Unpin()
        {
            
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
