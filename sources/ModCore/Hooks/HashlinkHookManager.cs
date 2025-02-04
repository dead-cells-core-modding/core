using Hashlink;
using Hashlink.Brigde;
using Hashlink.Reflection.Members;
using Hashlink.Reflection.Types;
using ModCore.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Hooks
{
    internal unsafe class HashlinkHookManager
    {
        public readonly NativeHooks.HookHandle hook;
        public readonly MethodWrapper wrapper;
        public readonly HashlinkFuncType function;

        private readonly List<Delegate> hooks = [];
        public HashlinkHookManager(nint target, HashlinkFunction func)
        {
            function = func.FuncType.BaseFunc;

            wrapper = new(HookEntry, function.ReturnType,
                function.ArgTypes);
            hook = NativeHooks.Instance.CreateHook(target, wrapper.EntryPointer, true);
            wrapper.RedirectTarget = hook.Original;
 
        }

        public void AddHook( Delegate hook )
        {
            ArgumentNullException.ThrowIfNull(hook);
            if (!hooks.Contains(hook))
            {
                hooks.Add(hook);
            }
            wrapper.RedirectTarget = 0;
        }
        public void RemoveHook( Delegate hook )
        {
            ArgumentNullException.ThrowIfNull(hook);
            hooks.Remove(hook);
            if (hooks.Count == 0)
            {
                wrapper.RedirectTarget = this.hook.Original;
            }
        }

        private object? HookEntry(MethodWrapper wrapper, object?[] args)
        {
            var func = new HashlinkFunc([..hooks], 1, function.TypeData, (void*)hook.Original);
            return hooks[0].DynamicInvoke([func, .. args]);
        }
    }
}
