using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32;

namespace SteamStartShell.Platform
{
    /// <summary>
    /// Windows platform service implementation
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal partial class WindowsPlatformServices : PlatformServices
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

        /// <inheritdoc />
        public override string GetSystemSpec()
        {
            var wineVer = DetectCompatibilityLayer();
            var sb = new StringBuilder();

            // OS section
            sb.AppendLine("Operating System:");
            sb.Append("  Description: ");
            sb.AppendLine(RuntimeInformation.OSDescription);
            sb.Append("  OS Architecture: ");
            sb.AppendLine(RuntimeInformation.OSArchitecture.ToString());

            // Under Wine, append real host system info
            if (!string.IsNullOrEmpty(wineVer))
            {
                var hostOs = ReadLinuxDistroName();
                var (sysname, release) = GetWineHostVersion();
                sb.Append("  Wine: ");
                sb.AppendLine(wineVer);
                if (!string.IsNullOrEmpty(hostOs) || !string.IsNullOrEmpty(sysname))
                {
                    sb.Append("  Host System: ");
                    if (!string.IsNullOrEmpty(hostOs))
                    {
                        sb.Append(hostOs);
                        if (!string.IsNullOrEmpty(release))
                        {
                            sb.Append(" (");
                            sb.Append(sysname ?? "Linux");
                            sb.Append(' ');
                            sb.Append(release);
                            sb.Append(')');
                        }
                    }
                    else if (!string.IsNullOrEmpty(sysname))
                    {
                        sb.Append(sysname);
                        if (!string.IsNullOrEmpty(release))
                        {
                            sb.Append(' ');
                            sb.Append(release);
                        }
                    }
                }
            }
            sb.AppendLine();

            // CPU section
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

            // Memory section
            sb.AppendLine("Memory:");
            var totalRam = GetTotalPhysicalMemory();
            if (!string.IsNullOrEmpty(totalRam))
            {
                sb.Append("  Total Physical: ");
                sb.AppendLine(totalRam);
            }
            sb.AppendLine();

            // GPU section
            AppendGpuFromRegistry(sb);

            return sb.ToString();
        }

        /// <summary>
        /// Read GPU info from the Windows registry and append to StringBuilder
        /// </summary>
        private static void AppendGpuFromRegistry(StringBuilder sb)
        {
            sb.AppendLine("Graphics:");
            try
            {
                const string displayClassGuid = "{4d36e968-e325-11ce-bfc1-08002be10318}";
                using var baseKey = Registry.LocalMachine.OpenSubKey(
                    $@"SYSTEM\CurrentControlSet\Control\Class\{displayClassGuid}");

                if (baseKey != null)
                {
                    var subKeyNames = baseKey.GetSubKeyNames();
                    var gpuCount = 0;

                    foreach (var subKeyName in subKeyNames)
                    {
                        if (!int.TryParse(subKeyName, out _)) continue;

                        using var gpuKey = baseKey.OpenSubKey(subKeyName);
                        if (gpuKey == null) continue;

                        var driverDesc = gpuKey.GetValue("DriverDesc")?.ToString();
                        var driverVersion = gpuKey.GetValue("DriverVersion")?.ToString();
                        var hwInfo = gpuKey.GetValue("HardwareInformation") as byte[];

                        if (!string.IsNullOrEmpty(driverDesc))
                        {
                            gpuCount++;
                            sb.Append("  GPU: ");
                            sb.AppendLine(driverDesc);
                            if (!string.IsNullOrEmpty(driverVersion))
                            {
                                sb.Append("  Driver Version: ");
                                sb.AppendLine(driverVersion);
                            }
                            if (hwInfo != null && hwInfo.Length >= 8)
                            {
                                var vramBytes = BitConverter.ToInt64(hwInfo, 0);
                                if (vramBytes > 0)
                                {
                                    sb.Append("  VRAM: ");
                                    sb.Append(vramBytes / (1024 * 1024));
                                    sb.AppendLine(" MB");
                                }
                            }
                            sb.AppendLine();
                        }
                    }

                    if (gpuCount == 0)
                    {
                        sb.AppendLine("  GPU: None detected via registry");
                    }
                }
                else
                {
                    sb.AppendLine("  GPU: Unavailable (registry key not found)");
                    sb.AppendLine();
                }
            }
            catch
            {
                sb.AppendLine("  GPU: Unavailable (registry query failed)");
                sb.AppendLine();
            }
        }

        /// <summary>
        /// Read the host Linux distribution name from /etc/os-release (e.g. "Ubuntu 26.04")
        /// </summary>
        private static string ReadLinuxDistroName()
        {
            try
            {
                if (File.Exists("/etc/os-release"))
                {
                    foreach (var line in File.ReadLines("/etc/os-release"))
                    {
                        if (line.StartsWith("PRETTY_NAME=", StringComparison.OrdinalIgnoreCase))
                        {
                            return line.Split('=', 2)[1].Trim('"', '\'');
                        }
                    }
                }
            }
            catch { }
            return string.Empty;
        }

     

        /// <summary>
        /// Call wine_get_host_version to get the real host kernel info (sysname, release)
        /// </summary>
        private static (string? sysname, string? release) GetWineHostVersion()
        {
            try
            {
                var ntdll = NativeLibrary.Load("ntdll.dll");
                if (NativeLibrary.TryGetExport(ntdll, "wine_get_host_version", out var func))
                {
                    unsafe
                    {
                        byte* sysnamePtr = null;
                        byte* releasePtr = null;
                        ((delegate* unmanaged<byte**, byte**, void>)func)(&sysnamePtr, &releasePtr);
                        return (
                            Marshal.PtrToStringUTF8((nint)sysnamePtr),
                            Marshal.PtrToStringUTF8((nint)releasePtr)
                        );
                    }
                }
            }
            catch { }
            return (null, null);
        }


        /// <inheritdoc />
        protected override string GetCpuModel()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                if (key != null)
                {
                    var name = key.GetValue("ProcessorNameString")?.ToString();
                    if (!string.IsNullOrEmpty(name))
                        return name.Trim();
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
                var memStatus = new MEMORYSTATUSEX();
                memStatus.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
                if (GlobalMemoryStatusEx(ref memStatus))
                {
                    var totalMB = memStatus.ullTotalPhys / (1024 * 1024);
                    return $"{totalMB} MB";
                }
            }
            catch { }
            return string.Empty;
        }


        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [LibraryImport("kernel32.dll")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
    }
}
