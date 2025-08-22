﻿using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.VM;
using ModCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore.Modules.Internals
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    internal unsafe partial class NativeModuleResolver : CoreModule<NativeModuleResolver>,
        IOnCoreModuleInitializing,
        IOnResolveNativeFunction,
        IOnResolveNativeLib
    {

        [LibraryImport("modcorenative")]
        private static partial nint hlu_load_so( [MarshalAs(UnmanagedType.LPStr)] string path, int lazy, out nint errMsg );
        public override int Priority => ModulePriorities.NativeModuleResolver;

        private readonly Dictionary<string, Dictionary<string, nint>> knownNativeFunctions = [];

        private void RegisterType( string libname, Type type )
        {
            if (!knownNativeFunctions.TryGetValue(libname, out var dict))
            {
                dict = [];
                knownNativeFunctions.Add(libname, dict);
            }
            foreach (var v in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            {
                var attr = v.GetCustomAttribute<UnmanagedCallersOnlyAttribute>();
                if (attr == null)
                {
                    continue;
                }
                var name = string.IsNullOrEmpty(attr.EntryPoint) ? v.Name : attr.EntryPoint;
                dict.Add(name, v.MethodHandle.GetFunctionPointer());
            }

        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int NativeReturnFalse()
        {
            return 0;
        }
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int NativeReturnTrue()
        {
            return 1;
        }

        private static readonly delegate* unmanaged[Cdecl]< int > ptr_NativeReturnFalse = &NativeReturnFalse;
        private static readonly delegate* unmanaged[Cdecl]< int > ptr_NativeReturnTrue = &NativeReturnTrue;
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static void NativeNotImplemented()
        {
            new NotImplementedException().HashlinkThrow();
        }
        private static readonly delegate* unmanaged[Cdecl]< void > ptr_NativeNotImplemented = &NativeNotImplemented;



        void IOnCoreModuleInitializing.OnCoreModuleInitializing()
        {

        }

        public static nint LoadLibrary( string path, bool lazy = true, Action<string>? loadDep = null )
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var result = hlu_load_so(path, lazy ? 1 : 0, out var err);
                if (result == 0)
                {
                    var msg = Marshal.PtrToStringAnsi(err)!;
                    var soName = msg[..msg.IndexOf(':')];
                    if (loadDep != null)
                    {
                        loadDep(soName);
                        return LoadLibrary(path, lazy, null);
                    }
                    throw new DllNotFoundException(msg);
                }
                return result;
            }
            else
            {
                return NativeLibrary.Load(path);
            }
        }


        EventResult<nint> IOnResolveNativeFunction.OnResolveNativeFunction( IOnResolveNativeFunction.NativeFunctionInfo info )
        {
            if (info.libname == "chroma")
            {
                //Not Support
                return (nint)ptr_NativeReturnFalse;
            }
            if (info.libname == "steam")
            {
                if (info.name == "is_user_logged_in")
                {
                    return (nint)ptr_NativeReturnTrue;
                }
                else if (info.name == "get_achievement")
                {
                    return (nint)ptr_NativeReturnTrue;
                }
                else if (info.name == "set_achievement")
                {
                    return (nint)ptr_NativeReturnTrue;
                }
            }
            if (knownNativeFunctions.TryGetValue(info.libname, out var dict))
            {
                if (dict.TryGetValue(info.name, out var result))
                {
                    return result;
                }
            }
            return default;
        }

        private nint TryLoadSteam()
        {

            if (Core.Config.Value.EnableGoldberg)
            {
                Logger.Information("Goldberg Enabled");
                var path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    FolderInfo.CoreNativeRoot.GetFilePath("goldberg/steam_api64.dll") :
                    FolderInfo.CoreNativeRoot.GetFilePath("goldberg/libsteam_api.so");
                Logger.Information("Try loading Goldberg from {path}", path);
                try
                {
                    LoadLibrary(path);
                    return 0;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Unable to load Goldberg");
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var hdll = FolderInfo.CoreNativeRoot.GetFilePath("hlsteam/steam.hdll");
                if (File.Exists(hdll))
                {
                    return LoadLibrary(hdll);
                }
            }
            return 0;
        }

        EventResult<nint> IOnResolveNativeLib.OnResolveNativeLib( string name )
        {
            if (name == "std" || name == "builtin")
            {
                return default;
            }

            if (name == "steam")
            {
                var result = TryLoadSteam();
                if (result != 0)
                {
                    return result;
                }
            }

            var path = FolderInfo.CoreNativeRoot.GetFilePath(name + ".hdll");
            if (!File.Exists(path))
            {
                path = FolderInfo.GameRoot.GetFilePath(name + ".hdll");
                if (File.Exists(path))
                {
                    Logger.Information("Loading native module from {path}", path);
                    return LoadLibrary(path);
                }
                return default;
            }
            Logger.Information("Loading native module from {path}", path);
            return LoadLibrary(path);
        }
    }
}
