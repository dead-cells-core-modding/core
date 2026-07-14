using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    
    using SteamStartShell;

namespace SteamStartShell.Platform
{
    /// <summary>
    /// Windows platform service implementation
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal class WindowsPlatformServices : PlatformServices
    {
        public override string Name => "win-x64";

        /// <summary>
        /// Extract steam_api64.dll from embedded resources to a temporary directory and load it
        /// </summary>
        public override void CheckNativeLib()
        {
            var steamapiPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steam_api64.dll");

            if (!File.Exists(steamapiPath))
            {
                Logger.Information("Extracting steam_api64.dll");
                using (var rs = typeof(Program).Assembly.GetManifestResourceStream("steam_api64.dll"))
                {
                    using var fs = File.OpenWrite(steamapiPath);
                    rs!.CopyTo(fs);
                }
            }

            NativeLibrary.Load(steamapiPath);
        }

        /// <summary>
        /// Detect the Wine/Proton compatibility layer by checking the wine_get_version exported function via ntdll.dll
        /// </summary>
        /// <returns>Wine version string, or null if not detected</returns>
        public override string? DetectCompatibilityLayer()
        {
            try
            {
                var ntdll = NativeLibrary.Load("ntdll.dll");
                if (NativeLibrary.TryGetExport(ntdll, "wine_get_version", out var wineVerFunc))
                {
                    unsafe
                    {
                        var ver = Marshal.PtrToStringUTF8(
                            (nint)((delegate* unmanaged< byte* >)wineVerFunc)()) ?? "Unknown";
                        return ver;
                    }
                }
            }
            catch
            {
                // Unable to detect Wine; ignore the error
            }

            return null;
        }

        /// <summary>
        /// Configure game process launch parameters on Windows
        /// </summary>
        public override ProcessStartInfo? ConfigureGameProcess( string deadCellsExePath )
        {
            return new ProcessStartInfo(deadCellsExePath)
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = false,
            };
        }
    }
}
