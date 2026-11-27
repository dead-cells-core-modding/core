using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy.Values;
using HaxeProxy.Runtime.Internals;
using System.Dynamic;

namespace HaxeProxy.Runtime
{
    /// <summary>
    /// Base class for strongly typed Haxe enum proxies.
    /// </summary>
    /// <typeparam name="TEnum">The concrete enum proxy type.</typeparam>
    /// <typeparam name="TIndex">The enum type used to identify enum constructors.</typeparam>
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
        /// <inheritdoc/>
        public override int RawIndex => (int)(object)Index;
        /// <summary>
        /// Gets the enum constructor represented by this instance.
        /// </summary>
        public abstract TIndex Index {
            get;
        }
        /// <summary>
        /// Converts an enum value to its strongly typed proxy.
        /// </summary>
        /// <param name="index">The enum value to convert.</param>
        public static implicit operator HaxeEnum<TEnum, TIndex>( TIndex index )
        {
            var it = itemTypes[index];
            return (HaxeEnum<TEnum, TIndex>?)Activator.CreateInstance(it) ??
                throw new InvalidOperationException();
        }
        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }
        /// <inheritdoc/>
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
        /// <inheritdoc/>
        public override string ToString()
        {
            return Index.ToString() ?? "";
        }
    }
    /// <summary>
    /// Base class for proxies that represent Haxe enum values.
    /// </summary>
    public abstract unsafe class HaxeEnum : HaxeProxyBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HaxeEnum"/> class.
        /// </summary>
        /// <exception cref="InvalidProgramException">This constructor is never called directly.</exception>
        protected HaxeEnum() : base(null!)
        {
            throw new InvalidProgramException();
        }

        /// <summary>
        /// Gets the raw index of the enum constructor.
        /// </summary>
        public abstract int RawIndex {
            get;
        }

        /// <summary>
        /// Determines whether two enum values are equal.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true"/> if both values are equal; otherwise, <see langword="false"/>.</returns>
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
        /// <summary>
        /// Determines whether two enum values are not equal.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns><see langword="true"/> if both values differ; otherwise, <see langword="false"/>.</returns>
        public static bool operator !=( HaxeEnum left, HaxeEnum right )
        {
            return !(left == right);
        }

        /// <summary>
        /// Gets or sets the enum constructor parameter at the specified index.
        /// </summary>
        /// <param name="paramId">The zero-based parameter index.</param>
        public dynamic? this[int paramId] {
            get {
                return HaxeProxyHelper.GetProxy<object>(((HashlinkEnum)HashlinkObj)[paramId]);
            }
            set {
                ((HashlinkEnum)HashlinkObj)[paramId] = value;
            }
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        /// <inheritdoc/>
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
