using MinHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore
{
    public unsafe static class DeadCells
    {
        public static nint LibhlHandle { get; private set; }
        public static HookEngine NativeHookEngine { get; } = new();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]

        private delegate void* hl_dyn_call_safe_handler(void* c, void** args, int nargs, bool* isException);
        private static hl_dyn_call_safe_handler orig_hl_dyn_call_safe = null!;

        private static void* Hook_hl_dyn_call_safe(void* c, void** args, int nargs, bool* isException)
        {
            RealInitalize();
            return orig_hl_dyn_call_safe(c, args, nargs, isException);
        }

        private static void SetupInitalizeHook()
        {
            var ptr_hl_dyn_call_safe = NativeLibrary.GetExport(LibhlHandle, "hl_dyn_call_safe");

            orig_hl_dyn_call_safe = NativeHookEngine.CreateHook<hl_dyn_call_safe_handler>(ptr_hl_dyn_call_safe, 
                Hook_hl_dyn_call_safe);
            NativeHookEngine.EnableHook(orig_hl_dyn_call_safe);
        }

        private static void RealInitalize()
        {
            NativeHookEngine.DisableHook(orig_hl_dyn_call_safe);
            Console.WriteLine("Real Initalize");

        }

        internal static void Initalize()
        {
            LibhlHandle = NativeLibrary.Load("libhl.dll");

            SetupInitalizeHook();
        }
    }
}
