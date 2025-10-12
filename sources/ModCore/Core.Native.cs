using ModCore.Native;
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

using NN = ModCore.Native.Native;

namespace ModCore
{
    internal unsafe partial class Core
    {
        private static ICoreNativeDetour? detourGetProcAddress;
        
        private readonly static NativeMembersManager nativeMembers = NativeMembersManager.Create();

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]

        private delegate nint GetProcAddressDel( nint module, byte* name, nint unknown );
        private static GetProcAddressDel? orig_GetProcAddress;

        [UnmanagedCallersOnly]
        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static nint New_GetProcAddress( nint module, byte* name, nint unknown )
        {

            NN.Current.Data->orig_GetProcAddress = detourGetProcAddress!.OrigEntrypoint;

            orig_GetProcAddress ??= Marshal.GetDelegateForFunctionPointer<GetProcAddressDel>(
                detourGetProcAddress!.OrigEntrypoint);
            
            var result = orig_GetProcAddress(module, name, unknown);

            if ((nint)name > 0xffff)
            {
                
                if (module == phLibhl)
                {
                    var nameStr = Marshal.PtrToStringAnsi((nint)name)!;
                    //Find in non public

                    var info = nativeMembers.Resolve(nameStr);
                    Debug.Assert(info != null);

                    if (!NativeLibrary.TryLoad(info.ModuleName, out var baseAddr))
                    {
                        if (!NativeLibrary.TryLoad(info.ModuleName + ".dll", out baseAddr))
                        {
                            baseAddr = NativeLibrary.Load(info.ModuleName + ".exe");
                        }
                    }
                   
                    return baseAddr + (nint)info.RVA;
                }
            }
            return result;
        }

        private static nint phLibhl;
        internal static void InitializeNative()
        {
            //AddPath();

            
            
            _ = NativeLibrary.Load(FolderInfo.CurrentNativeRoot.GetFilePath("modcorenative"));


            foreach (var v in Directory.EnumerateFiles(FolderInfo.NativeRoot.FullPath, "*.json"))
            {
                var fn = Path.GetFileName(v);
                if (fn.StartsWith("nativemembers", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Information("Loading native member list from {path}", v);
                    nativeMembers.LoadFromFile(v);
                }
            }

            //Load hashlink libraries

            foreach (var v in ContextConfig.Config.hashlinkLibraries)
            {
                if (!NativeLibrary.TryLoad(v, out _))
                {
                    NativeLibrary.Load(FolderInfo.CurrentNativeRoot.GetFilePath(v));
                }
                nativeMembers.LoadAndActivateModule(v);
            }

            phLibhl = NativeLibrary.Load("libhl");

            //TODO
            //**
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var k32 = NativeLibrary.Load("kernelbase.dll");
                var getprocaddress = NativeLibrary.GetExport(k32, "GetProcAddressForCaller");

                detourGetProcAddress = DetourFactory.Current.CreateNativeDetour(new(getprocaddress,
                   NN.Current.asm_hook_GetProcAddress_Entry)
                {
                    ApplyByDefault = false
                });
                NN.Current.Data->new_GetProcAddress = (nint)(delegate* unmanaged< nint, byte*, nint, nint >)&New_GetProcAddress;
                NN.Current.Data->orig_GetProcAddress = NN.Current.Data->new_GetProcAddress;
                NN.Current.Data->phLibhl = phLibhl;
                detourGetProcAddress.Apply();
                
            }//**/

            _  = NativeLibrary.GetExport(phLibhl, "break_on_trap");

        }
    }
}
