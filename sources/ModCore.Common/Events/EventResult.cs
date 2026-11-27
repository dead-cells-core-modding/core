using System.Runtime.InteropServices;

namespace ModCore.Events
{
    /// <summary>
    /// Represents the result of an event handler.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    public readonly struct EventResult<T>
    {
        private readonly bool hasValue = true;
        private readonly T value;
        /// <summary>
        /// Gets a value indicating whether the result carries a value.
        /// </summary>
        public readonly bool HasValue => hasValue;
        /// <summary>
        /// Gets the result value.
        /// </summary>
        public readonly T Value => value;
        /// <summary>
        /// Converts a value to an <see cref="EventResult{T}"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        public static implicit operator EventResult<T>( T value )
        {
            return new(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventResult{T}"/> struct with a value.
        /// </summary>
        /// <param name="value">The result value.</param>
        public EventResult( T value )
        {
            this.value = value;
        }

        /// <summary>
        /// Gets an empty result.
        /// </summary>
        public static EventResult<T> Null {
            get;
        } = new();

    }
}
