using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;
using static Windows.Win32.PInvoke;


namespace DeadCellsModding
{
    internal unsafe class Program
    {
        [DllImport("KERNEL32.dll", ExactSpelling = true, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [SupportedOSPlatform("windows6.1")]
        internal static extern unsafe HANDLE CreateRemoteThreadEx(HANDLE hProcess, 
            [Optional] SECURITY_ATTRIBUTES* lpThreadAttributes, 
            nuint dwStackSize, void* lpStartAddress, 
            [Optional] void* lpParameter, uint dwCreationFlags, LPPROC_THREAD_ATTRIBUTE_LIST lpAttributeList, 
            [Optional] uint* lpThreadId);


        static unsafe void InjectGame(HANDLE hProc, string dllRoot, params string[] dllpaths)
        {
            
            
            nint pk32dll = NativeLibrary.Load("kernel32.dll");
            nint pLoadLibrary = NativeLibrary.GetExport(pk32dll, "LoadLibraryW");
            NativeLibrary.Free(pk32dll);

            foreach (var v in dllpaths)
            {
                var dp = Path.GetFullPath(Path.Combine(dllRoot, v));

                void* prDllpath = VirtualAllocEx(hProc, null, (nuint)dp.Length * 3,
                Windows.Win32.System.Memory.VIRTUAL_ALLOCATION_TYPE.MEM_COMMIT,
                Windows.Win32.System.Memory.PAGE_PROTECTION_FLAGS.PAGE_READWRITE);
                fixed (char* pDllpath = dp)
                {
                    _ = WriteProcessMemory(hProc, prDllpath, pDllpath, (uint)dp.Length * 2);
                }
                uint threadId = 0;
                HANDLE hThread = CreateRemoteThreadEx(hProc, null, 0, (void*)pLoadLibrary, prDllpath, 0, default, &threadId);

                if(WaitForSingleObject(hThread, 5000) == WAIT_EVENT.WAIT_TIMEOUT)
                {
                    Console.Error.WriteLine("Failed to inject deadcells.exe");
                    TerminateProcess(hProc, uint.MaxValue);
                    Environment.Exit(-1);
                }

                uint exitCode = 0;
                if(GetExitCodeThread(hThread, &exitCode) && exitCode == 0)
                {
                    Console.Error.WriteLine("Failed to inject deadcells.exe");
                    TerminateProcess(hProc, uint.MaxValue);
                    Environment.Exit(-1);
                }
            }
        }
        static void Main(string[] args)
        {
            string dc_exe = Environment.GetEnvironmentVariable("DEAD_CELLS_GAME_PATH") ?? "deadcells.exe";
            string loader_dll_dir = Environment.GetEnvironmentVariable("MODCORE_NATIVE_PATH") ?? "";

            dc_exe = Path.GetFullPath(dc_exe);

            Console.WriteLine("Starting deadcells.exe");

            STARTUPINFOW startupInfo = new()
            {
                cb = (uint) sizeof(STARTUPINFOW),
                
            };

            PROCESS_INFORMATION procInfo = default;

            fixed (char* pdc_exe = dc_exe)
            {
                fixed (char* ppwd = Path.GetDirectoryName(dc_exe))
                {
                    if (!CreateProcess(new PCWSTR(pdc_exe), new PWSTR(), 
                        null, 
                        null, false,
                        PROCESS_CREATION_FLAGS.CREATE_SUSPENDED |
                        PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT,
                        null,
                        new PCWSTR(ppwd),
                        &startupInfo,
                        &procInfo
                        ))
                    {
                        Console.Error.WriteLine("Unable to start deadcells.exe!");
                        Environment.Exit(-1);
                    }
                }
            }
            Console.Error.WriteLine("Injecting deadcells.exe");
            try
            {
                InjectGame(procInfo.hProcess,
                    loader_dll_dir,
                    "modcorenative.dll");
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine("Failed to inject deadcells.exe");
                Console.Error.WriteLine(ex.ToString());
                TerminateProcess(procInfo.hProcess, uint.MaxValue);
                Environment.Exit(-1);
            }
            Console.Error.WriteLine("Resume deadcells.exe");
            _ = ResumeThread(procInfo.hThread);
            WaitForSingleObject(procInfo.hProcess, uint.MaxValue);
        }
    }
}
