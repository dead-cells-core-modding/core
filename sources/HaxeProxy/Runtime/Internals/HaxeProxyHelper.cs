using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.DynamicAccess;
using Hashlink.Proxy.Objects;
using Hashlink.Proxy.Values;
using Hashlink.Reflection.Members;
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
        private class VirtualCastCache<T>(T value) : IExtraDataItem where T : HaxeVirtual
        {
            public static object Create( HashlinkObj obj )
            {
                return new VirtualCastCache<T>(obj.AsHaxe().ToVirtual<T>());
            }
            public T Value => value;
        }

        [ThreadStatic]
        private static bool nextCallOrig;
       
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

                    var f = ot.FindField(name) ??
                        throw new MissingFieldException(ot.Name, name);
                    cache.field = f.FieldType;
                    cache.offset = (nint)HashlinkNative.hl_obj_lookup((HL_vdynamic*)self.HashlinkPointer,
                        f.HashedName, out _) - self.HashlinkPointer;
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
       
        public static object? GetFieldById<T>( HaxeProxyBase self, string name, ref ObjFieldInfoCache cache )
            where T : class
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            EnsureFieldInfo(self, name, ref cache);
            if (cache.offset == 0)
            {
                return GetProxy<T>(
                    ((IHashlinkFieldObject) self.HashlinkObj).GetFieldValue(name)
                    );
            }
            var result = HashlinkMarshal.ReadData((void*)(self.HashlinkPointer + cache.offset),
                cache.field);

            if (typeof(T) == typeof(object) && 
                cache.isConstructor &&
                result is HashlinkClosure ctorClosure)
            {
                return ctorClosure.NoClosure;
            }

            return GetProxy<T>(result);
        }
       
        public static T GetValueFieldById<T>( HaxeProxyBase self, string name, ref ObjFieldInfoCache cache )
            where T : unmanaged
        {
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
       
        public static void SetFieldById( HaxeProxyBase self, object? value, string name, ref ObjFieldInfoCache cache )
        {
            EnsureFieldInfo(self, name, ref cache);
            if (cache.offset == 0)
            {
                ((IHashlinkFieldObject)self.HashlinkObj).SetFieldValue(name, value);
                return;
            }
            HashlinkMarshal.WriteData((void*)(self.HashlinkPointer + cache.offset),
                value, cache.field);
        }
       
        public static void SetValueFieldById<T>( HaxeProxyBase self, T value, string name, ref ObjFieldInfoCache cache )
            where T : unmanaged
        {
            EnsureFieldInfo(self, name, ref cache);
            if (cache.offset == 0)
            {
                ((IHashlinkFieldObject)self.HashlinkObj).SetFieldValue(name, value);
                return;
            }
            *(T*)(self.HashlinkPointer + cache.offset) = value;
        }
       
        public static HaxeProxyBase? GetGlobal( int globalIndex, ref HaxeProxyBase? cache )
        {
            var global = (HashlinkObj?) HashlinkMarshal.Module.Globals[globalIndex].Value;

            Debug.Assert(global != null);

            if (cache != null && cache.HashlinkPointer == global.HashlinkPointer)
            {
                return cache;
            }

            return cache = (HaxeProxyBase?)GetProxy<HaxeProxyBase>(
               global
                );
        }
        public static T ToObject<T>( HaxeProxyBase val )  where T : HaxeObject
        {
            return val.AsObject<T>();
        }
       
        public static T ToVirtual<T>( HaxeProxyBase val ) where T : HaxeVirtual
        {
            return ((IExtraData)val).GetData<VirtualCastCache<T>>().Value;
        }
        [return: NotNullIfNotNull(nameof(val))]
       
        public static object? GetProxy<T>( object? val )
        {
            if (val == null)
            {
                return null;
            }
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
            if (val is HashlinkDynObj dyn)
            {
                return dyn.AsDynamic();
            }
            if (val is IExtraData ied)
            {
                return ied.GetData<HaxeProxyBase>();
            }
            return val;
        }
       
        [return: NotNullIfNotNull(nameof(val))]
        public static HaxeNullable<T>? GetNullableProxy<T>( object? val ) where T : struct
        {
            if (val == null)
            {
                return null;
            }
            return (T)val;
        }
       
        public static HashlinkObj CreateInstance( int typeIndex )
        {
            HashlinkMarshal.EnsureThreadRegistered();
            return HashlinkMarshal.Module.Types[typeIndex].CreateInstance();
        }
       
        public static HashlinkEnum CreateEnumInstance( int typeIndex, int elIndex )
        {
            HashlinkMarshal.EnsureThreadRegistered();
            var t = (HashlinkEnumType)HashlinkMarshal.Module.Types[typeIndex];
            return new HashlinkEnum(t, elIndex);
        }
       
        public static int GetTypeIndexFromType<T>( ref int cachedValue )
        {
            if (cachedValue > 0)
            {
                return cachedValue;
            }
            return cachedValue = HaxeProxyManager.type2typeId[typeof(T)];
        }
       
        public static DelegateInfo GetCallInfoById( int findex, ref FunctionInfoCache cache )
        {
            if (cache.function == null)
            {
                cache.function = (HashlinkFunction) HashlinkMarshal.Module.GetFunctionByFIndex(findex);
            }
            if (nextCallOrig)
            {
                nextCallOrig = false;
                if (cache.hookRealEntry == null)
                {
                    cache.hookRealEntry = HashlinkWrapperFactory.GetWrapperInfo(cache.function.FuncType,
                        cache.function.EntryPointer + HashlinkFunction.FS_OFFSET_REAL_ENTRY);
                }
                return cache.hookRealEntry;
            }
            else
            {
                if (cache.directEntry == null)
                {
                    cache.directEntry = HashlinkWrapperFactory.GetWrapperInfo(cache.function.FuncType,
                        cache.function.EntryPointer);
                }
                return cache.directEntry;
            }
        }
       
        public static void AddHook( int findex, Delegate hook )
        {
            HaxeHookManager.AddHook( findex, hook );
        }
       
        public static void RemoveHook( int findex, Delegate hook )
        {
            HaxeHookManager.RemoveHook( findex, hook );
        }
    }
}
