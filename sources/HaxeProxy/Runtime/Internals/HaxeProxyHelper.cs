using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
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

        private static void EnsureFieldInfo( HaxeProxyBase self, string name, ref ObjFieldInfoCache cache )
        {
            if (cache.field == null)
            {
                var t = self.HashlinkObj.Type;
                cache.field = t is HashlinkObjectType ot ? ot.FindField(name)! :
                    ((HashlinkVirtualType)t).FindField(name)!;
                if (cache.field is HashlinkObjectField)
                {
                    cache.offset = (nint)HashlinkNative.hl_obj_lookup((HL_vdynamic*)self.HashlinkPointer,
                        cache.field.HashedName, out _) - self.HashlinkPointer;
                }
            }
        }
        public static object? GetFieldById<T>( HaxeProxyBase self, string name, ref ObjFieldInfoCache cache )
            where T : class
        {
            EnsureFieldInfo(self, name, ref cache);
            if (cache.offset == 0)
            {
                return ((IHashlinkFieldObject) self.HashlinkObj).GetFieldValue(name);
            }
            return GetProxy<T>(HashlinkMarshal.ReadData((void*)(self.HashlinkPointer + cache.offset),
                cache.field!.FieldType));
        }
        public static T GetValueFieldById<T>( HaxeProxyBase self, string name, ref ObjFieldInfoCache cache )
            where T : unmanaged
        {
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
                value, cache.field!.FieldType);
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
            if (cache != null)
            {
                return cache;
            }

            return cache = (HaxeProxyBase?)GetProxy<HaxeProxyBase>(
                HashlinkMarshal.Module.Globals[globalIndex].Value
                );
        }
        [return: NotNullIfNotNull(nameof(val))]
        public static object? GetProxy<T>( object? val )
        {
            if (val == null)
            {
                return null;
            }
            if (val is T)
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
            if (val is IExtendData ied)
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
            return HashlinkMarshal.Module.Types[typeIndex].CreateInstance();
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
