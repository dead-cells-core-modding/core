
using ModCore.Events;
using ModCore.Modules;
using ModCore.Modules.Events;
using Serilog;
using Serilog.Core;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ModCore
{
    class Program
    {

        static void Initalize()
        {
            Directory.CreateDirectory(GameConstants.LogsRoot);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(Serilog.Events.LogEventLevel.Verbose,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    Path.Combine(GameConstants.LogsRoot, "log_.log"),
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Minute
                )
                .CreateLogger();

            Log.Logger.Information("Runtime: {FrameworkDescription} {RuntimeIdentifier}",
                   RuntimeInformation.FrameworkDescription, RuntimeInformation.RuntimeIdentifier);

            Log.Logger.Information("Initalizing");

            

            Log.Logger.Information("Loading core modules");

            foreach (var type in typeof(Program).Assembly.GetTypes())
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
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                    !attr.supportOS.HasFlag(CoreModuleAttribute.SupportOS.Windows))
                {
                    continue;
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                    !attr.supportOS.HasFlag(CoreModuleAttribute.SupportOS.Linux))
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
                Initalize();
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "An excpetion was occured on Initalizing");
                Utils.ExitGame();
            }
            return 0;
        }
    }
}
