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

#nullable disable

namespace Hashlink.Wrapper
{
    public unsafe class HlFuncSign
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

        private ArgSign[] argTypes;

        public TypeKind ReturnType
        {
            get; init;
        }
        public ReadOnlySpan<ArgSign> ArgTypes => argTypes;

        public static HlFuncSign Create( TypeKind ret, params ReadOnlySpan<TypeKind> args )
        {
            var sign = new HlFuncSign
            {
                argTypes = new ArgSign[args.Length],
                ReturnType = ret
            };
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == TypeKind.HREF || args[i] == TypeKind.HNULL)
                {
                    throw new InvalidOperationException();
                }
                sign.argTypes[i].Kind = args[i];
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
                argTypes = new ArgSign[args.Length],
                ReturnType = ret.TypeKind
            };
            for (var i = 0; i < args.Length; i++)
            {
                var t = args[i];
                var k = t.TypeKind;
                sign.argTypes[i].Kind = k;
                if (t is HashlinkRefType rt)
                {
                    sign.argTypes[i].KindEx = rt.RefType.TypeKind;
                }
                else if (t is HashlinkNullType nt)
                {
                    sign.argTypes[i].KindEx = nt.ValueType.TypeKind;
                }

            }
            return sign;
        }



        public override bool Equals( [NotNullWhen(true)] object obj )
        {
            if (obj is not HlFuncSign sign)
            {
                return false;
            }
            if (sign.argTypes.Length != sign.argTypes.Length ||
                sign.ReturnType != ReturnType)
            {
                return false;
            }
            return argTypes.SequenceEqual(
                sign.argTypes
                );
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17 + argTypes.Length;;
                hash = hash * 31 + (byte)ReturnType;
                for (var i = 0; i < argTypes.Length; i++)
                {
                    hash = hash * 31 + ((int)argTypes[i].Kind << 8 | (int)argTypes[i].KindEx);
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
