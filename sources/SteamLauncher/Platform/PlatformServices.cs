using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Serilog;
using SteamLauncher.Platform;

namespace SteamLauncher.Platform
{
    /// <summary>
    /// Abstract base class for platform services——provides platform-specific native library loading, compatibility layer detection, and process configuration
    /// </summary>
    internal abstract class PlatformServices
    {
        protected static ILogger Logger => Log.Logger;

        public abstract string Name { get; }

        /// <summary>
        /// Runtime platform instance——automatically selected based on the current operating system
        /// </summary>
        public static PlatformServices Current { get; } = CreateService();

        private static PlatformServices CreateService()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsPlatformServices();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new LinuxPlatformServices();
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        /// <summary>
        /// Load the platform-native Steam library (Windows: steam_api64.dll, Linux: libsteam_api.so)
        /// </summary>
        public abstract void CheckNativeLib();

        /// <summary>
        /// Detect the Wine/Proton compatibility layer version
        /// </summary>
        /// <returns>Wine version string, or null if not detected</returns>
        public virtual string? DetectCompatibilityLayer()
        {
            return null;
        }

        /// <summary>
        /// Configure game process launch parameters
        /// </summary>
        /// <param name="deadCellsExePath">DeadCells executable file path</param>
        /// <returns>Platform-specific ProcessStartInfo configuration; null indicates using default configuration</returns>
        public virtual ProcessStartInfo? ConfigureGameProcess(string deadCellsExePath)
        {
            return null;
        }
    }
}
