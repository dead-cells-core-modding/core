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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object? GetFieldById<T>( HaxeProxyBase self, string name, ref ObjFieldInfoCache cache )
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
                return (T?) GetProxy<T>(
                    ((IHashlinkFieldObject)self.HashlinkObj).GetFieldValue(name)
                    );
            }
            Debug.Assert(cache.field != null);
            var result = HashlinkMarshal.ReadData((void*)(self.HashlinkPointer + cache.offset),
                cache.field);

            if (typeof(T) == typeof(object) &&
                cache.isConstructor &&
                result is HashlinkClosure ctorClosure)
            {
                return ctorClosure.NoClosure;
            }

            return (T?) GetProxy<T>(result);
        }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetFieldById( HaxeProxyBase self, object? value, string name, ref ObjFieldInfoCache cache )
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
            HashlinkMarshal.WriteData((void*)(self.HashlinkPointer + cache.offset),
                value, cache.field);
        }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ToObject<T>( HaxeProxyBase val ) where T : HaxeObject
        {
            Debug.Assert(val.GetType() != null);
            return val.AsObject<T>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ToVirtual<T>( HaxeProxyBase val ) where T : HaxeVirtual
        {
            Debug.Assert(val.GetType() != null);
            return ((IExtraData)val).GetData<VirtualCastCache<T>>().Value;
        }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashlinkObj CreateInstance( int typeIndex )
        {
            HashlinkMarshal.EnsureThreadRegistered();
            return HashlinkMarshal.Module.PreferTypes[typeIndex].CreateInstance();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashlinkEnum CreateEnumInstance( int typeIndex, int elIndex )
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var t = (HashlinkEnumType)HashlinkMarshal.Module.PreferTypes[typeIndex];
            return new HashlinkEnum(t, elIndex);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTypeIndexFromType<T>( ref int cachedValue )
        {
            if (cachedValue > 0)
            {
                return cachedValue;
            }
            return cachedValue = HaxeProxyManager.type2typeId[typeof(T)];
        }
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
                    if (HaxeProxyManager.next_call_origin_function == findex)
                    {
                        return null;
                    }
                    HaxeProxyManager.next_call_origin_function = -1;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddHook( int findex, Delegate hook )
        {
            HaxeHookManager.AddHook(findex, hook);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveHook( int findex, Delegate hook )
        {
            HaxeHookManager.RemoveHook(findex, hook);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCSharpCustomType( Type type )
        {
            return type.Assembly != HaxeProxyManager.proxyAssembly;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ref<T> GetRef<T>(ref T val) where T : unmanaged
        {
            return Ref<T>.From(ref val);
        }
    }
}
