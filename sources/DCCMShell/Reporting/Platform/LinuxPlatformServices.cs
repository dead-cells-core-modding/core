using SteamLauncher;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace DCCMShell.Reporting.Platform
{
    /// <summary>
    /// Linux platform service implementation——currently a stub; features to be implemented in a future version
    /// </summary>
    [SupportedOSPlatform("linux")]
    internal class LinuxPlatformServices : PlatformServices
    {
        public override string Name => "linux-x64";

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
            return new ProcessStartInfo(deadCellsExePath)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = false,
            };
        }

        /// <inheritdoc />
        public override string GetSystemSpec()
        {
            var sb = new StringBuilder();
            sb.Append(base.GetSystemSpec());

            sb.AppendLine("Graphics:");
            var gpuFound = false;

            // Try reading from /sys/class/drm/ for GPU info
            try
            {
                var drmDir = "/sys/class/drm";
                if (Directory.Exists(drmDir))
                {
                    foreach (var entry in Directory.GetDirectories(drmDir, "card*"))
                    {
                        var deviceDir = Path.Combine(entry, "device");
                        if (!Directory.Exists(deviceDir)) continue;

                        var vendorPath = Path.Combine(deviceDir, "vendor_name");
                        var devicePath = Path.Combine(deviceDir, "product_name");

                        var vendor = File.Exists(vendorPath)
                            ? File.ReadAllText(vendorPath).Trim()
                            : "Unknown";
                        var product = File.Exists(devicePath)
                            ? File.ReadAllText(devicePath).Trim()
                            : "Unknown";

                        sb.Append("  GPU: ");
                        sb.Append(vendor);
                        sb.Append(' ');
                        sb.AppendLine(product);
                        gpuFound = true;
                    }
                }
            }
            catch
            {
                // Reading from /sys failed, fall through
            }

            // Fallback: try lspci
            if (!gpuFound)
            {
                try
                {
                    using var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "lspci",
                            Arguments = "-mm",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                        }
                    };
                    proc.Start();
                    var output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(3000);

                    foreach (var line in output.Split('\n'))
                    {
                        if (line.Contains("VGA", StringComparison.OrdinalIgnoreCase) ||
                            line.Contains("3D", StringComparison.OrdinalIgnoreCase) ||
                            line.Contains("Display", StringComparison.OrdinalIgnoreCase))
                        {
                            // Format: "00:02.0 "VGA compatible controller" "Intel Corp" "Device 1234" ..."
                            var parts = line.Split('"');
                            if (parts.Length >= 6)
                            {
                                sb.Append("  GPU: ");
                                sb.Append(parts[3].Trim());
                                sb.Append(" ");
                                sb.AppendLine(parts[5].Trim());
                                gpuFound = true;
                            }
                        }
                    }
                }
                catch
                {
                    // lspci not available
                }
            }

            if (!gpuFound)
            {
                sb.AppendLine("  GPU: Unavailable");
            }

            sb.AppendLine();
            return sb.ToString();
        }

        /// <inheritdoc />
        protected override string GetCpuModel()
        {
            try
            {
                if (File.Exists("/proc/cpuinfo"))
                {
                    foreach (var line in File.ReadLines("/proc/cpuinfo"))
                    {
                        if (line.StartsWith("model name", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split(':', 2);
                            if (parts.Length == 2)
                                return parts[1].Trim();
                        }
                    }
                }
            }
            catch { }
            return base.GetCpuModel();
        }

        /// <inheritdoc />
        protected override string GetTotalPhysicalMemory()
        {
            try
            {
                if (File.Exists("/proc/meminfo"))
                {
                    foreach (var line in File.ReadLines("/proc/meminfo"))
                    {
                        if (line.StartsWith("MemTotal", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split(':', 2);
                            if (parts.Length == 2)
                            {
                                var memStr = parts[1].Trim();
                                // /proc/meminfo reports in kB; convert to MB
                                if (memStr.EndsWith("kB", StringComparison.OrdinalIgnoreCase))
                                {
                                    var kbStr = memStr[..^2].Trim();
                                    if (long.TryParse(kbStr, out var kb))
                                    {
                                        return $"{kb / 1024} MB";
                                    }
                                }
                                return memStr;
                            }
                        }
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        public override void RedirectStderr( nint target )
        {
            throw new NotImplementedException();
        }

        public override int GetExitCode( int pid )
        {
            throw new NotImplementedException();
        }
    }
}
