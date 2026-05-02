
using Serilog;

namespace ModCore
{
    internal static class LogInitializer
    {
        private const string OUTPUT_FORMAT_TEMPLATE = "[{Timestamp:HH:mm:ss} {Level:u3}][SteamShell] {Message:lj}{NewLine}{Exception}";
        internal static void InitializeLog()
        {

            var latest = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath!)!, "log_latest.log");
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
              .WriteTo.Console(
                outputTemplate: OUTPUT_FORMAT_TEMPLATE
                );
            Log.Logger = configuration.CreateLogger();
        }
    }
}
