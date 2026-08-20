# Release Notes - 35.13.3

## Feature

- ModLoader: add a per-mod data directory (ModDataRoot) so each mod gets a dedicated storage folder under the core data directory
- SteamLauncher: proactively download subscribed workshop items and wait for pending downloads before starting the game
- DCCMShell: show the error report window even when the process exits with code 0 if a fatal error marker is present

## Fix

- SteamLauncher: detect a broken Steam installation during API init and report a clear reinstall hint instead of failing silently
- SteamLauncher: reduce the waiting interval while the required mod is downloading to speed up startup
- DCCMShell: implement Linux stderr redirection via libc dup2 instead of throwing NotImplementedException
- ModLoader: log a warning when a declared mod dependency is missing
- Storage: automatically create folder directories when FolderInfo is constructed to prevent missing-directory crashes
- ModCore: emit a fatal error marker on unhandled .NET exceptions and fatal startup errors so the error report is always triggered

---

**Code signing policy**: Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

---

# 更新说明

## Feature

- ModLoader：为每个 Mod 新增独立的 ModDataRoot 数据目录，用于在核心数据目录下存放各模组的专属存储文件夹
- SteamLauncher：启动前主动下载已订阅的工坊物品，并等待待处理下载完成后才开始游戏
- DCCMShell：当错误信息包含致命错误标记时，即使进程以退出码 0 结束也会显示错误报告窗口

## Fix

- SteamLauncher：在 Steam API 初始化时检测损坏的 Steam 安装，并给出明确的重新安装提示，避免静默失败
- SteamLauncher：缩短所需 Mod 下载时的等待间隔，加快启动速度
- DCCMShell：通过 libc dup2 实现 Linux 平台 stderr 重定向，不再抛出 NotImplementedException
- ModLoader：当声明的 Mod 依赖缺失时输出警告日志
- Storage：FolderInfo 构造时自动创建对应文件夹，避免因目录不存在而崩溃
- ModCore：未处理的 .NET 异常及启动致命错误会输出致命错误标记，确保错误报告始终被触发

---

**代码签名政策**: 免费代码签名由 [SignPath.io](https://about.signpath.io) 提供，证书由 [SignPath Foundation](https://signpath.org) 颁发。
