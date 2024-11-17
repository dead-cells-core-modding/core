using MinHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    public unsafe class NativeHookModule : Module<NativeHookModule>
    {
        public override int Priority => ModulePriorities.NativeHookModule;

        [SupportedOSPlatform("windows")]
        private readonly HookEngine? hookEngine;

        public NativeHookModule()
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                hookEngine = new HookEngine();
            }
        }

        public TTarget CreateHook<TTarget>(nint nativeFunc, TTarget target) where TTarget : Delegate
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var result = hookEngine!.CreateHook(nativeFunc, target);
                hookEngine!.EnableHook(result);
                return result;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        public void DisableHook(Delegate original)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                hookEngine!.DisableHook(original);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        public void EnableHook(Delegate original)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                hookEngine!.EnableHook(original);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
