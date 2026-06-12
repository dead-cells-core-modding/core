using ModCore.Storage;
using Serilog;

namespace ModCore
{
    internal static class LogInitializer
    {
        private const string OUTPUT_FORMAT_TEMPLATE = "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}";
        private const int MAX_LOGS_COUNT = 31;
        internal static void InitializeLog()
        {
            var latest = Path.Combine(FolderInfo.Logs.FullPath, "log_latest.log");

            //Console.OutputEncoding = System.Text.Encoding.UTF8;


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
                catch
                {

                }
            }

            {
                var logs = Directory.GetFiles(FolderInfo.Logs.FullPath, "log_*.log")
                    .Where(x => Path.GetFileNameWithoutExtension(x) != "log_latest")
                    .ToList();
                logs.Sort();
                if (logs.Count > MAX_LOGS_COUNT)
                {
                    //Remove the oldest
                    foreach (var v in logs.Take(logs.Count - MAX_LOGS_COUNT))
                    {
                        try
                        {
                            File.Delete(v);
                        }
                        catch
                        {

                        }
                    }
                }
            }

            var date = DateTime.Now;

            var configuration = new LoggerConfiguration()
              .MinimumLevel.Debug()
              .WriteTo.File(
                latest,
                outputTemplate: OUTPUT_FORMAT_TEMPLATE,
                buffered: false
                )
              .WriteTo.File(
                  Path.Combine(FolderInfo.Logs.FullPath, $"log_{date:yyyy-MM-dd-HH-mm-ss}.log"),
                  outputTemplate: OUTPUT_FORMAT_TEMPLATE,
                  rollingInterval: RollingInterval.Infinite,

                  buffered: false
              );


            if (ContextConfig.Config.consoleOutput)
            {
                configuration.WriteTo.Console(Serilog.Events.LogEventLevel.Verbose,
                  outputTemplate: OUTPUT_FORMAT_TEMPLATE, applyThemeToRedirectedOutput: true);

                if (Console.IsErrorRedirected)
                {
                    configuration.WriteTo.Console(Serilog.Events.LogEventLevel.Warning,
                        outputTemplate: OUTPUT_FORMAT_TEMPLATE, standardErrorFromLevel: Serilog.Events.LogEventLevel.Warning,
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
