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
        [LibraryImport(MODCORE_NATIVE_NAME)]
        [return: MarshalAs(UnmanagedType.I4)] 
        public static partial bool mcn_memory_readable(void* ptr);
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
        public static partial void* hlu_get_hl_bytecode_from_exe(
            [MarshalAs(UnmanagedType.LPWStr)] string path, int* outSize);
        [LibraryImport(MODCORE_NATIVE_NAME)]
        public static partial int hlu_start_game(void* code);
       
        #endregion
        #region HL CS Interop
        [LibraryImport(MODCORE_NATIVE_NAME)]
        public static partial void* get_asm_call_bridge_hl_to_cs();
        #endregion
    }
}
