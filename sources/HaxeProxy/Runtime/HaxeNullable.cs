using Hashlink;
using Hashlink.Marshaling;

#pragma warning disable CS8500

namespace HaxeProxy.Runtime
{
    /// <summary>
    /// Represents a nullable value that can be marshaled to and from Haxe.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    [Obsolete]
    public unsafe class HaxeNullable<T> : IHashlinkPointer where T : struct
    {
        /// <summary>
        /// Gets or sets the wrapped value.
        /// </summary>
        public T Value {
            get; set;
        }

        nint IHashlinkPointer.HashlinkPointer {
            get {
                var ptr = HashlinkNative.hl_alloc_dynamic(HashlinkMarshal.GetHashlinkType(typeof(T))!.NativeType);
                *(T*)&ptr->val = Value;
                return (nint)ptr;
            }
        }

        /// <summary>
        /// Initializes a new nullable value with the specified value.
        /// </summary>
        /// <param name="value">The value to wrap.</param>
        public HaxeNullable( T value )
        {
            Value = value;
        }
        /// <summary>
        /// Initializes a new nullable value with the default value of <typeparamref name="T"/>.
        /// </summary>
        public HaxeNullable()
        {
        }

        /// <summary>
        /// Converts a nullable value to a <see cref="HaxeNullable{T}"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator HaxeNullable<T>?( T? value )
        {
            return value == null ? null : new(value.Value);
        }
        /// <summary>
        /// Converts a <see cref="HaxeNullable{T}"/> to a nullable value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator T?( HaxeNullable<T>? value )
        {
            return value?.Value;
        }
        /// <summary>
        /// Extracts the wrapped value.
        /// </summary>
        /// <param name="value">The value to extract from.</param>
        public static explicit operator T( HaxeNullable<T> value )
        {
            return value.Value;
        }
    }
}
