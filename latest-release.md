# Release Notes - 35.12.3

## Feature

- Added SteamStartShell Linux support with embedded libsteam_api.so, hlboot.dat-based game detection, and .hl argument passthrough
- Added a one-time DCCM startup warning popup informing users that achievements and rankings are disabled when modding is active
- Replaced the Iced x86 assembler with a TCC-backed AsmAssembler for unified cross-platform assembly code generation
- Supported ELF symbol analysis from Windows build hosts via LLVM toolchain probing, enabling cross-compilation native member scanning

## Fix

- Fixed HashlinkSharp closure lifecycle (closures are now stateful by default) and HDYN type resolution during value marshaling, preventing premature garbage collection
- Fixed HUD mod icon (bmpMod) not displaying when a user profile is loaded
- Fixed native library module base address resolution to use platform-specific delegation instead of hardcoded logic

---

# 更新说明 - 35.12.3

## 新增功能

- 新增 SteamStartShell 的 Linux 支持：内嵌 libsteam_api.so、基于 hlboot.dat 的游戏检测以及 .hl 参数透传
- 新增 DCCM 启动一次性警告弹窗，提醒用户启用 Mod 后成就和排行榜功能已禁用
- 将 Iced x86 汇编器替换为基于 TCC 的 AsmAssembler，实现统一的跨平台汇编代码生成
- 支持在 Windows 构建主机上通过 LLVM 工具链探测进行 ELF 符号分析，实现交叉编译原生成员扫描

## 修复

- 修复 HashlinkSharp 闭包生命周期（闭包默认标记为有状态）和 HDYN 类型解析问题，防止闭包被过早垃圾回收
- 修复 HUD Mod 图标（bmpMod）在加载用户存档后不显示的问题
- 修复原生库模块基地址解析，改为平台特定的委托方式，替代硬编码逻辑
