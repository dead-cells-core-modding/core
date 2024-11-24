using ModCore.Modules.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules
{
    [CoreModule(supportOS = CoreModuleAttribute.SupportOS.Windows)]
    internal class WindowsPlatformUtils : CoreModule<WindowsPlatformUtils>, IOnBeforeGameStartup
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]

        private delegate void FreeConsole_handler();
        public override int Priority => ModulePriorities.PlatformUtils;

        private static void FreeConsole()
        {

        }

        public void OnBeforeGameStartup()
        {
            var kernel32 = NativeLibrary.Load("kernel32.dll");
            var freeconsole = NativeLibrary.GetExport(kernel32, "FreeConsole");

            NativeHookModule.Instance.CreateHook(freeconsole, (FreeConsole_handler)FreeConsole);

            NativeLibrary.Free(kernel32);
        }
    }
}
