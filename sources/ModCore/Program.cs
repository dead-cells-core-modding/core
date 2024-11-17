
using ModCore.Modules;
using ModCore.Modules.Events;
using Serilog;
using Serilog.Core;
using System.Runtime.InteropServices;

namespace ModCore
{
    class Program
    {

        static void Initalize()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(Serilog.Events.LogEventLevel.Information)
                .CreateLogger();

            Log.Logger.Information("Runtime: {FrameworkDescription} {RuntimeIdentifier}",
                   RuntimeInformation.FrameworkDescription, RuntimeInformation.RuntimeIdentifier);

            Log.Logger.Information("Initalizing");

            Module.AddModule(new NativeHookModule());
            Module.AddModule(new HashlinkModule());


            Module.BroadcastEvent<IOnModCoreInjected>();

            Log.Logger.Information("Loaded modding core");
        }
        static int InjectMain(IntPtr args, int argsSize)
        {
            try
            {
                Initalize();
                throw null;
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "An excpetion was occured on Initalizing");
                Environment.Exit(-1);
            }
            return 0;
        }
    }
}
