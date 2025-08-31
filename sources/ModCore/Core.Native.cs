using ModCore.Storage;
using MonoMod.Core;
using NonPublicNativeMembers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ModCore
{
    internal unsafe partial class Core
    {
        private static ICoreNativeDetour? detourGetProcAddress;

        private readonly static NativeMembersManager nativeMembers = NativeMembersManager.Create();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]

        private delegate nint GetProcAddressDel( nint module, byte* name );
        private static GetProcAddressDel? hook_GetProcAddress;
        private static GetProcAddressDel? orig_GetProcAddress;

        [UnmanagedCallersOnly]
        private static nint New_GetProcAddress( nint module, byte* name )
        {
            orig_GetProcAddress ??= Marshal.GetDelegateForFunctionPointer<GetProcAddressDel>(
                detourGetProcAddress!.OrigEntrypoint);
            var result = orig_GetProcAddress(module, name);
            
            //if (result == 0)
            {
                if (module == phLibhl)
                {
                    //Find in non public
                    Console.WriteLine($"Loading sym from: {Marshal.PtrToStringAnsi((nint)name)}");
                    var info = nativeMembers.Resolve(Marshal.PtrToStringAnsi((nint)name)!);
                    if (info == null)
                    {
                        return 0;
                    }
                    
                    return module + (nint)info.RVA;
                }
            }
            return result;
        }

        private static nint phLibhl;
        internal static void InitializeNative()
        {
            AddPath();

            phLibhl = NativeLibrary.Load(FolderInfo.CurrentNativeRoot.GetFilePath("libhl"));
            //_ = NativeLibrary.Load(FolderInfo.CurrentNativeRoot.GetFilePath("modcorenative"));


            foreach (var v in Directory.EnumerateFiles(FolderInfo.NativeRoot.FullPath, "*.json"))
            {
                var fn = Path.GetFileName(v);
                if (fn.StartsWith("nativemembers", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Information("Loading native member list from {path}", v);
                    nativeMembers.LoadFromFile(v);
                }
            }

            //TODO
            /**
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                nativeMembers.LoadAndActivateModule("libhl.dll");

                var k32 = NativeLibrary.Load("kernel32.dll");
                var getprocaddress = NativeLibrary.GetExport(k32, "GetProcAddress");

                detourGetProcAddress = DetourFactory.Current.CreateNativeDetour(new(getprocaddress,
                    (nint)(delegate* unmanaged<nint, byte*, nint>)&New_GetProcAddress)
                {
                    ApplyByDefault = false
                });
                detourGetProcAddress.Apply();
                
            }**/
        }
    }
}
