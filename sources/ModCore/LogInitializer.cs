using ModCore.Storage;
using Serilog;
using System.Runtime.CompilerServices;

namespace ModCore
{
    internal static class LogInitializer
    {
        internal static void InitializeLog()
        {
            var configuration = new LoggerConfiguration()
              .MinimumLevel.Debug()
              .WriteTo.File(
                Path.Combine(FolderInfo.Logs.FullPath, "log_latest.log"),
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}"
                )
              .WriteTo.File(
                  Path.Combine(FolderInfo.Logs.FullPath, "log_.log"),
                  outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}",
                  rollingInterval: RollingInterval.Minute
              );
            if (!Core.Config.Value.NoConsole)
            {
                configuration.WriteTo.Console(Serilog.Events.LogEventLevel.Verbose,
                  outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}");
            }
            Log.Logger = configuration.CreateLogger();
        }
    }
}
