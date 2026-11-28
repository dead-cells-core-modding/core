using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;
using Hashlink.Proxy.Values;
using Hashlink.Reflection.Members;
using Hashlink.Reflection.Members.Object;
using Hashlink.Reflection.Types;
using Hashlink.UnsafeUtilities;
using Hashlink.Wrapper;
using HaxeProxy.Runtime.Internals.Cache;
using HaxeProxy.Runtime.Internals.Hooks;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HaxeProxy.Runtime.Internals
{
    /// <summary>
    /// Provides low-level helpers used by generated Haxe proxies.
    /// </summary>
    public static unsafe class HaxeProxyHelper
    {
        private class VirtualCastCache<T>( T value ) : IExtraDataItem where T : HaxeVirtual
        {
            public static object Create( HashlinkObj obj )
            {
                return new VirtualCastCache<T>(obj.AsHaxe().ToVirtual<T>());
            }
            public T Value => value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureFieldInfo( HaxeProxyBase self, string name, ref ObjFieldInfoCache cache )
        {
            if (!cache.hasCache)
            {
                var t = self.HashlinkObj.Type;
                if (t is HashlinkObjectType ot)
                {
                    if (name == "__constructor__"
                        && ot.Super?.Name == "hl.Class")
                    {
                        cache.isConstructor = true;
                    }
                    HashlinkObjectField? f;

                    if (name.Length > 0 && name[0] == '#')
                    {
                        var idx = int.Parse(name[1..]);
                        f = ot.FindFieldById(idx) ??
                        throw new MissingFieldException(ot.Name, name);
                    }
                    else
                    {
                        f = ot.FindField(name) ??
                        throw new MissingFieldException(ot.Name, name);
                    }

                    Debug.Assert(f.Index < t.NativeType->data.obj->rt->nfields);
                    cache.field = f.FieldType;
                    cache.offset = t.NativeType->data.obj->rt->fields_indexes[f.Index];

                    var memsize = HashlinkObjPtr.Get(self.HashlinkPointer).GetMemSize();

                    Debug.Assert(cache.offset >= 0);
                    Debug.Assert(memsize == -1 || cache.offset < memsize);
                }
                else if (t is HashlinkEnumType et)
                {
                    var idx = int.Parse(name);
                    var pid = idx & 0xffff;
                    var c = et.Constructs[idx >> 16];
                    cache.field = c.Params[pid];
                    cache.offset = c.ParamOffsets[pid];
                }
                else
                {
                    cache.offset = 0;
                }
                Interlocked.MemoryBarrier();
                cache.hasCache = true;
            }
        }
        /// <summary>
        /// Gets a managed proxy for the specified field of a Haxe object.
        /// </summary>
        /// <typeparam name="T">The expected proxy type.</typeparam>
        /// <param name="self">The Haxe object proxy.</param>
        /// <param name="name">The field name.</param>
        /// <param name="cache">The field information cache.</param>
        /// <param name="valueCache">The cached field value.</param>
        /// <returns>The proxied field value, or <see langword="null"/> if the field is empty.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object? GetFieldById<T>( HaxeProxyBase self, string name,
            ref ObjFieldInfoCache cache, ref object? valueCache )
            where T : class
        {
            Debug.Assert(self.GetType() != null);
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            EnsureFieldInfo(self, name, ref cache);
            if (cache.offset == 0)
            {
                return (T?)GetProxy<T>(
                    ((IHashlinkFieldObject)self.HashlinkObj).GetFieldValue(name)
                    );
            }
            Debug.Assert(cache.field != null);
            Debug.Assert(cache.field.TypeKind.IsManagedPointer());

            var fieldValuePtr = (void**)(self.HashlinkPointer + cache.offset);
            var valuePtr = *fieldValuePtr;

            if (valuePtr == null)
            {
                if (!Unsafe.IsNullRef(ref valueCache))
                {
                    valueCache = null;
                }
                return null;
            }

            if (!Unsafe.IsNullRef(ref valueCache))
            {
                if (valueCache is HaxeProxyBase hobj)
                {
                    if (hobj.HashlinkPointer == (nint)valuePtr)
                    {
                        return hobj;
                    }
                }
            }

            var result = HashlinkMarshal.ReadData(fieldValuePtr,
                cache.field);
            object? presult;

            if (typeof(T) == typeof(object) &&
                cache.isConstructor &&
                result is HashlinkClosure ctorClosure)
            {
                presult = ctorClosure.NoClosure;
            }
            else
            {
                presult = (T?)GetProxy<T>(result);
            }

            if (!Unsafe.IsNullRef(ref valueCache))
            {
                valueCache = presult;
            }
            return presult;
        }
        /// <summary>
        /// Gets the unmanaged value of the specified field of a Haxe object.
        /// </summary>
        /// <typeparam name="T">The unmanaged field value type.</typeparam>
        /// <param name="self">The Haxe object proxy.</param>
        /// <param name="name">The field name.</param>
        /// <param name="cache">The field information cache.</param>
        /// <returns>The field value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetValueFieldById<T>( HaxeProxyBase self, string name, ref ObjFieldInfoCache cache )
            where T : unmanaged
        {
            Debug.Assert(self.GetType() != null);
            if (string.IsNullOrEmpty(name))
            {
                return default;
            }
            EnsureFieldInfo(self, name, ref cache);
            if (cache.offset == 0)
            {
                return (T)((IHashlinkFieldObject)self.HashlinkObj).GetFieldValue(name)!;
            }
            return *(T*)(self.HashlinkPointer + cache.offset);
        }
        /// <summary>
        /// Sets the value of the specified field of a Haxe object.
        /// </summary>
        /// <param name="self">The Haxe object proxy.</param>
        /// <param name="value">The value to assign.</param>
        /// <param name="name">The field name.</param>
        /// <param name="cache">The field information cache.</param>
        /// <param name="valueCache">The cached field value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetFieldById( HaxeProxyBase self, object? value, string name, 
            ref ObjFieldInfoCache cache, ref object? valueCache )
        {
            Debug.Assert(self.GetType() != null);
            Debug.Assert(value == null || value?.GetType() != null);

            EnsureFieldInfo(self, name, ref cache);
            if (cache.offset == 0)
            {
                ((IHashlinkFieldObject)self.HashlinkObj).SetFieldValue(name, value);
                return;
            }
            Debug.Assert(cache.field != null);

            if (!Unsafe.IsNullRef(ref valueCache))
            {
                valueCache = value as HaxeProxyBase;
            }
            HashlinkMarshal.WriteData((void*)(self.HashlinkPointer + cache.offset),
                value, cache.field);
        }
        /// <summary>
        /// Sets the unmanaged value of the specified field of a Haxe object.
        /// </summary>
        /// <typeparam name="T">The unmanaged field value type.</typeparam>
        /// <param name="self">The Haxe object proxy.</param>
        /// <param name="value">The value to assign.</param>
        /// <param name="name">The field name.</param>
        /// <param name="cache">The field information cache.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValueFieldById<T>( HaxeProxyBase self, T value, string name, ref ObjFieldInfoCache cache )
            where T : unmanaged
        {
            Debug.Assert(self.GetType() != null);
            EnsureFieldInfo(self, name, ref cache);
            if (cache.offset == 0)
            {
                ((IHashlinkFieldObject)self.HashlinkObj).SetFieldValue(name, value);
                return;
            }
            *(T*)(self.HashlinkPointer + cache.offset) = value;
        }
        /// <summary>
        /// Gets the global value at the specified index.
        /// </summary>
        /// <param name="globalIndex">The global value index.</param>
        /// <param name="cache">The cached proxy for the global value.</param>
        /// <returns>The proxied global value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HaxeProxyBase? GetGlobal( int globalIndex, ref HaxeProxyBase? cache )
        {
            var global = (HashlinkObj?)HashlinkMarshal.Module.Globals[globalIndex].Value;

            Debug.Assert(global != null);

            if (cache != null && cache.HashlinkPointer == global.HashlinkPointer)
            {
                return cache;
            }

            return cache = (HaxeProxyBase?)GetProxy<HaxeProxyBase>(
               global
                );
        }
        /// <summary>
        /// Converts a Haxe proxy to a Haxe object.
        /// </summary>
        /// <typeparam name="T">The expected Haxe object type.</typeparam>
        /// <param name="val">The value to convert.</param>
        /// <returns>The converted Haxe object.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ToObject<T>( HaxeProxyBase val ) where T : HaxeObject
        {
            Debug.Assert(val.GetType() != null);
            return val.AsObject<T>();
        }
        /// <summary>
        /// Converts a Haxe proxy to a Haxe virtual object.
        /// </summary>
        /// <typeparam name="T">The expected virtual proxy type.</typeparam>
        /// <param name="val">The value to convert.</param>
        /// <returns>The converted virtual object.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ToVirtual<T>( HaxeProxyBase val ) where T : HaxeVirtual
        {
            Debug.Assert(val.GetType() != null);
            return ((IExtraData)val).GetData<VirtualCastCache<T>>().Value;
        }
        /// <summary>
        /// Converts a value to a managed proxy of the requested type.
        /// </summary>
        /// <typeparam name="T">The requested proxy type.</typeparam>
        /// <param name="val">The value to convert.</param>
        /// <returns>The converted proxy, or <see langword="null"/> if <paramref name="val"/> is <see langword="null"/>.</returns>
        [return: NotNullIfNotNull(nameof(val))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object? GetProxy<T>( object? val )
        {
            if (val == null)
            {
                return null;
            }

            Debug.Assert(val.GetType() != null);

            if (val is T && typeof(T) != typeof(object))
            {
                return val;
            }
            if (typeof(T) == typeof(object) && val is HashlinkClosure cl)
            {
                return cl;
            }
            if (typeof(T).IsAssignableTo(typeof(Delegate)))
            {
                if (val is Delegate d)
                {
                    return d.CreateAdaptDelegate(typeof(T));
                }
                if (val is HashlinkClosure closure)
                {
                    return closure.CreateDelegate(typeof(T));
                }
            }
            if (val is IExtraData ied)
            {
                return ied.GetData<HaxeProxyBase>();
            }
            return (T)val;
        }

        /// <summary>
        /// Creates an instance of the Hashlink type at the specified index.
        /// </summary>
        /// <param name="typeIndex">The Hashlink type index.</param>
        /// <returns>The created Hashlink object.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashlinkObj CreateInstance( int typeIndex )
        {
            HashlinkMarshal.EnsureThreadRegistered();
            return HashlinkMarshal.Module.PreferTypes[typeIndex].CreateInstance();
        }

        /// <summary>
        /// Creates a Hashlink enum value for the specified type and constructor.
        /// </summary>
        /// <param name="typeIndex">The Hashlink enum type index.</param>
        /// <param name="elIndex">The enum constructor index.</param>
        /// <returns>The created enum value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashlinkEnum CreateEnumInstance( int typeIndex, int elIndex )
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var t = (HashlinkEnumType)HashlinkMarshal.Module.PreferTypes[typeIndex];
            return new HashlinkEnum(t, elIndex);
        }
        /// <summary>
        /// Gets the Hashlink type index for <typeparamref name="T"/>, caching the result.
        /// </summary>
        /// <typeparam name="T">The .NET type.</typeparam>
        /// <param name="cachedValue">The cached type index.</param>
        /// <returns>The Hashlink type index.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTypeIndexFromType<T>( ref int cachedValue )
        {
            if (cachedValue > 0)
            {
                return cachedValue;
            }
            return cachedValue = HaxeProxyManager.type2typeId[typeof(T)];
        }
        /// <summary>
        /// Gets or creates call information for the specified Hashlink function index.
        /// </summary>
        /// <param name="findex">The Hashlink function index.</param>
        /// <param name="cache">The function information cache.</param>
        /// <returns>The call information, or <see langword="null"/> if the function should not be called directly.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DelegateInfo? GetCallInfoById( int findex, ref FunctionInfoCache cache )
        {
            if (cache.function == null)
            {
                cache.function = (HashlinkFunction)HashlinkMarshal.Module.GetFunctionByFIndex(findex);
            }

            if(cache.function.IsDotnetCompiled)
            {
                Debug.Assert(HaxeProxyManager.function_is_hooked_mark != null);
                if (HaxeProxyManager.function_is_hooked_mark[findex])
                {
                    var ncof = HaxeProxyManager.next_call_origin_function;
                    HaxeProxyManager.next_call_origin_function = -1;
                    if (ncof == findex)
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }

            if (cache.directEntry != null)
            {
                return cache.directEntry;
            }
            if (cache.directEntry == null)
            {
                cache.directEntry = HashlinkWrapperFactory.GetWrapperInfo(cache.function.FuncType,
                    cache.function.EntryPointer);
            }
            return cache.directEntry;
        }
        /// <summary>
        /// Gets or creates a delegate that invokes the specified native function.
        /// </summary>
        /// <typeparam name="T">The delegate type.</typeparam>
        /// <param name="index">The Hashlink function index.</param>
        /// <param name="cache">The cached delegate.</param>
        /// <returns>The delegate that invokes the native function.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Delegate GetNativeCall<T>( int index, ref Delegate? cache ) where T : Delegate
        {
            if (cache != null)
            {
                return cache;
            }
            var ptr = (HashlinkNativeFunction)HashlinkMarshal.Module.GetFunctionByFIndex(
                //HashlinkMarshal.Module.NativeCode->natives[index].findex
                index
                );
            cache = ptr.CreateDelegate<T>();
            return cache;
        }
        /// <summary>
        /// Adds a hook to the Hashlink function at the specified index.
        /// </summary>
        /// <param name="findex">The Hashlink function index.</param>
        /// <param name="hook">The hook delegate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddHook( int findex, Delegate hook )
        {
            HaxeHookManager.AddHook(findex, hook);
        }
        /// <summary>
        /// Removes a hook from the Hashlink function at the specified index.
        /// </summary>
        /// <param name="findex">The Hashlink function index.</param>
        /// <param name="hook">The hook delegate to remove.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveHook( int findex, Delegate hook )
        {
            HaxeHookManager.RemoveHook(findex, hook);
        }
        /// <summary>
        /// Determines whether the specified type is a user-defined Haxe proxy type rather than a generated one.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns><see langword="true"/> if the type is user-defined; otherwise, <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCSharpCustomType( Type type )
        {
            return type.Assembly != HaxeProxyManager.proxyAssembly;
        }
        /// <summary>
        /// Creates a reference wrapper for the specified value.
        /// </summary>
        /// <typeparam name="T">The unmanaged value type.</typeparam>
        /// <param name="val">The value to reference.</param>
        /// <returns>A reference wrapping <paramref name="val"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ref<T> GetRef<T>(ref T val) where T : unmanaged
        {
            return Ref<T>.From(ref val);
        }
    }
}
