using System.Diagnostics;
using System.Runtime.Versioning;

namespace SteamStartShell.Platform
{
    /// <summary>
    /// Linux 平台服务实现——当前留空，功能在后续版本中实现
    /// </summary>
    [SupportedOSPlatform("linux")]
    internal class LinuxPlatformServices : PlatformServices
    {
        /// <summary>
        /// Linux 平台暂不支持原生 Steam 库加载
        /// </summary>
        public override void CheckNativeLib()
        {
            
        }

        /// <summary>
        /// 检测 Wine/Proton 兼容层——Linux 原生运行，无需检测
        /// </summary>
        public override string? DetectCompatibilityLayer()
        {
            return null;
        }

        /// <summary>
        /// 配置 Linux 下的游戏进程启动参数
        /// </summary>
        public override ProcessStartInfo? ConfigureGameProcess(string deadCellsExePath)
        {
            return new ProcessStartInfo(deadCellsExePath)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = false,
            };
        }
    }
}
