using Hashlink;
using Hashlink.Reflection.Members;
using Hashlink.Reflection.Types;
using Hashlink.Wrapper;
using Hashlink.Wrapper.Callbacks;
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
        public readonly HlCallback router;
        public readonly HashlinkFuncType function;

        private readonly List<Delegate> hooks = [];
        public HashlinkHookManager(nint target, HashlinkFunction func)
        {
            function = func.FuncType.BaseFunc;
            router = HlCallbackFactory.GetHlCallback(
                HlFuncSign.Create(func.FuncType)
                );
            
            hook = NativeHooks.Instance.CreateHook(target, router.RouterPointer, true);
            router.RedirectTarget = hook.Original;
 
        }

        public void AddHook( Delegate hook )
        {
            ArgumentNullException.ThrowIfNull(hook);
            if (!hooks.Contains(hook))
            {
                hooks.Add(hook);
            }
            //router.RedirectTarget = 0;
        }
        public void RemoveHook( Delegate hook )
        {
            ArgumentNullException.ThrowIfNull(hook);
            hooks.Remove(hook);
            if (hooks.Count == 0)
            {
                router.RedirectTarget = this.hook.Original;
            }
        }

        private object? HookEntry(object?[] args)
        {
            throw new NotImplementedException();
            //var func = new HashlinkFunc([..hooks], 1, function.TypeData, (void*)hook.Original);
            //return hooks[0].DynamicInvoke([func, .. args]);
        }
    }
}
