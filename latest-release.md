# Release Notes - 35.13.7

## Feature

- Added Mesa D3D12 rendering backend support: bundled the Mesa D3D12 OpenGL-over-D3D12 library for Windows, exposed a settings menu toggle, and automatically enabled it when OpenGL context creation fails, while listing available graphics devices to help diagnose rendering issues.
- Introduced a No Mods mode that can be activated through a command line flag or an environment variable, loading only essential core modules and skipping Steam native function overrides while it is enabled.
- Added an MDK plugin build target that builds plugins as libraries by default, simplifying plugin project configuration.
- Added a Chinese error report header shown to Chinese speaking users, so they can more easily understand crash reports and forward logs to support tools.

## Fix

- Fixed the TMX collapse and expand commands so they return the actual exit code of the underlying tool.
- Fixed integer index access on Hashlink field objects by routing it through getDyn and setDyn instead of field name lookup.
- Improved Steam handling by supporting Steam API version mismatch during initialization, shutting down Steamworks properly on exit, and relaunching the launcher with elevated privileges when file access errors occur.
- Emitted int3 breakpoints for unresolved native stubs so invalid stub calls fail fast instead of misbehaving silently.
- Added a validation assertion that pak entry names contain no path separators, catching malformed pak files early.

---

**Code signing policy**: Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

# 更新说明

## Feature

- 新增 Mesa D3D12 渲染后端支持：为 Windows 打包 Mesa D3D12（基于 D3D12 的 OpenGL）库，在设置菜单中新增开关，并在 OpenGL 上下文创建失败时自动启用，同时列出可用图形设备以便排查渲染问题。
- 新增无 Mod 模式，可通过命令行参数或环境变量启用，仅加载必要的核心模块，并在该模式下跳过 Steam 原生函数替换。
- 新增 MDK 插件构建目标，使插件默认以库类型构建，简化插件工程配置。
- 新增中文错误报告抬头，在中文环境下显示，方便中文用户理解崩溃报告并将日志转发给支持工具。

## Fix

- 修复 TMX 折叠与展开命令，使其返回底层工具的实际退出码。
- 修复 Hashlink 字段对象的整数索引访问，改为通过 getDyn 与 setDyn 处理，而非按字段名查找。
- 改进 Steam 处理：支持初始化时处理 Steam API 版本不匹配，在退出时正确关闭 Steamworks，并在出现文件访问错误时以提权方式重新启动启动器。
- 为未解析的原生桩生成 int3 断点，使非法的桩调用快速失败，避免出现静默的异常行为。
- 新增校验断言，确保 pak 条目名称不包含路径分隔符，及早发现格式错误的 pak 文件。

---

**代码签名政策**: 免费代码签名由 [SignPath.io](https://about.signpath.io) 提供，证书由 [SignPath Foundation](https://signpath.org) 颁发。
