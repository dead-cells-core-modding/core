using ModCore.Storage;
using Serilog;
using System.Runtime.CompilerServices;

namespace ModCore
{
    internal static class LogInitializer
    {
        private const string OUTPUT_FORMAT_TEMPLATE = "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}";
        internal static void InitializeLog()
        {
            var latest = Path.Combine(FolderInfo.Logs.FullPath, "log_latest.log");
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


            if (!Core.Config.Value.NoConsole)
            {
                configuration.WriteTo.Console(Serilog.Events.LogEventLevel.Verbose,
                  outputTemplate: OUTPUT_FORMAT_TEMPLATE);
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
