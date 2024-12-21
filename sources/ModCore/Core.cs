
using ModCore.Events;
using ModCore.Modules;
using ModCore.Modules.Events;
using ModCore.Storage;
using Serilog;
using Serilog.Core;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ModCore
{
    static class Core
    {
        private static bool init = false;
        public static Config<CoreConfig> Config { get; } = new("modcore");

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
            val = 
                FolderInfo.CoreNativeRoot.FullPath + ";" +
                FolderInfo.GameRoot.FullPath + ";" +
                val;
            Environment.SetEnvironmentVariable(envName, val);
        }

        internal static void Initialize()
        {
            if(init)
            {
                return;
            }
            init = true;

            Environment.SetEnvironmentVariable("DCCM_CoreLoaded", "true");

            AddPath();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(Serilog.Events.LogEventLevel.Verbose,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    Path.Combine(FolderInfo.Logs.FullPath, "log_.log"),
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Minute
                )
                .CreateLogger();

            Log.Logger.Information("Runtime: {FrameworkDescription} {RuntimeIdentifier}",
                   RuntimeInformation.FrameworkDescription, RuntimeInformation.RuntimeIdentifier);

            Log.Logger.Information("Initalizing");

            

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

            EventSystem.BroadcastEvent<IOnModCoreInjected>();

            Log.Logger.Information("Loaded modding core");
        }
        static int InjectMain(IntPtr args, int argsSize)
        {
            try
            {
                
                Initialize();
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "An excpetion was occured on Initalizing");
                Console.Error.WriteLine(ex.ToString());
                Utils.ExitGame();
            }
            return 0;
        }
    }
}
