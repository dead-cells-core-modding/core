using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Types;
using HaxeProxy.Runtime;
using Microsoft.CSharp.RuntimeBinder;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace HaxeProxy.Runtime.Internals
{
    /// <summary>
    /// Provides runtime helpers invoked by generated pseudocode assemblies.
    /// </summary>
    public unsafe class PseudocodeHelper
    {

        /// <summary>
        /// Creates an empty instance of the specified type, allocating a Haxe object when necessary.
        /// </summary>
        /// <typeparam name="T">The type to instantiate.</typeparam>
        /// <returns>An empty instance of <typeparamref name="T"/>.</returns>
        public static T CreateEmptyInstance<T>()
        {
            var t = typeof(T);
            if (typeof(HaxeProxyBase).IsAssignableFrom(t))
            {
                HashlinkMarshal.EnsureThreadRegistered();
                var type = HashlinkMarshal.Module.PreferTypes[GetTypeIndex(t)];
                return (T)(object)type.CreateInstance().AsHaxe<HaxeProxyBase>();
            }
            if (t.IsValueType)
            {
                return default!;
            }
            return (T)System.Runtime.CompilerServices.RuntimeHelpers
                .GetUninitializedObject(t);
        }

        /// <summary>
        /// Reads a field from a dynamic Haxe object.
        /// </summary>
        /// <param name="obj">The target object.</param>
        /// <param name="name">The field name.</param>
        /// <returns>The field value, or <see langword="null"/> if the object is not a field object.</returns>
        public static object? DynGet( object? obj, string name )
        {
            if (obj is HaxeProxyBase proxy &&
                proxy.HashlinkObj is IHashlinkFieldObject f)
            {
                return HaxeProxyHelper.GetProxy<object>(f.GetFieldValue(name));
            }
            return null;
        }
        /// <summary>
        /// Writes a field on a dynamic Haxe object.
        /// </summary>
        /// <param name="obj">The target object.</param>
        /// <param name="name">The field name.</param>
        /// <param name="value">The value to assign.</param>
        public static void DynSet( object? obj, string name, object? value )
        {
            if (obj is HaxeProxyBase proxy &&
                proxy.HashlinkObj is IHashlinkFieldObject f)
            {
                f.SetFieldValue(name, value);
            }
        }

        /// <summary>
        /// Assigns a value to the Hashlink global at the specified index.
        /// </summary>
        /// <param name="globalIndex">The global value index.</param>
        /// <param name="val">The value to assign.</param>
        /// <returns>The assigned value.</returns>
        public static object? SetGlobal( int globalIndex, object? val )
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var module = HashlinkMarshal.Module;
            var global = module.Globals[globalIndex];
            var ptr = (void*)((nint)module.NativeModule->globals_data +
                module.NativeModule->globals_indexes[globalIndex]);
            HashlinkMarshal.WriteData(ptr, val, global.Type);
            return val;
        }

        /// <summary>
        /// Attempts to cast a value to the specified type, returning <see langword="null"/> on failure.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="obj">The value to cast.</param>
        /// <returns>The cast value, or <see langword="null"/> if the cast is not possible.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T? CastSafe<T>( object? obj ) where T : class
        {
            if (obj == null)
            {
                return null;
            }
            if (obj is T r)
            {
                return r;
            }
            if (typeof(T).IsAssignableTo(typeof(HaxeVirtual)))
            {
                return ((HaxeProxyBase)HaxeProxyHelper.GetProxy<T>(obj))?.ToVirtual(typeof(T)) as T;
            }
            if (typeof(T).IsAssignableTo(typeof(HaxeProxyBase)))
            {
                if (obj is HaxeVirtual virt)
                {
                    return virt.AsObject() as T;
                }
                if (obj is HashlinkObj ho)
                {
                    return ho.AsHaxe() as T;
                }
            }
            try
            {
                return (T)(dynamic)obj;
            }
            catch (RuntimeBinderException)
            {
                return null;
            }
        }

        /// <summary>
        /// Casts a value to the specified type.
        /// </summary>
        /// <typeparam name="T">The target type.</typeparam>
        /// <param name="obj">The value to cast.</param>
        /// <returns>The cast value, or <see langword="null"/> if <paramref name="obj"/> is <see langword="null"/>.</returns>
        /// <exception cref="InvalidCastException">The value cannot be cast to <typeparamref name="T"/>.</exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T? Cast<T>( object? obj ) where T : class
        {
            if (obj == null)
            {
                return null;
            }
            if (obj is T r)
            {
                return r;
            }
            return CastSafe<T>(obj) ?? throw new InvalidCastException($"Unable to cast {obj.GetType().FullName} to {typeof(T).FullName}");
        }

        /// <summary>
        /// Wraps a thrown Haxe value in a managed exception.
        /// </summary>
        /// <param name="value">The thrown value.</param>
        /// <returns>An exception that represents the thrown value.</returns>
        public static Exception WrapException( object? value )
        {
            if (value is IHashlinkPointer ptr)
            {
                return new HashlinkError(ptr.HashlinkPointer);
            }
            return value as Exception ?? new HaxeThrownException(value);
        }
        /// <summary>
        /// Unwraps a managed exception back into a Haxe value.
        /// </summary>
        /// <typeparam name="T">The type of the thrown value.</typeparam>
        /// <param name="ex">The exception to unwrap.</param>
        /// <returns>The unwrapped Haxe value.</returns>
        [StackTraceHidden]
        public static T UnwrapException<T>( Exception ex )
        {
            if (ex is HaxeThrownException thrown)
            {
                return (T)thrown.Value!;
            }
            if (ex is HashlinkError err && err.Error != 0)
            {
                var obj = HashlinkMarshal.ConvertHashlinkObject((void*)err.Error);
                return (T)HaxeProxyHelper.GetProxy<T>(obj)!;
            }
            ExceptionDispatchInfo.Capture(ex).Throw();
            return default!;
        }

        /// <summary>
        /// Gets the native Hashlink type pointer for <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The .NET type.</typeparam>
        /// <returns>The native type pointer.</returns>
        public static nint GetTypePointer<T>()
        {
            return (nint)HashlinkMarshal.Module.PreferTypes[GetTypeIndex(typeof(T))].NativeType;
        }
        /// <summary>
        /// Gets the native Hashlink type pointer for the void type.
        /// </summary>
        /// <returns>The native void type pointer, or zero if it is unavailable.</returns>
        public static nint GetVoidTypePointer()
        {
            var type = HashlinkMarshal.GetHashlinkType(typeof(void));
            return type == null ? 0 : (nint)type.NativeType;
        }

        /// <summary>
        /// Gets the native Hashlink type pointer for the runtime type of a value.
        /// </summary>
        /// <param name="value">The value whose runtime type should be resolved.</param>
        /// <returns>The native type pointer.</returns>
        public static nint GetRuntimeTypePointer( object? value )
        {
            if (value == null)
            {
                return (nint)HashlinkMarshal.Module.KnownTypes.Void.NativeType;
            }
            if (value is HashlinkObj hobj)
            {
                return (nint)hobj.NativeType;
            }
            if (value is HaxeProxyBase proxy)
            {
                return (nint)proxy.HashlinkObj.Type.NativeType;
            }
            var t = HashlinkMarshal.GetHashlinkType(value.GetType());
            Debug.Assert(t != null);
            return (nint)t.NativeType;
        }
        /// <summary>
        /// Gets the Hashlink type kind for the specified native type pointer.
        /// </summary>
        /// <param name="value">The native type pointer.</param>
        /// <returns>The type kind, or zero if the pointer is null.</returns>
        public static int GetTypeId( nint value )
        {
            var t = (HL_type*)value;
            if (t == null)
            {
                return 0;
            }
            return (int)t->kind;
        }

        /// <summary>
        /// Returns the referenced value of a <see cref="Ref{T}"/>.
        /// </summary>
        /// <typeparam name="T">The referenced value type.</typeparam>
        /// <param name="value">The reference to unwrap.</param>
        /// <returns>A reference to the underlying value.</returns>
        public static ref T Unref<T>( Ref<T> value )
        {
            return ref value.value;
        }
        /// <summary>
        /// Creates a <see cref="Ref{T}"/> for the specified value.
        /// </summary>
        /// <typeparam name="T">The referenced value type.</typeparam>
        /// <param name="value">The value to reference.</param>
        /// <returns>A reference wrapping <paramref name="value"/>.</returns>
        public static Ref<T> MakeRef<T>( ref T value )
        {
            return Ref<T>.From(ref value);
        }
        /// <summary>
        /// Wraps a value in a nullable value.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="value">The value to wrap.</param>
        /// <returns>The nullable value.</returns>
        public static T? MakeNullable<T>( T value ) where T : struct
        {
            return value;
        }
        /// <summary>
        /// Creates an empty nullable value.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <returns>A nullable value without a value.</returns>
        public static T? NullNullable<T>() where T : struct
        {
            return default;
        }
        /// <summary>
        /// Gets the value of a nullable value, or the default value if it is empty.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="value">The nullable value.</param>
        /// <returns>The wrapped value, or <see langword="default"/> if it is empty.</returns>
        public static T GetNullableValue<T>( T? value ) where T : struct
        {
            return value ?? default;
        }
        /// <summary>
        /// Converts a .NET string to a Haxe string of the specified proxy type.
        /// </summary>
        /// <typeparam name="T">The Haxe string proxy type.</typeparam>
        /// <param name="value">The .NET string.</param>
        /// <returns>The Haxe string proxy.</returns>
        public static T AsHaxeString<T>( string value ) where T : HaxeProxyBase
        {
            return new HashlinkString(value).AsHaxe<T>();
        }
        /// <summary>
        /// Converts a .NET string to a pointer to its Haxe string bytes.
        /// </summary>
        /// <param name="value">The .NET string.</param>
        /// <returns>A pointer to the Haxe string bytes.</returns>
        public static nint AsHaxeStringPtr( string value )
        {
            var proxy = new HashlinkString(value);
            return (nint)((HL_vstring*)proxy.HashlinkPointer)->bytes;
        }

        internal static int GetTypeIndex( Type type )
        {
            if (HaxeProxyManager.type2typeId.TryGetValue(type, out var index))
            {
                return index;
            }
            if (type.GetCustomAttribute<HashlinkTIndexAttribute>(true) is { } attr)
            {
                return attr.Index;
            }
            var ht = HashlinkMarshal.GetHashlinkType(type) ??
                throw new InvalidOperationException($"Cannot resolve hashlink type for {type}");
            return ht.TypeIndex;
        }

        /// <summary>
        /// Compares two values using Haxe enum semantics where applicable.
        /// </summary>
        /// <param name="a">The first value.</param>
        /// <param name="b">The second value.</param>
        /// <returns><see langword="true"/> if the values are equal; otherwise, <see langword="false"/>.</returns>
        public static bool Compare(object a, object b)
        {
            if (a == b)
            {
                return true;
            }
            if (a == null && b != null)
            {
                return false;
            }
            if (a != null && b == null)
            {
                return false;
            }
            
            if (a is HaxeEnum ae)
            {
                return ae.Equals(b);
            }
            return a!.Equals(b);
        }
    }

    internal sealed class HaxeThrownException( object? value ) : Exception
    {
        public object? Value {
            get;
        } = value;

        public override string Message => Value?.ToString() ?? "<null>";
    }
}


