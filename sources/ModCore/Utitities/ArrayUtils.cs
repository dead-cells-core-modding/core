using dc.hl.types;
using Hashlink.Marshaling;
using System.Runtime.InteropServices;

namespace ModCore.Utitities
{

    /// <summary>
    /// Provides utility methods for creating empty array structures for various types.
    /// </summary>
    /// <remarks>This class is intended for scenarios where a reusable, empty array representation is needed
    /// for different numeric element types. All methods return array structures with zero length and no allocated data
    /// beyond a shared, zeroed memory area. The class is static and cannot be instantiated.</remarks>
    public unsafe static class ArrayUtils
    {
        private static readonly nint emptyArrayArea = (nint)NativeMemory.AllocZeroed(8);

        /// <summary>
        /// Creates a new instance of the ArrayBytes_hl_UI16 structure with an empty byte array and zero length.
        /// </summary>
        /// <returns>A new ArrayBytes_hl_UI16 instance initialized with no data.</returns>
        public static ArrayBytes_hl_UI16 CreateUI16()
        {
            return new()
            {
                bytes = emptyArrayArea,
                length = 0,
                size = 0
            };
        }

        /// <summary>
        /// Creates a new instance of the ArrayBytes_Int structure with no data.
        /// </summary>
        /// <returns>A new ArrayBytes_Int instance initialized with an empty byte array and zero length and size.</returns>
        public static ArrayBytes_Int CreateInt()
        {
            return new()
            {
                bytes = emptyArrayArea,
                length = 0,
                size = 0
            };
        }
        /// <summary>
        /// Creates a new instance of the ArrayBytes_Single structure initialized to an empty state.
        /// </summary>
        /// <returns>An ArrayBytes_Single instance with no data and a length of zero.</returns>
        public static ArrayBytes_Single CreateSingle()
        {
            return new()
            {
                bytes = emptyArrayArea,
                length = 0,
                size = 0
            };
        }
        /// <summary>
        /// Creates a new instance of the ArrayBytes_Float structure with no data.
        /// </summary>
        /// <returns>A new ArrayBytes_Float instance initialized with an empty byte array and zero length and size.</returns>
        public static ArrayBytes_Float CreateFloat()
        {
            return new()
            {
                bytes = emptyArrayArea,
                length = 0,
                size = 0
            };
        }
        /// <summary>
        /// Creates a new instance of the dynamic array with default initialization.
        /// </summary>
        /// <returns>A new <see cref="ArrayDyn"/> instance with its internal array initialized and length set to zero.</returns>
        public static ArrayDyn CreateDyn()
        {
            return new()
            {
                array = new ArrayObj()
                {
                    array = new(HashlinkMarshal.Module.KnownTypes.Dynamic, 0),
                    length = 0
                },
                
            };
        }
    }
}
