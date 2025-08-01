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
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime.Internals
{
    public static unsafe class HaxeProxyHelper
    {
        [ThreadStatic]
        private static bool nextCallOrig;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureFieldInfo( HaxeProxyBase self, string name, ref ObjFieldInfoCache cache )
        {
            if (!cache.hasCache)
            {
                var t = self.HashlinkObj.Type;
                if (t is HashlinkObjectType ot)
                {
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            return GetProxy<T>(HashlinkMarshal.ReadData((void*)(self.HashlinkPointer + cache.offset),
                cache.field));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HaxeProxyBase? GetGlobal( int globalIndex, ref HaxeProxyBase? cache )
        {
            if (cache != null)
            {
                return cache;
            }

            return cache = (HaxeProxyBase?)GetProxy<HaxeProxyBase>(
                HashlinkMarshal.Module.Globals[globalIndex].Value
                );
        }
        [return: NotNullIfNotNull(nameof(val))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                return HashlinkObjDynamicAccess.Create(dyn);
            }
            if (val is IExtraData ied)
            {
                return ied.GetData<HaxeProxyBase>();
            }
            return val;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: NotNullIfNotNull(nameof(val))]
        public static HaxeNullable<T>? GetNullableProxy<T>( object? val ) where T : struct
        {
            if (val == null)
            {
                return null;
            }
            return (T)val;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashlinkObj CreateInstance( int typeIndex )
        {
            return HashlinkMarshal.Module.Types[typeIndex].CreateInstance();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashlinkEnum CreateEnumInstance( int typeIndex, int elIndex )
        {
            var t = (HashlinkEnumType)HashlinkMarshal.Module.Types[typeIndex];
            return new HashlinkEnum(t, elIndex);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddHook( int findex, Delegate hook )
        {
            HaxeHookManager.AddHook( findex, hook );
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveHook( int findex, Delegate hook )
        {
            HaxeHookManager.RemoveHook( findex, hook );
        }
    }
}
