using Hashlink.Reflection.Types;
using Hashlink.Reflection.Types.Special;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Hashlink.Wrapper
{
    public unsafe struct HlFuncSign
    {
        public struct ArgSign
        {
            private byte t1;
            private byte t2;

            public TypeKind Kind
            {
                readonly get => (TypeKind)t1;
                set => t1 = (byte)value;
            }
            public TypeKind KindEx
            {
                readonly get => (TypeKind)t2;
                set => t2 = (byte)value;
            }
        }
        private const int MAX_ARG_COUNT = 32;
        private const int ARG_TYPES_SIZE = MAX_ARG_COUNT * 2;
        private fixed byte argTypes[ARG_TYPES_SIZE];
        private int argCount;

        public readonly TypeKind ReturnType
        {
            get; init;
        }
        public Span<ArgSign> ArgTypes => MemoryMarshal.CreateSpan(
            ref Unsafe.As<byte, ArgSign>(ref argTypes[0])
            , argCount);

        public static HlFuncSign Create( TypeKind ret, params ReadOnlySpan<TypeKind> args )
        {
            var sign = new HlFuncSign
            {
                argCount = args.Length,
                ReturnType = ret
            };
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == TypeKind.HREF || args[i] == TypeKind.HNULL)
                {
                    throw new InvalidOperationException();
                }
                sign.ArgTypes[i].Kind = args[i];
            }
            return sign;
        }
        public static HlFuncSign Create( HashlinkFuncType type )
        {
            return Create(type.ReturnType, type.ArgTypes);
        }
        public static HlFuncSign Create( HashlinkType ret, params ReadOnlySpan<HashlinkType> args )
        {
            var sign = new HlFuncSign
            {
                argCount = args.Length,
                ReturnType = ret.TypeKind
            };
            for (var i = 0; i < args.Length; i++)
            {
                var t = args[i];
                var k = t.TypeKind;
                sign.ArgTypes[i].Kind = k;
                if (t is HashlinkRefType rt)
                {
                    sign.ArgTypes[i].KindEx = rt.RefType.TypeKind;
                }
                else if (t is HashlinkNullType nt)
                {
                    sign.ArgTypes[i].KindEx = nt.ValueType.TypeKind;
                }

            }
            return sign;
        }



        public readonly override bool Equals( [NotNullWhen(true)] object? obj )
        {
            if (obj is not HlFuncSign sign)
            {
                return false;
            }
            if (sign.argCount != argCount ||
                sign.ReturnType != ReturnType)
            {
                return false;
            }
            return MemoryMarshal.CreateReadOnlySpan(in argTypes[0], ARG_TYPES_SIZE).SequenceEqual(
                MemoryMarshal.CreateReadOnlySpan(in sign.argTypes[0], ARG_TYPES_SIZE)
                );
        }

        public readonly override int GetHashCode()
        {
            unchecked
            {
                var hash = 17 + argCount;
                hash = hash * 31 + (byte)ReturnType;
                for (var i = 0; i < ARG_TYPES_SIZE; i++)
                {
                    hash = hash * 31 + argTypes[i];
                }
                return hash;
            }
        }
        public static bool operator ==( HlFuncSign left, HlFuncSign right )
        {
            return left.Equals(right);
        }

        public static bool operator !=( HlFuncSign left, HlFuncSign right )
        {
            return !(left == right);
        }
    }
}
