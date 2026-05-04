using Hashlink.Marshaling;
using Hashlink.Reflection.Members;
using HaxeProxy.Events;
using ModCore.Events;

namespace HaxeProxy.Runtime.Internals.Hooks
{
    internal static class HaxeHookManager
    {
        public static void AddHook( int fid, Delegate hook )
        {
            var f = (HashlinkFunction)HashlinkMarshal.Module.GetFunctionByFIndex(fid);
            EventSystem.BroadcastEvent<IOnAddHashlinkHook, IOnAddHashlinkHook.Data>(new(f, hook));
        }
        public static void RemoveHook( int fid, Delegate hook )
        {
            var f = (HashlinkFunction)HashlinkMarshal.Module.GetFunctionByFIndex(fid);
            EventSystem.BroadcastEvent<IOnRemoveHashlinkHook, IOnRemoveHashlinkHook.Data>(new(f, hook));
        }
    }
}
