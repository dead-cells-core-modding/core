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
        public static partial int modcore_x86_load_stacktrace(void** buf, int maxCount, void* stackBottom);

        [LibraryImport(MODCORE_NATIVE_NAME)]
        public static partial void* hl_code_read(void* data, int size, byte** errorMsg);
        [LibraryImport(MODCORE_NATIVE_NAME)]
        public static partial void* hlu_get_hl_bytecode_from_exe(
            [MarshalAs(UnmanagedType.LPWStr)] string path, int* outSize);
        [LibraryImport(MODCORE_NATIVE_NAME)]
        public static partial int hlu_start_game(void* code);
    }
}
