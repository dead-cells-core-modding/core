using Hashlink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore
{
    internal static unsafe partial class Native
    {
        private const string MODCORE_NATIVE_NAME = "modcorenative";

        #region Stack Trace
        [LibraryImport(MODCORE_NATIVE_NAME)]
        public static partial int mcn_load_stacktrace(void** buf, int maxCount, void* stackBottom);
        [LibraryImport(MODCORE_NATIVE_NAME)]
        public static partial void* mcn_get_ebp();
        [LibraryImport(MODCORE_NATIVE_NAME)]
        public static partial void* mcn_get_esp();
        [LibraryImport(MODCORE_NATIVE_NAME)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool module_resolve_pos(HL_module* m, void* addr, out int fidx, out int fpos);
        #endregion
        #region HL Utils
        [LibraryImport(MODCORE_NATIVE_NAME)]
        public static partial void* hl_code_read(void* data, int size, byte** errorMsg);
        [LibraryImport(MODCORE_NATIVE_NAME)]
        public static partial void* hlu_get_hl_bytecode_from_exe(
            [MarshalAs(UnmanagedType.LPWStr)] string path, int* outSize);
        [LibraryImport(MODCORE_NATIVE_NAME)]
        public static partial int hlu_start_game(void* code);
        [LibraryImport(MODCORE_NATIVE_NAME)]
        public static partial void* callback_c2hl(void* f, HL_type* t, void** args, HL_vdynamic* ret);
        #endregion
    }
}
