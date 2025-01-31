using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Events
{
    public struct EventResult<T>( T value )
    {
        public T Value
        {
            get; private set;
        } = value;
        public bool HasValue
        {
            get; private set;
        } = true;

        public static implicit operator EventResult<T>( T value )
        {
            return new(value);
        }

        public static EventResult<T> Null
        {
            get;
        } = new();

    }
}
