using Hashlink;
using Hashlink.Brigde;
using Hashlink.Marshaling;
using Hashlink.Reflection.Members;
using Hashlink.Reflection.Types;
using ModCore.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    [CoreModule]
    public unsafe class HashlinkHooks : CoreModule<HashlinkHooks>
    {
        private readonly Dictionary<nint, HashlinkHookManager> managers = [];
        private readonly List<HookHandle> hooks = [];

        public class HookHandle
        {
            internal HookHandle( Delegate hook, HashlinkHookManager manager )
            {
                Hook = hook;
                Manager = manager;
            }
            internal HashlinkHookManager Manager
            {
                get;
            }
            public Delegate Hook
            {
                get;
            }
            public void Enable()
            {
                Manager.AddHook(Hook);
            }
            public void Disable()
            {
                Manager.RemoveHook( Hook );
            }
        }
        public HookHandle CreateHook( string typeName, string protoName, Delegate hook, nint entry = 0 )
        {
            var type = (HashlinkObjectType) HashlinkMarshal.Module.GetTypeByName(typeName);
            var proto = type.FindProto(protoName) ?? throw new InvalidOperationException();
            var func = proto.Function;
            return CreateHook(func, hook, entry);
        }
        public HookHandle CreateHook(HashlinkFunction func, Delegate hook, nint entry = 0)
        {
            ArgumentNullException.ThrowIfNull(func);
            ArgumentNullException.ThrowIfNull(hook);
            if (entry == 0)
            {
                entry = (nint) func.EntryPointer;
            }
            if(!managers.TryGetValue(entry, out var manager))
            {
                manager = new(entry, func);
                managers.Add(entry, manager);
            }
            var h = new HookHandle(hook, manager);
            hooks.Add(h);
            return h;
        }
    }
}
