
using ModCore.Events;
using ModCore.Events.Interfaces;
using ModCore.Modules;
using ModCore.Storage;
using Serilog;
using Serilog.Core;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace ModCore
{
    static class Core
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
            if(Thread.CurrentThread != MainThread)
            {
                throw new InvalidOperationException();
            }
        }

        public static bool InMainThread => Thread.CurrentThread == MainThread;

        static void AddPath()
        {
            string envName;
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                envName = "Path";
            }
            else
            {
                envName = "LD_LIBRARY_PATH";
            }
            var val = Environment.GetEnvironmentVariable(envName);
            val = string.Join(';', nativeSearchPath) + ";" + val;
            Environment.SetEnvironmentVariable(envName, val);
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
            

            Initialize2();
        }

        private static void Initialize2()
        {
           
            Log.Logger.Information("Runtime: {FrameworkDescription} {RuntimeIdentifier}",
                   RuntimeInformation.FrameworkDescription, RuntimeInformation.RuntimeIdentifier);

            Log.Logger.Information("Initalizing");

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            Log.Logger.Information("Loading core modules");

            CoreModuleAttribute.SupportOS os;
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                os = CoreModuleAttribute.SupportOS.Windows;
            }
            else
            {
                os = CoreModuleAttribute.SupportOS.Linux;
            }

            foreach (var type in typeof(Core).Assembly.GetTypes())
            {
                if (!type.IsSubclassOf(typeof(Module)) || type.IsAbstract)
                {
                    continue;
                }
                var attr = type.GetCustomAttribute<CoreModuleAttribute>();
                if (attr == null)
                {
                    continue;
                }
                if((attr.supportOS & os) != os)
                {
                    continue;
                }
                Log.Logger.Information("Loading core module: {type}", type.FullName);
                var module = (Module?)Activator.CreateInstance(type);
                if(module == null)
                {
                    continue;
                }
                Module.AddModule(module);
            }

            EventSystem.BroadcastEvent<IOnCoreModuleInitializing>();

            Log.Logger.Information("Loaded modding core");
        }

        private static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
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

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            EventSystem.BroadcastEvent<IOnSaveConfig>();
        }
    }
}
