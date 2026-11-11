# Release Notes - 35.15.1

## Feature

- Core: Added optional legacy game audio effects with a settings menu toggle and Chinese translation, enabled by default and applied on restart.
- Runtime: Migrated the framework and sample mods to .NET 11 and refreshed MonoMod, SDL3 and CsWin32 dependencies.

## Fix

- Launcher: Improved startup failure diagnostics with error codes and a .NET 11 download page when the runtime is missing, removed duplicate runtime checks, and clarified that error reports should attach the full log file.
- HashlinkSharp: Hardened thread state handling, object handle validation and marshaling defaults for safer interaction with the Hashlink VM.
- HaxeProxy: Made default reference targets thread-local to prevent state leakage across threads.
- ModCore: Stored hook closure state in GC-managed memory and tightened GC root and stack validation to reduce crashes.
- Native: Hardened GC stack marking and Windows thread stack capture against invalid pointers and exited threads, and added dedicated read-pointer validation.
- Build: Fixed nethost package path resolution in the native build configuration.

---

**Code signing policy**: Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

---

# 更新说明

## Feature

- Core：新增可选的旧版游戏音频效果，提供设置菜单开关与中文翻译，默认启用并在重启游戏后生效。
- Runtime：框架与示例 Mod 迁移至 .NET 11，并更新 MonoMod、SDL3 与 CsWin32 等依赖。

## Fix

- 启动器：改进启动失败诊断，在缺少运行时时显示错误码并打开 .NET 11 下载页面，移除重复的运行时检查，并提示错误报告中应附带完整日志文件。
- HashlinkSharp：加强线程状态处理、对象句柄校验与序列化默认值，提升与 Hashlink 虚拟机交互的安全性。
- HaxeProxy：将默认引用目标改为线程本地存储，避免跨线程状态泄漏。
- ModCore：将 Hook 闭包状态存放在 GC 托管内存中，并收紧 GC 根与栈校验，减少崩溃。
- Native：加强 GC 栈标记与 Windows 线程栈捕获对无效指针和已退出线程的处理，并新增专用的读指针校验接口。
- Build：修复原生构建中 nethost 包路径解析问题。

---

**代码签名政策**: 免费代码签名由 [SignPath.io](https://about.signpath.io) 提供，证书由 [SignPath Foundation](https://signpath.org) 颁发。
