using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    
    using SteamStartShell;

namespace SteamStartShell.Platform
{
    /// <summary>
    /// Windows 平台服务实现
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal class WindowsPlatformServices : PlatformServices
    {
        public override string Name => "win-x64";

        /// <summary>
        /// 从嵌入资源提取 steam_api64.dll 到临时目录并加载
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
        /// 通过 ntdll.dll 检查 wine_get_version 导出函数来检测 Wine/Proton 兼容层
        /// </summary>
        /// <returns>Wine 版本字符串，如果未检测到则返回 null</returns>
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
                // 无法检测 Wine，忽略错误
            }

            return null;
        }

        /// <summary>
        /// 配置 Windows 下的游戏进程启动参数
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
