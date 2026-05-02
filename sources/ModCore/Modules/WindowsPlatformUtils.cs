using ModCore.Events.Interfaces.VM;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ModCore.Modules
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload,
        CoreModuleAttribute.SupportOSKind.Windows)]
    [SupportedOSPlatform("windows")]
    internal partial class WindowsPlatformUtils : CoreModule<WindowsPlatformUtils>,
        IOnHashlinkVMReady,
        IOnCodeLoading
    {

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]

        private delegate void FreeConsole_handler();
        public override int Priority => ModulePriorities.PlatformUtils;

        private static void FreeConsole()
        {

        }

        public void OnHashlinkVMReady()
        {
            var kernel32 = NativeLibrary.Load("kernel32.dll");
            var freeconsole = NativeLibrary.GetExport(kernel32, "FreeConsole");

            if (!Core.Config.Value.AllowCloseConsole)
            {
                NativeHooks.Instance.CreateHook(freeconsole, (FreeConsole_handler)FreeConsole);
            }

            NativeLibrary.Free(kernel32);


        }


        void IOnCodeLoading.OnCodeLoading( ref ReadOnlySpan<byte> data )
        {

        }
    }
}
