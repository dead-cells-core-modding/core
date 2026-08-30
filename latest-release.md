# Release Notes - 35.13.4

## Feature

- ModCore: add pak loading events, broadcasting IOnLoadedPak and IOnUpdatedPakRecord to mods when paks are loaded or records are updated
- ModCore: auto-refresh FX atlases when pak records are updated, with a config option to disable
- ModCore: remove the mod limit on Steam by loading mod paks directly
- ModCore: remove the obsolete PlatformService module in favor of GameInfo
- DCCMShell: add a platform setup phase to suppress Windows error dialogs
- SteamLauncher: validate workshop content against a file list and retry automatically when a corrupted install is detected

## Fix

- DCCMShell: report unhandled exceptions, force-showing the error report when the process terminates
- HashlinkSharp: validate boolean values in generated wrapper and callback code to prevent invalid bool marshaling

---

**Code signing policy**: Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

---

# 更新说明

## Feature

- ModCore：新增 pak 加载事件，pak 加载或记录更新时向 mod 广播 IOnLoadedPak 与 IOnUpdatedPakRecord
- ModCore：pak 记录更新时自动刷新 FX 图集，并提供配置项可关闭
- ModCore：移除 Steam 平台 mod 数量上限，直接加载 mod pak
- ModCore：移除已废弃的 PlatformService 模块，改用 GameInfo
- DCCMShell：新增平台设置阶段，抑制 Windows 错误对话框
- SteamLauncher：按文件清单校验 Workshop 内容，检测到损坏安装时自动重试

## Fix

- DCCMShell：未处理异常导致进程终止时强制显示错误报告
- HashlinkSharp：生成的包装器与回调代码校验布尔值，修复无效布尔值封送

---

**代码签名政策**: 免费代码签名由 [SignPath.io](https://about.signpath.io) 提供，证书由 [SignPath Foundation](https://signpath.org) 颁发。
