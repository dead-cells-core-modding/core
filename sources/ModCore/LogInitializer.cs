using ModCore.Storage;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ModCore
{
    internal static class LogInitializer
    {
        [ModuleInitializer]
        internal static void InitializeLog()
        {
            Log.Logger = new LoggerConfiguration()
              .MinimumLevel.Debug()
              .WriteTo.Console(Serilog.Events.LogEventLevel.Verbose,
                  outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}")
              .WriteTo.File(
                  Path.Combine(FolderInfo.Logs.FullPath, "log_.log"),
                  outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}",
                  rollingInterval: RollingInterval.Minute
              )
              .CreateLogger();
        }
    }
}
