using Serilog;

namespace SteamLauncher
{
    internal static class LogInitializer
    {
        private const string OUTPUT_FORMAT_TEMPLATE = "[{Timestamp:HH:mm:ss} {Level:u3}][Launcher] {Message:lj}{NewLine}{Exception}";
        public static readonly string LOG_PATH = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath!)!, "log_latest.log");
        internal static void InitializeLog()
        {
            if (File.Exists(LOG_PATH))
            {
                try
                {
                    File.Delete(LOG_PATH);
                }
                catch (Exception)
                {

                }
            }
            var configuration = new LoggerConfiguration()
              .MinimumLevel.Debug()
              .WriteTo.File(
                LOG_PATH,
                outputTemplate: OUTPUT_FORMAT_TEMPLATE,
                buffered: false,
                shared: true
                )
              .WriteTo.Console(
                outputTemplate: OUTPUT_FORMAT_TEMPLATE
                )
              ;
            Log.Logger = configuration.CreateLogger();
        }
    }
}
