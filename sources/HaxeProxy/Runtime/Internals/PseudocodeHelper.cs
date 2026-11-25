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
    public unsafe class PseudocodeHelper
    {

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

        public static object? DynGet( object? obj, string name )
        {
            if (obj is HaxeProxyBase proxy &&
                proxy.HashlinkObj is IHashlinkFieldObject f)
            {
                return HaxeProxyHelper.GetProxy<object>(f.GetFieldValue(name));
            }
            return null;
        }
        public static void DynSet( object? obj, string name, object? value )
        {
            if (obj is HaxeProxyBase proxy &&
                proxy.HashlinkObj is IHashlinkFieldObject f)
            {
                f.SetFieldValue(name, value);
            }
        }

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

        public static Exception WrapException( object? value )
        {
            if (value is IHashlinkPointer ptr)
            {
                return new HashlinkError(ptr.HashlinkPointer);
            }
            return value as Exception ?? new HaxeThrownException(value);
        }
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

        public static nint GetTypePointer<T>()
        {
            return (nint)HashlinkMarshal.Module.PreferTypes[GetTypeIndex(typeof(T))].NativeType;
        }
        public static nint GetVoidTypePointer()
        {
            var type = HashlinkMarshal.GetHashlinkType(typeof(void));
            return type == null ? 0 : (nint)type.NativeType;
        }

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
        public static int GetTypeId( nint value )
        {
            var t = (HL_type*)value;
            if (t == null)
            {
                return 0;
            }
            return (int)t->kind;
        }

        public static ref T Unref<T>( Ref<T> value )
        {
            return ref value.value;
        }
        public static Ref<T> MakeRef<T>( ref T value )
        {
            return Ref<T>.From(ref value);
        }
        public static T? MakeNullable<T>( T value ) where T : struct
        {
            return value;
        }
        public static T? NullNullable<T>() where T : struct
        {
            return default;
        }
        public static T GetNullableValue<T>( T? value ) where T : struct
        {
            return value ?? default;
        }
        public static T AsHaxeString<T>( string value ) where T : HaxeProxyBase
        {
            return new HashlinkString(value).AsHaxe<T>();
        }
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


