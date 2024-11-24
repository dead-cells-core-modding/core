using ModCore.Events;
using ModCore.Modules.Events;
using MonoMod.RuntimeDetour;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    [CoreModule]
    public unsafe class HashlinkModule : CoreModule<HashlinkModule>, IOnModCoreInjected
    {
        public override int Priority => ModulePriorities.HashlinkModule;
        public nint LibhlHandle { get; private set; }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]

        private delegate void* hl_dyn_call_safe_handler(void* c, void** args, int nargs, bool* isException);
        private static hl_dyn_call_safe_handler orig_hl_dyn_call_safe = null!;

        private static void* Hook_hl_dyn_call_safe(void* c, void** args, int nargs, bool* isException)
        {
            Logger.Information("Game will be started");
            EventSystem.BroadcastEvent<IOnBeforeGameStartup>();
            return orig_hl_dyn_call_safe(c, args, nargs, isException);
        }
        void IOnModCoreInjected.OnModCoreInjected()
        {
            Logger.Information("Initalizing HashlinkModule");

            LibhlHandle = NativeLibrary.Load("libhl.dll");

            Logger.Information("Hooking functions");


            orig_hl_dyn_call_safe = NativeHookModule.Instance.CreateHook<hl_dyn_call_safe_handler>(
                NativeLibrary.GetExport(LibhlHandle, "hl_dyn_call_safe"), Hook_hl_dyn_call_safe);
        }
    }
}
