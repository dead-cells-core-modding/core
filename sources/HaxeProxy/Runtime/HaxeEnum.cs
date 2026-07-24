using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy.Values;
using HaxeProxy.Runtime.Internals;
using System.Dynamic;

namespace HaxeProxy.Runtime
{
    public abstract unsafe class HaxeEnum<TEnum, TIndex> : HaxeEnum where TIndex : struct, Enum
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
        public abstract TIndex Index {
            get;
        }
        public static implicit operator HaxeEnum<TEnum, TIndex>( TIndex index )
        {
            var it = itemTypes[index];
            return (HaxeEnum<TEnum, TIndex>?)Activator.CreateInstance(it) ??
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
            if (obj is not TEnum)
            {
                return false;
            }
            return base.Equals(obj);
        }
        public override string ToString()
        {
            return Index.ToString() ?? "";
        }
    }
    public abstract unsafe class HaxeEnum : HaxeProxyBase
    {
        protected HaxeEnum() : base(null!)
        {
            throw new InvalidProgramException();
        }

        public abstract int RawIndex {
            get;
        }

        public static bool operator ==( HaxeEnum? left, HaxeEnum? right )
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }
            if (left is null || right is null)
            {
                return false;
            }
            return left.Equals(right);
        }
        public static bool operator !=( HaxeEnum left, HaxeEnum right )
        {
            return !(left == right);
        }

        public dynamic? this[int paramId] {
            get {
                return HaxeProxyHelper.GetProxy<object>(((HashlinkEnum)HashlinkObj)[paramId]);
            }
            set {
                ((HashlinkEnum)HashlinkObj)[paramId] = value;
            }
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
            if (obj is HaxeEnum e)
            {
                if (e.RawIndex != RawIndex)
                {
                    return false;
                }
                return HashlinkNative.hl_dyn_compare((HL_vdynamic*)e.HashlinkPointer, (HL_vdynamic*)HashlinkPointer) == 0;
            }
            return false;
        }
    }
}
