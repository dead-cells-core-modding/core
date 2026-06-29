using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Serilog;

namespace SteamStartShell.Platform
{
    /// <summary>
    /// 平台服务抽象基类——提供平台相关的原生库加载、兼容层检测和进程配置
    /// </summary>
    internal abstract class PlatformServices
    {
        protected static ILogger Logger => Log.Logger;

        public abstract string Name { get; }

        /// <summary>
        /// 运行时平台实例——根据当前操作系统自动选择
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
        /// 加载平台原生 Steam 库（Windows: steam_api64.dll, Linux: libsteam_api.so）
        /// </summary>
        public abstract void CheckNativeLib();

        /// <summary>
        /// 检测 Wine/Proton 兼容层版本
        /// </summary>
        /// <returns>Wine 版本字符串，如果未检测到则返回 null</returns>
        public virtual string? DetectCompatibilityLayer()
        {
            return null;
        }

        /// <summary>
        /// 配置游戏进程启动参数
        /// </summary>
        /// <param name="deadCellsExePath">DeadCells 可执行文件路径</param>
        /// <returns>平台特定的 ProcessStartInfo 配置，null 表示使用默认配置</returns>
        public virtual ProcessStartInfo? ConfigureGameProcess(string deadCellsExePath)
        {
            return null;
        }
    }
}
