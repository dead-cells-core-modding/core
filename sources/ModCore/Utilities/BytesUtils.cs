using dc.haxe.io;
using System.Buffers;

namespace ModCore.Utilities
{
    /// <summary>
    /// 
    /// </summary>
    public static unsafe class BytesUtils
    {
        private class BytesMemoryManager : MemoryManager<byte>
        {
            private readonly Bytes bytes;
            public BytesMemoryManager( Bytes bytes )
            {
                this.bytes = bytes;
            }
            public override Memory<byte> Memory => CreateMemory(bytes.length);
            public override Span<byte> GetSpan()
            {
                return new((void*)bytes.b, bytes.length);
            }
            protected override void Dispose( bool disposing )
            {
            }
            public override MemoryHandle Pin( int elementIndex = 0 )
            {
                if (elementIndex < 0 || elementIndex >= bytes.length)
                {
                    throw new ArgumentOutOfRangeException(nameof(elementIndex));
                }
                return new MemoryHandle((byte*)bytes.b + elementIndex);
            }
            public override void Unpin()
            {
            }
        }
        /// <summary>
        /// Creates a span over the contents of the specified <see cref="Bytes"/> instance.
        /// </summary>
        /// <remarks>The returned span provides direct access to the underlying memory of the <see
        /// cref="Bytes"/> instance. Modifying the span will affect the original buffer. Use caution when accessing the
        /// span if the lifetime of the underlying buffer is not guaranteed.</remarks>
        /// <param name="bytes">The <see cref="Bytes"/> object whose underlying buffer will be exposed as a <see cref="Span{T}"/>. Cannot
        /// be null.</param>
        /// <returns>A <see cref="System.Span{T}"/> representing the bytes contained in <paramref name="bytes"/>.</returns>
        public static Span<byte> AsSpan( this Bytes bytes )
        {
            return new((void*)bytes.b, bytes.length);
        }
        /// <summary>
        /// Creates a <see cref="System.Memory{T}"/> instance that represents the contents of the specified Bytes object.
        /// </summary>
        /// <param name="bytes">The Bytes object whose data will be exposed as a <see cref="System.Memory{T}"/>. Cannot be null.</param>
        /// <returns>A <see cref="System.Memory{T}"/> containing the data from the specified Bytes object.</returns>
        public static Memory<byte> AsMemory( this Bytes bytes )
        {
            return new BytesMemoryManager(bytes).Memory;
        }


    }
}
