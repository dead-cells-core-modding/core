using Hashlink;
using ModCore.Hashlink;
using ModCore.Hashlink.Transitions;
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
        public override int Priority => ModulePriorities.HashlinkHook;

        private object? Hook_logClientInfos2(HashlinkFunc orig)
        {
            Logger.Information("AAAAAAAAAAAAAAAAAA");
            return orig.Call();
        }

        public void OnBeforeGameStartup()
        {
            Logger.Information("Initializing");

            Logger.Information("Hooking Hashlink");

            var f = HashlinkUtils.FindFunction(
                    HashlinkUtils.FindTypeFromName("$Boot"), "logClientInfos"
                    );
            //var plogClientInfos = HashlinkUtils.GetFunctionNativePtr(
            //    f
            //    );
            //orig_logClientInfos = nhook.CreateHook<mt_logClientInfosHandler>((nint)plogClientInfos, Hook_mt_logClientInfos);

            var inst = HookTransition.CreateHook(f);
            inst.AddChain(Hook_logClientInfos2);
        }


    }
}
