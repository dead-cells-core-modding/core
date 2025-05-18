using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Reflection.Members;
using Hashlink.Reflection.Types;
using HaxeProxy.Events;
using ModCore.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    public unsafe class HashlinkHooks : CoreModule<HashlinkHooks>,
        IOnAddHashlinkHook,
        IOnRemoveHashlinkHook
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
        private HashlinkHookManager GetManager(HashlinkFunction func )
        {
            if (!managers.TryGetValue(func.EntryPointer, out var manager))
            {
                manager = new(func.EntryPointer, func);
                managers.Add(func.EntryPointer, manager);
            }
            return manager;
        }
        public HookHandle CreateHook( string typeName, string protoName, Delegate hook, bool enableByDefault = true)
        {
            return CreateHook(HashlinkMarshal.FindFunction(typeName, protoName), hook, enableByDefault);
        }
        public HookHandle CreateHook(HashlinkFunction func, Delegate hook, bool enableByDefault = true )
        {
            ArgumentNullException.ThrowIfNull(func);
            ArgumentNullException.ThrowIfNull(hook);
            var manager = GetManager(func);
            var h = new HookHandle(hook, manager);
            hooks.Add(h);
            if (enableByDefault)
            {
                h.Enable();
            }
            return h;
        }

        void IOnAddHashlinkHook.OnAddHashlinkHook( IOnAddHashlinkHook.Data data )
        {
            GetManager(data.Function).AddHook(data.Target);
        }

        void IOnRemoveHashlinkHook.OnRemoveHashlinkHook( IOnRemoveHashlinkHook.Data data )
        {
            GetManager(data.Function).RemoveHook(data.Target);
        }
    }
}
