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
    /// <summary>
    /// A type for managing Hashlink Hooks
    /// </summary>
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    public unsafe class HashlinkHooks : CoreModule<HashlinkHooks>,
        IOnAddHashlinkHook,
        IOnRemoveHashlinkHook
    {
        private readonly Dictionary<nint, HashlinkHookManager> managers = [];
        private readonly List<HookHandle> hooks = [];

        /// <summary>
        /// A Hashlink hook
        /// </summary>
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
            /// <summary>
            /// 
            /// </summary>
            public Delegate Hook
            {
                get;
            }
            /// <summary>
            /// Enable hook
            /// </summary>
            public void Enable()
            {
                Manager.AddHook(Hook);
            }
            /// <summary>
            /// Disable Hook
            /// </summary>
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
        /// <summary>
        /// Create a Hashlink Hook
        /// </summary>
        /// <param name="typeName">The name of the type</param>
        /// <param name="protoName">The name of the target method</param>
        /// <param name="hook">Hook</param>
        /// <param name="enableByDefault">A value indicating whether the hook should be automatically enabled</param>
        /// <returns></returns>
        public HookHandle CreateHook( string typeName, string protoName, Delegate hook, bool enableByDefault = true)
        {
            return CreateHook(HashlinkMarshal.FindFunction(typeName, protoName), hook, enableByDefault);
        }
        /// <summary>
        /// Create a Hashlink Hook
        /// </summary>
        /// <param name="func">The target method</param>
        /// <param name="hook">Hook</param>
        /// <param name="enableByDefault">A value indicating whether the hook should be automatically enabled</param>
        /// <returns></returns>
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
