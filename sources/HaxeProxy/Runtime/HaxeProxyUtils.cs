using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection.Members;
using Hashlink.Reflection.Types;
using HaxeProxy.Runtime.Internals;
using HaxeProxy.Runtime.Internals.Inheritance;
using System.Reflection;

namespace HaxeProxy.Runtime
{
    /// <summary>
    /// Provides helpers for working with Haxe proxy types and Hashlink objects.
    /// </summary>
    public static class HaxeProxyUtils
    {
        private static readonly FieldInfo f_next_call_origin_function = typeof(HaxeProxyManager)
            .GetField(nameof(HaxeProxyManager.next_call_origin_function))!;
        internal static void UseCSharpVersionCode( Delegate del, bool throwOnCompiled )
        {
            UseCSharpVersionCode(del.Method, throwOnCompiled);
        }
        /// <summary>
        /// Switches all eligible methods of the specified type to their C# implementation.
        /// </summary>
        /// <param name="t">The type whose methods should use the C# implementation.</param>
        /// <param name="throwOnCompiled">Whether to throw when a method has already been compiled by .NET.</param>
        public static void UseCSharpVersionCode( Type t, bool throwOnCompiled )
        {
            foreach (var v in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                if (v.GetCustomAttribute<HashlinkFIndexAttribute>() is not null &&
                    v.GetCustomAttribute<HashlinkShouldUseNativeAttribute>() is null)
                {
                    UseCSharpVersionCode(v, throwOnCompiled);
                }
            }
        }
        /// <summary>
        /// Switches the specified method to its C# implementation.
        /// </summary>
        /// <param name="m">The method to switch.</param>
        /// <param name="throwOnCompiled">Whether to throw when the method has already been compiled by .NET.</param>
        /// <exception cref="InvalidOperationException">The method cannot be switched to the C# implementation.</exception>
        public static void UseCSharpVersionCode( MethodInfo m, bool throwOnCompiled )
        {
            if (!HaxeProxyManager.generatedPseudocode)
            {
                throw new InvalidOperationException();
            }

            if (m.GetCustomAttribute<HashlinkShouldUseNativeAttribute>(false) != null)
            {
                throw new InvalidOperationException();
            }

            if (m.GetCustomAttribute<HashlinkFIndexAttribute>(false) is not HashlinkFIndexAttribute fidx)
            {
                throw new InvalidOperationException();
            }

            var ifunc = HashlinkMarshal.Module.GetFunctionByFIndex(fidx.Index);
            if (ifunc is not HashlinkFunction func)
            {
                throw new InvalidOperationException();
            }

            if (func.IsDotnetCompiled)
            {
                if (throwOnCompiled)
                {
                    throw new InvalidOperationException();
                }
                return;
            }

            func.UseDotnetCompiledVersion(m, f_next_call_origin_function);
        }
        /// <summary>
        /// Gets the managed proxy associated with the specified Hashlink object.
        /// </summary>
        /// <param name="obj">The Hashlink object.</param>
        /// <returns>The managed proxy for the object.</returns>
        public static HaxeProxyBase AsHaxe( this HashlinkObj obj )
        {
            return ((IExtraData)obj).GetData<HaxeProxyBase>();
        }
        /// <summary>
        /// Gets the Haxe object represented by the specified Hashlink object.
        /// </summary>
        /// <param name="obj">The Hashlink object.</param>
        /// <returns>The represented Haxe object.</returns>
        /// <exception cref="InvalidCastException">The object does not represent a Haxe object.</exception>
        public static HaxeProxyBase AsObject( this HashlinkObj obj )
        {
            if (obj is HashlinkVirtual virt)
            {
                return virt.GetValue()?.AsHaxe() ?? throw new InvalidCastException();
            }
            return obj.AsHaxe();
        }
        /// <summary>
        /// Gets the managed proxy associated with the specified Hashlink object.
        /// </summary>
        /// <typeparam name="T">The expected proxy type.</typeparam>
        /// <param name="obj">The Hashlink object.</param>
        /// <returns>The proxy cast to <typeparamref name="T"/>.</returns>
        public static T AsHaxe<T>( this HashlinkObj obj ) where T : HaxeProxyBase
        {
            return (T)obj.AsHaxe();
        }
        /// <summary>
        /// Gets the Hashlink type that corresponds to the specified .NET type.
        /// </summary>
        /// <param name="type">The .NET type.</param>
        /// <returns>The corresponding Hashlink type.</returns>
        /// <exception cref="InvalidOperationException">No Hashlink type is registered for the specified type.</exception>
        public static HashlinkType GetHashlinkType( Type type )
        {
            return HashlinkMarshal.GetHashlinkType(type) ??
                throw new InvalidOperationException();
        }
        /// <summary>
        /// Gets the managed proxy type that corresponds to the specified Hashlink type.
        /// </summary>
        /// <param name="type">The Hashlink type.</param>
        /// <returns>The corresponding managed proxy type.</returns>
        public static Type GetProxyType( HashlinkType type )
        {
            return HaxeProxyManager.GetTypeFromHashlinkType(type);
        }
        /// <summary>
        /// Gets the global class value for the specified .NET type.
        /// </summary>
        /// <typeparam name="TClass">The expected proxy type.</typeparam>
        /// <param name="type">The .NET type whose global value should be retrieved.</param>
        /// <returns>The global class proxy cast to <typeparamref name="TClass"/>.</returns>
        public static TClass GetClass<TClass>( Type type ) where TClass : HaxeProxyBase
        {
            return ((HashlinkObjectType)GetHashlinkType(type)).GlobalValue.AsHaxe<TClass>();
        }
        /// <summary>
        /// Gets the global class value for the specified .NET type.
        /// </summary>
        /// <typeparam name="TType">The .NET type whose global value should be retrieved.</typeparam>
        /// <typeparam name="TClass">The expected proxy type.</typeparam>
        /// <returns>The global class proxy cast to <typeparamref name="TClass"/>.</returns>
        public static TClass GetClass<TType, TClass>() where TClass : HaxeProxyBase
        {
            return GetClass<TClass>(typeof(TType));
        }
    }
}
