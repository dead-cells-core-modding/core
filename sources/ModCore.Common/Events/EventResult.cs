using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly unsafe struct EventResult<T>
    {
        private readonly bool hasValue = true;
        private readonly int size;
        private readonly T value;
        public readonly bool HasValue => hasValue;
        public readonly T Value => value;
        public static implicit operator EventResult<T>( T value )
        {
            return new(value);
        }

        public EventResult( T value )
        {
#pragma warning disable CS8500
            size = sizeof(T);
#pragma warning restore CS8500
            this.value = value;
        }

        public static EventResult<T> Null
        {
            get;
        } = new();

    }
}
