using ModCore.Storage;
using Serilog;

namespace ModCore
{
    internal static class LogInitializer
    {
        private const string OUTPUT_FORMAT_TEMPLATE = "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}";
        internal static void InitializeLog()
        {
            var latest = Path.Combine(FolderInfo.Logs.FullPath, "log_latest.log");

            if (Console.IsErrorRedirected)
            {
                Console.Error.WriteLine("\n[DCCMLOGLATEST]" + latest);
            }

            if (File.Exists(latest))
            {
                try
                {
                    File.Delete(latest);
                }
                catch (Exception)
                {
                
                }
            }
            var configuration = new LoggerConfiguration()
              .MinimumLevel.Debug()
              .WriteTo.File(
                latest,
                outputTemplate: OUTPUT_FORMAT_TEMPLATE
                )
              .WriteTo.File(
                  Path.Combine(FolderInfo.Logs.FullPath, "log_.log"),
                  outputTemplate: OUTPUT_FORMAT_TEMPLATE,
                  rollingInterval: RollingInterval.Minute
              );


            if (ContextConfig.Config.consoleOutput)
            {
                configuration.WriteTo.Console(Serilog.Events.LogEventLevel.Verbose,
                  outputTemplate: OUTPUT_FORMAT_TEMPLATE, applyThemeToRedirectedOutput: true);

                if (Console.IsErrorRedirected)
                {
                    configuration.WriteTo.Console(Serilog.Events.LogEventLevel.Error,
                        outputTemplate: OUTPUT_FORMAT_TEMPLATE, standardErrorFromLevel: Serilog.Events.LogEventLevel.Error,
                            applyThemeToRedirectedOutput: false);
                }
            }
            else
            {
                configuration.WriteTo.Trace(
                    outputTemplate: OUTPUT_FORMAT_TEMPLATE
                    );
            }
            Log.Logger = configuration.CreateLogger();
        }
    }
}
