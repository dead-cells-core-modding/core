using Hashlink.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime
{
    public abstract class HaxeEnum<TEnum, TIndex> : HaxeEnum where TIndex : struct, Enum
        where TEnum : HaxeEnum<TEnum, TIndex>
    {

        private static readonly Dictionary<TIndex, Type> itemTypes = [];
        static HaxeEnum()
        {
            foreach (var v in typeof(TIndex).GetEnumNames())
            {
                var it = typeof(TEnum).GetNestedType(v) ?? throw new InvalidOperationException();
                itemTypes.Add(Enum.Parse<TIndex>(v, true), it);
            }
        }
        public override int RawIndex => (int)(object)Index;
        public abstract TIndex Index
        {
            get;
        }
        public static implicit operator HaxeEnum<TEnum, TIndex>( TIndex index )
        {
            var it = itemTypes[index];
            return (HaxeEnum < TEnum, TIndex >?)Activator.CreateInstance(it) ??
                throw new InvalidOperationException();
        }
        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }
        public override bool Equals( object? obj )
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj is not TEnum tenum)
            {
                return false;
            }
            return Index.Equals(tenum);
        }
        public override string ToString()
        {
            return Index.ToString() ?? "";
        }
    }
    public abstract class HaxeEnum : HaxeProxyBase
    {
        protected HaxeEnum( ) : base(null!)
        {
            throw new InvalidProgramException();
        }

        public abstract int RawIndex
        {
            get;
        }

        public static bool operator ==( HaxeEnum? left, HaxeEnum? right )
        {
            if (left == right)
            {
                return true;
            }
            if (left is null || right is null)
            {
                return false;
            }
            return left.Equals( right );
        }
        public static bool operator !=( HaxeEnum left, HaxeEnum right )
        {
            return !(left == right);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals( object? obj )
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            return false;
        }
    }
}
