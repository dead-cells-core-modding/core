
using Hashlink.Proxy;
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Modules;
using ModCore.Modules.Internals;
using ModCore.Storage;
using Serilog;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ModCore
{
    internal static class Core
    {
        private static bool init = false;
        public static readonly List<string> dllSearchPath = [
            Path.GetDirectoryName(typeof(Core).Assembly.Location),
            FolderInfo.Plugins.FullPath,
            FolderInfo.Mods.FullPath,
            ];
        public static readonly List<string> nativeSearchPath = [
            FolderInfo.CoreNativeRoot.FullPath,
             FolderInfo.GameRoot.FullPath[..^1]
            ];
        public static Config<CoreConfig> Config { get; } = new("modcore");
        public static string Version { get; } = typeof(Core).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
                "Unknown";
        public static Thread MainThread { get; } = Thread.CurrentThread;
        public static void ThrowIfNotMainThread()
        {
            if (Thread.CurrentThread != MainThread)
            {
                throw new InvalidOperationException();
            }
        }

        public static bool InMainThread => Thread.CurrentThread == MainThread;

        private static void AddPath()
        {
            var split = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ";" : ":";
            var envName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Path" : "LD_LIBRARY_PATH";
            var val = Environment.GetEnvironmentVariable(envName);
            val = string.Join(split, nativeSearchPath) + split + val;
            Environment.SetEnvironmentVariable(envName, val);
        }

        private static readonly ConcurrentDictionary<string, Assembly?> name2typeLookup = [];

        public static void LoadCoreModules(
            Assembly asm,
            CoreModuleAttribute.CoreModuleKind kind)
        {
            var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                   ? CoreModuleAttribute.SupportOSKind.Windows
                   : CoreModuleAttribute.SupportOSKind.Linux;
            foreach (var type in asm.SafeGetAllTypes())
            {
                if (type == null)
                {
                    continue;
                }
                if (!type.IsSubclassOf(typeof(Module)) || type.IsAbstract)
                {
                    continue;
                }
                var attr = type.GetCustomAttribute<CoreModuleAttribute>();
                if (attr == null)
                {
                    continue;
                }
                if (attr.Kind != kind)
                {
                    continue;
                }
                if ((attr.SupportOS & os) != os)
                {
                    continue;
                }
                Log.Logger.Information("Loading core module: {type}", type.FullName);
                Activator.CreateInstance(type);
            }
        }

        private static void LoadLinuxLibs()
        {
            foreach (var v in Directory.GetFiles(FolderInfo.GameRoot.FullPath, "*", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(v);
                if (name.StartsWith("lib") &&
                    name.Contains(".so") &&
                    //!name.StartsWith("libsteam") &&
                    !name.StartsWith("libhl")
                    )
                {
                    NativeModuleResolver.LoadLibrary(v, false, name =>
                    {
                        NativeModuleResolver.LoadLibrary(
                            FolderInfo.GameRoot.GetFilePath(name), false
                        );
                    });
                }
            }
        }


        internal static void Initialize()
        {
            if (init)
            {
                return;
            }
            init = true;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            NativeLibrary.SetDllImportResolver(typeof(Core).Assembly, NativeResolver);
            NativeLibrary.SetDllImportResolver(typeof(EventSystem).Assembly, NativeResolver);
            NativeLibrary.SetDllImportResolver(typeof(HashlinkObj).Assembly, NativeResolver);

            Environment.SetEnvironmentVariable("DCCM_CoreLoaded", "true");

            AddPath();

            nint hlPtr;
            hlPtr = NativeModuleResolver.LoadLibrary(FolderInfo.CoreNativeRoot.GetFilePath("libhl.so"));
            _ = NativeModuleResolver.LoadLibrary(FolderInfo.CoreNativeRoot.GetFilePath("modcorenative.so"));
            _ = NativeLibrary.GetExport(hlPtr, "hl_modcore_native_was_here");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                LoadLinuxLibs();
            }
            


            Log.Logger.Information("Runtime: {FrameworkDescription} {RuntimeIdentifier}",
                   RuntimeInformation.FrameworkDescription, RuntimeInformation.RuntimeIdentifier);
            Log.Logger.Information("Core Version: {version}",
                Version);

            Log.Logger.Information("Initalizing");

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            Log.Logger.Information("Loading core modules");

            LoadCoreModules(typeof(Core).Assembly, CoreModuleAttribute.CoreModuleKind.Preload);

            EventSystem.BroadcastEvent<IOnCoreModuleInitializing>();

            Log.Logger.Information("Loaded modding core");
        }
        private static Assembly? CurrentDomain_TypeResolve( object? sender, ResolveEventArgs args )
        {
            var parts = args.Name.Split(',');
            var type = parts[0];
            return name2typeLookup.GetOrAdd(type, name =>
            {
                foreach (var v in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (v.GetType(name) != null)
                    {
                        return v;
                    }
                }
                return null;
            });
            
        }

        private static nint NativeResolver( string libraryName, Assembly assembly, DllImportSearchPath? searchPath )
        {
            if (libraryName == "libhl" ||
                libraryName == "hl")
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return NativeLibrary.Load(FolderInfo.CoreNativeRoot.GetFilePath("libhl.so"));
                }
                else
                {
                    return NativeLibrary.Load("libhl");
                }
            }
            if (NativeLibrary.TryLoad(
                FolderInfo.CoreNativeRoot.GetFilePath(
                    libraryName + ".so"
                ), out var result))
            {
                return result;
            }
            return 0;
        }
        private static Assembly? CurrentDomain_AssemblyResolve( object? sender, ResolveEventArgs args )
        {
            var asmName = new AssemblyName(args.Name);
            foreach (var p in dllSearchPath)
            {
                var fileName = Path.Combine(
                    p,
                    asmName.Name + ".dll");
                if (File.Exists(fileName))
                {
                    try
                    {
                        return Assembly.LoadFrom(fileName);
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }
            return null;
        }

        private static void CurrentDomain_ProcessExit( object? sender, EventArgs e )
        {
            EventSystem.BroadcastEvent<IOnSaveConfig>();
        }
    }
}
