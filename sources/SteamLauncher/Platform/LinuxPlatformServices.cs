using SteamLauncher;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace SteamLauncher.Platform
{
    /// <summary>
    /// Linux platform service implementation——currently a stub; features to be implemented in a future version
    /// </summary>
    [SupportedOSPlatform("linux")]
    internal class LinuxPlatformServices : PlatformServices
    {
        public override string Name => "linux-x64";

        /// <summary>
        /// Linux platform does not yet support native Steam library loading
        /// </summary>
        public override void CheckNativeLib()
        {
            var steamapiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libsteam_api.so");

            using var rs = typeof(Program).Assembly.GetManifestResourceStream("libsteam_api.so")!;
            var template = new byte[rs.Length];
            rs.ReadExactly(template);

            if (File.Exists(steamapiPath))
            {
                var sapi = File.ReadAllBytes(steamapiPath);
                if (sapi.SequenceEqual(template))
                {
                    NativeLibrary.Load(steamapiPath);
                    return;
                }
            }

            Logger.Information("Extracting libsteam_api.so");
            try
            {
                File.WriteAllBytes(steamapiPath, template);
            }
            catch (IOException)
            {
                Logger.Warning("Unable to extract libsteam_api.so");
            }

            NativeLibrary.Load(steamapiPath);
        }

        /// <summary>
        /// Detect Wine/Proton compatibility layer——runs natively on Linux, no detection needed
        /// </summary>
        public override string? DetectCompatibilityLayer()
        {
            return null;
        }

        /// <summary>
        /// Configure game process launch parameters on Linux
        /// </summary>
        public override ProcessStartInfo? ConfigureGameProcess( string deadCellsExePath )
        {
            return new ProcessStartInfo(deadCellsExePath);
        }

    }
}
