using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Serilog;


namespace DCCMShell.Reporting.Platform
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

        /// <summary>
        /// Collect system specifications for error reporting
        /// </summary>
        /// <returns>Multi-line string with OS, CPU, memory, and platform-specific info</returns>
        public virtual string GetSystemSpec()
        {
            var sb = new StringBuilder();

            sb.AppendLine("Operating System:");
            sb.Append("  Description: ");
            sb.AppendLine(RuntimeInformation.OSDescription);
            sb.Append("  Architecture: ");
            sb.AppendLine(RuntimeInformation.OSArchitecture.ToString());
            sb.AppendLine();

            sb.AppendLine("CPU:");
            var cpuModel = GetCpuModel();
            if (!string.IsNullOrEmpty(cpuModel))
            {
                sb.Append("  Model: ");
                sb.AppendLine(cpuModel);
            }
            sb.Append("  Logical Processors: ");
            sb.AppendLine(Environment.ProcessorCount.ToString());
            sb.AppendLine();

            sb.AppendLine("Memory:");
            var totalRam = GetTotalPhysicalMemory();
            if (!string.IsNullOrEmpty(totalRam))
            {
                sb.Append("  Total Physical: ");
                sb.AppendLine(totalRam);
            }
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Get the CPU model name (e.g. "Intel Core i7-9700K", "AMD Ryzen 5 5600X")
        /// </summary>
        protected virtual string GetCpuModel()
        {
            // Platform-agnostic fallback: try PROCESSOR_IDENTIFIER env var (Windows)
            var envCpu = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
            if (!string.IsNullOrEmpty(envCpu))
                return envCpu;
            return string.Empty;
        }

        /// <summary>
        /// Get total physical memory as a human-readable string (e.g. "16384 MB")
        /// </summary>
        protected virtual string GetTotalPhysicalMemory()
        {
            return string.Empty;
        }

        public abstract void RedirectStderr( nint target );
        public abstract int GetExitCode( int pid );
    }
}
