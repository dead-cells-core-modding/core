using System.Runtime.InteropServices;

namespace ModCore.Events
{
    public readonly struct EventResult<T>
    {
        private readonly bool hasValue = true;
        private readonly T value;
        public readonly bool HasValue => hasValue;
        public readonly T Value => value;
        public static implicit operator EventResult<T>( T value )
        {
            return new(value);
        }

        public EventResult( T value )
        {
            this.value = value;
        }

        public static EventResult<T> Null {
            get;
        } = new();

    }
}
