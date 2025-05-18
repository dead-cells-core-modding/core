using Hashlink.Marshaling;
using Hashlink.Reflection.Members;
using HaxeProxy.Events;
using ModCore.Events;
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
        public static void AddHook( int fid, Delegate hook )
        {
            var f = (HashlinkFunction) HashlinkMarshal.Module.GetFunctionByFIndex(fid);
            EventSystem.BroadcastEvent<IOnAddHashlinkHook, IOnAddHashlinkHook.Data>(new(f, hook));
        }
        public static void RemoveHook( int fid, Delegate hook )
        {
            var f = (HashlinkFunction)HashlinkMarshal.Module.GetFunctionByFIndex(fid);
            EventSystem.BroadcastEvent<IOnRemoveHashlinkHook, IOnRemoveHashlinkHook.Data>(new(f, hook));
        }
    }
}
