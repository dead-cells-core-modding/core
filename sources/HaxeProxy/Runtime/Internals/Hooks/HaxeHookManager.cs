using Hashlink.Marshaling;
using Hashlink.Reflection.Members;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HaxeProxy.Runtime.Internals.Hooks
{
    internal static class HaxeHookManager
    {
        private static readonly ConcurrentDictionary<(int, Delegate), Hook> hooks = [];
        public static void AddHook( int fid, Delegate hook )
        {
            var f = (HashlinkFunction) HashlinkMarshal.Module.GetFunctionByFIndex(fid);
            throw new NotImplementedException();
        }
        public static void RemoveHook( int fid, Delegate hook )
        {
            var f = (HashlinkFunction)HashlinkMarshal.Module.GetFunctionByFIndex(fid);
            if (hooks.TryRemove((fid, hook), out var h))
            {
                h.Dispose();
            }
        }
    }
}
