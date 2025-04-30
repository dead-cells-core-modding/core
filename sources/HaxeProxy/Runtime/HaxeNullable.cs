using Hashlink;
using Hashlink.Marshaling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS8500

namespace HaxeProxy.Runtime
{
    public unsafe class HaxeNullable<T> : IHashlinkPointer where T : struct
    {
        public T Value { get; set; }

        nint IHashlinkPointer.HashlinkPointer
        {
            get
            {
                var ptr = HashlinkNative.hl_alloc_dynamic(HashlinkMarshal.GetHashlinkType(typeof(T))!.NativeType);
                *(T*)&ptr->val = Value;
                return (nint)ptr;
            }
        }

        public HaxeNullable( T value )
        {
            Value = value;
        }
        public HaxeNullable()
        {
        }

        public static implicit operator HaxeNullable<T>?( T? value )
        {
            return value == null ? null : new(value.Value);
        }
        public static implicit operator T?( HaxeNullable<T>? value )
        {
            return value?.Value;
        }
        public static explicit operator T( HaxeNullable<T> value )
        {
            return value.Value;
        }
    }
}
