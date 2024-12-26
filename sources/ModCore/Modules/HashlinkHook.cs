using Hashlink;
using ModCore.Hashlink;
using ModCore.Hashlink.Hooks;
using ModCore.Modules.Events;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    [CoreModule]
    public unsafe class HashlinkHook : CoreModule<HashlinkHook> , IOnBeforeGameStartup
    {
        private static readonly ConcurrentDictionary<int, HashlinkHookInst> hooks = [];
        private static readonly ConcurrentDictionary<Delegate, HLHook> d2hooks = [];
        public class HLHook
        {
            private readonly HashlinkHookInst hook;
            public Delegate Detour { get; }
            public HL_function* Target => hook.Target;
            internal HLHook(HashlinkHookInst inst, Delegate detour)
            {
                hook = inst;
                Detour = detour;
            }

            public void Apply()
            {
                hook.AddChain(Detour);
            }
            public void Undo()
            {
                hook.RemoveChain(Detour);
            }
        }
        public override int Priority => ModulePriorities.HashlinkHook;


        public HLHook CreateHook(HL_function* func, Delegate detour, bool autoApply = true)
        {
            var result = d2hooks.GetOrAdd(detour, (detour, func) =>
            {
                var f = (HL_function*)func;
                var inst = hooks.GetOrAdd(f->findex, (_, func) => HookTransition.CreateHook((HL_function*) func),
                    func);
                return new(inst, detour);
            }, (nint)func);
            if(autoApply)
            {
                result.Apply();
            }
            return result;
        }

        public void OnBeforeGameStartup()
        {
            Logger.Information("Initializing");

            Logger.Information("Hooking Hashlink");

        }


    }
}
