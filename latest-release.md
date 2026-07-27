# Release Notes - 35.12.8

## Feature

- Shell: Extract shared launcher logic into a new SteamLauncher library, enabling code reuse between Steam and GOG shells
- GOG Shell: Add linux-x64 runtime support and application icon
- ModCore: Log game version, build date, and Git hash at startup for easier diagnostics
- FolderInfo: Implement deferred path resolution with dependency-aware rebuild for more robust folder path management

## Fix

- HashlinkNET Compiler: Add missing Castclass IL instruction when generating pseudo-code for field-by-ID setting
- Shell: Enable unbuffered shared file logging for launcher logs to ensure timely log output

---

# 更新说明 - 35.12.8

## Feature

- Shell: 将共享启动器逻辑提取到新的 SteamLauncher 库中，使 Steam 与 GOG 启动器可复用代码
- GOG Shell: 新增 linux-x64 运行时支持及应用图标
- ModCore: 在启动时记录游戏版本、构建日期和 Git 哈希，便于诊断问题
- FolderInfo: 实现延迟路径解析与依赖感知重建机制，提升文件夹路径管理的健壮性

## Fix

- HashlinkNET Compiler: 修复生成伪代码时按 ID 设置字段缺少 Castclass IL 指令的问题
- Shell: 启用启动器日志的非缓冲共享文件记录，确保日志及时输出

