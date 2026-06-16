using HashlinkNET.Bytecode;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Events.Interfaces.VM;
using ModCore.Storage;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ModCore.Modules.Internals
{
    [CoreModule(CoreModuleAttribute.CoreModuleKind.Preload)]
    internal unsafe class NativeModuleResolver : CoreModule<NativeModuleResolver>,
        IOnCoreModuleInitializing,
        IOnResolveNativeFunction,
        IOnResolveNativeLib,
        IOnCodeLoading
    {
        public override int Priority => ModulePriorities.NativeModuleResolver;

        private readonly Dictionary<string, Dictionary<string, nint>> knownNativeFunctions = [];

        // GOG's gog.hdll is often a 32-bit DLL that cannot be loaded into DCCM's
        // 64-bit process. We pre-build cdecl stubs for every gog native and fall
        // back to them when the real library fails to load.
        private readonly Dictionary<string, nint> gogStubs = [];
        private bool gogStubMode;

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

        void IOnCodeLoading.OnCodeLoading( ref ReadOnlySpan<byte> data )
        {
            var code = HlCode.FromBytes(data);
            var gogNatives = code.Natives.Where(n => n.Lib == "gog").ToList();
            if (gogNatives.Count == 0)
            {
                return;
            }

            Logger.Information("Pre-building {count} GOG native stubs", gogNatives.Count);

            var asmName = new AssemblyName("GOGNativeStubs");
            var asm = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            var module = asm.DefineDynamicModule(asmName.Name!);
            var type = module.DefineType("GOGNativeStubs", TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed);

            var builders = new List<(HlNative native, MethodBuilder method)>();
            foreach (var native in gogNatives)
            {
                try
                {
                    var method = BuildStub(type, native.Name, native.Type.Value);
                    builders.Add((native, method));
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Failed to build GOG stub for {name}", native.Name);
                }
            }

            var baked = type.CreateType();
            foreach (var (native, method) in builders)
            {
                var mi = baked.GetMethod(method.Name, BindingFlags.Public | BindingFlags.Static)!;
                gogStubs[native.Name] = mi.MethodHandle.GetFunctionPointer();
            }
        }

        private static MethodBuilder BuildStub( TypeBuilder type, string name, HlType hlType )
        {
            var fun = (hlType as HlTypeWithFun)?.FunctionDescription
                      ?? throw new InvalidOperationException($"Native {name} does not have a function type");

            var returnType = ToUnmanagedType(fun.ReturnType.Value);
            var parameterTypes = fun.Arguments.Select(a => ToUnmanagedType(a.Value)).ToArray();

            var method = type.DefineMethod(
                name,
                MethodAttributes.Public | MethodAttributes.Static,
                CallingConventions.Standard,
                returnType,
                parameterTypes);

            method.SetCustomAttribute(new CustomAttributeBuilder(
                typeof(UnmanagedCallersOnlyAttribute).GetConstructor(Type.EmptyTypes)!,
                [],
                [typeof(UnmanagedCallersOnlyAttribute).GetProperty(nameof(UnmanagedCallersOnlyAttribute.CallConvs))!],
                [new[] { typeof(CallConvCdecl) }]));

            var il = method.GetILGenerator();

            if (returnType == typeof(void))
            {
                il.Emit(OpCodes.Ret);
            }
            else if (returnType == typeof(float))
            {
                il.Emit(OpCodes.Ldc_R4, 0f);
                il.Emit(OpCodes.Ret);
            }
            else if (returnType == typeof(double))
            {
                il.Emit(OpCodes.Ldc_R8, 0d);
                il.Emit(OpCodes.Ret);
            }
            else if (returnType == typeof(long) || returnType == typeof(ulong))
            {
                il.Emit(OpCodes.Ldc_I8, 0L);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                // byte, ushort, short, int, uint, bool, and all pointer-sized types
                il.Emit(OpCodes.Ldc_I4_0);
                if (returnType == typeof(nint) || returnType == typeof(nuint))
                {
                    il.Emit(OpCodes.Conv_I);
                }
                il.Emit(OpCodes.Ret);
            }

            return method;
        }

        private static Type ToUnmanagedType( HlType type )
        {
            return type.Kind switch
            {
                HlTypeKind.Void => typeof(void),
                HlTypeKind.UI8 => typeof(byte),
                HlTypeKind.UI16 => typeof(ushort),
                HlTypeKind.I32 => typeof(int),
                HlTypeKind.I64 => typeof(long),
                HlTypeKind.F32 => typeof(float),
                HlTypeKind.F64 => typeof(double),
                HlTypeKind.Bool => typeof(bool),
                HlTypeKind.Type => typeof(nint),
                HlTypeKind.Bytes or HlTypeKind.Dyn or HlTypeKind.Fun or HlTypeKind.Obj
                    or HlTypeKind.Array or HlTypeKind.Ref or HlTypeKind.Virtual
                    or HlTypeKind.DynObj or HlTypeKind.Abstract or HlTypeKind.Null
                    or HlTypeKind.Method or HlTypeKind.Struct or HlTypeKind.Packed
                    or HlTypeKind.Enum => typeof(nint),
                _ => typeof(nint)
            };
        }

        EventResult<nint> IOnResolveNativeFunction.OnResolveNativeFunction( IOnResolveNativeFunction.NativeFunctionInfo info )
        {
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
            if (info.libname == "sdl")
            {
                if (info.name == "set_relative_mouse_mode")
                {
                    if (!Core.Config.Value.AllowLockCursor)
                    {
                        return (nint)ptr_NativeReturnFalse;
                    }
                }
            }
            if (gogStubMode && info.libname == "gog" && gogStubs.TryGetValue(info.name, out var stubPtr))
            {
                return stubPtr;
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

        [SupportedOSPlatform("windows")]
        private void TryLoadSDLWindows()
        {
            NativeLibrary.Load(FolderInfo.CurrentNativeRoot.GetFilePath("SDL3.dll"));
            NativeLibrary.Load(FolderInfo.CurrentNativeRoot.GetFilePath("SDL2.dll"));
        }

        private void TryLoadSteam()
        {
            GameInfo.Platform = GameInfo.PlatformKind.Steam;

            if (Core.Config.Value.EnableGoldberg)
            {
                Logger.Information("Goldberg Enabled");
                var path = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    FolderInfo.CurrentNativeRoot.GetFilePath("goldberg/steam_api64.dll") :
                    FolderInfo.CurrentNativeRoot.GetFilePath("goldberg/libsteam_api.so");
                Logger.Information("Try loading Goldberg from {path}", path);
                if (NativeLibrary.TryLoad(path, out _))
                {
                    return;
                }
                Logger.Information("Unable to load Goldberg");
            }


        }

        EventResult<nint> IOnResolveNativeLib.OnResolveNativeLib( string name )
        {
            if (name == "std" || name == "builtin")
            {
                return default;
            }

            if (name == "steam")
            {
                TryLoadSteam();
            }

            if (name == "gog")
            {
                GameInfo.Platform = GameInfo.PlatformKind.GOG;
            }

            if (name == "directx")
            {
                Logger.Fatal("DirectX is not supported on this platform");
                return default;
            }

            if (name == "sdl" && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                TryLoadSDLWindows();
            }

            // Some platform hdlls (e.g. GOG's gog.hdll) ship in the game root rather than
            // under coremod/native. Try the current native root first, then fall back.
            var searchPaths = new[] { FolderInfo.CurrentNativeRoot.FullPath, FolderInfo.GameRoot.FullPath };
            foreach (var dir in searchPaths)
            {
                var path = Path.Combine(dir, name + ".hdll");
                if (File.Exists(path))
                {
                    if (name == "gog")
                    {
                        // GOG's wrapper is frequently 32-bit while DCCM is 64-bit.
                        // If it fails to load, fall through to the pre-built stubs.
                        try
                        {
                            Logger.Information("Loading native module from {path}", path);
                            return NativeLibrary.Load(path);
                        }
                        catch (BadImageFormatException)
                        {
                            Logger.Warning("GOG native library {path} cannot be loaded (wrong architecture). Using stubs.", path);
                            gogStubMode = true;
                            return (nint)1;
                        }
                    }

                    Logger.Information("Loading native module from {path}", path);
                    return NativeLibrary.Load(path);
                }
            }

            if (name == "gog" && gogStubs.Count > 0)
            {
                // Real DLL not present anywhere, but we have bytecode natives for it.
                Logger.Warning("GOG native library not found. Using stubs.");
                gogStubMode = true;
                return (nint)1;
            }

            return default;
        }
    }
}
