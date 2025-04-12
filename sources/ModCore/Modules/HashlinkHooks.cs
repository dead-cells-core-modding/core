using Hashlink;
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
        [Obsolete]
        private HookHandle CreateHook( string typeName, string protoName, Delegate hook, nint entry )
        {
            return CreateHook(typeName, protoName, hook);
        }
        public HookHandle CreateHook( string typeName, string protoName, Delegate hook, bool enableByDefault = true)
        {
            return CreateHook(HashlinkMarshal.FindFunction(typeName, protoName), hook, enableByDefault);
        }
        public HookHandle CreateHook(HashlinkFunction func, Delegate hook, bool enableByDefault = true )
        {
            ArgumentNullException.ThrowIfNull(func);
            ArgumentNullException.ThrowIfNull(hook);
            nint entry = func.EntryPointer;
            if(!managers.TryGetValue(entry, out var manager))
            {
                manager = new(entry, func);
                managers.Add(entry, manager);
            }
            var h = new HookHandle(hook, manager);
            hooks.Add(h);
            if (enableByDefault)
            {
                h.Enable();
            }
            return h;
        }
    }
}
