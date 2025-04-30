
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Modules;
using ModCore.Storage;
using Serilog;
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
             FolderInfo.GameRoot.FullPath
            ];
        public static Config<CoreConfig> Config { get; } = new("modcore");

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
            var envName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Path" : "LD_LIBRARY_PATH";
            var val = Environment.GetEnvironmentVariable(envName);
            val = string.Join(';', nativeSearchPath) + ";" + val;
            Environment.SetEnvironmentVariable(envName, val);
        }

        public static void LoadCoreModules(Assembly asm)
        {
            var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                   ? CoreModuleAttribute.SupportOS.Windows
                   : CoreModuleAttribute.SupportOS.Linux;
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
                if ((attr.supportOS & os) != os)
                {
                    continue;
                }
                Log.Logger.Information("Loading core module: {type}", type.FullName);
                Activator.CreateInstance(type);
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

            Environment.SetEnvironmentVariable("DCCM_CoreLoaded", "true");

            AddPath();

            _ = NativeLibrary.Load(FolderInfo.CoreNativeRoot.GetFilePath("libhl"));
            _ = NativeLibrary.Load(FolderInfo.CoreNativeRoot.GetFilePath("modcorenative"));

            Log.Logger.Information("Runtime: {FrameworkDescription} {RuntimeIdentifier}",
                   RuntimeInformation.FrameworkDescription, RuntimeInformation.RuntimeIdentifier);
            Log.Logger.Information("Core Version: {version}",
                typeof(Core).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
                "Unknown");

            Log.Logger.Information("Initalizing");

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            Log.Logger.Information("Loading core modules");

            LoadCoreModules(typeof(Core).Assembly);

            EventSystem.BroadcastEvent<IOnCoreModuleInitializing>();

            Log.Logger.Information("Loaded modding core");
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
                    return Assembly.LoadFrom(fileName);
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
