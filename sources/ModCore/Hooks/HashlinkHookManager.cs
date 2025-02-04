using Hashlink;
using Hashlink.Brigde;
using Hashlink.Reflection.Members;
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
        public readonly HashlinkFunction function;
        public HashlinkHookManager(nint target, HashlinkFunction func)
        {
            function = func;
            var f = func.FuncType.TypeData;
            wrapper = new(HookEntry, f->ret->kind,
                new Span<HL_type>(f->args, f->nargs)
                .ToArray().Select(x => x.kind));
            hook = NativeHooks.Instance.CreateHook(target, wrapper.EntryPointer, false);
            //wrapper.RedirectTarget = hook.Original;
            hook.Enable();
        }

        private object? HookEntry(MethodWrapper wrapper, object?[] args)
        {
            var func = new HashlinkFunc(function.FuncType.TypeData, (void*)hook.Original);
            return func.CallDynamic(args);
        }
    }
}
