# Release Notes - 35.13.1

## Feature

- Crash reports now include an AI analysis guide that helps AI assistants diagnose common crash causes, such as memory issues, antivirus interference and version mismatches, and directs users to the appropriate support channels

## Fix

- Adapted the framework to the rewritten Hashlink JIT used by the updated game VM, including reworked JIT hooks, updated VM structure definitions, reserved code patch space and adjusted function pointer handling
- Fixed res.pak failing to load by redirecting it to the game root directory and setting the working directory correctly
- Fixed Linux builds failing to load the Hashlink library by producing and referencing an unversioned libhl.so
- Fixed a logging crash when a mod's modinfo.json is missing the version field
- Fixed the mapping of the I8/I16 types to the correct Hashlink unsigned integer types

---

**Code signing policy**: Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

---

# 更新说明

## Feature

- 崩溃报告新增 AI 分析指引，帮助 AI 助手诊断常见崩溃原因，例如内存不足、杀毒软件拦截与版本不匹配，并引导用户前往合适的支持渠道

## Fix

- 适配游戏新版 VM 采用的重写版 Hashlink JIT，包括重新实现 JIT 钩子、更新 VM 结构体定义、预留代码补丁空间并调整函数指针处理
- 修复 res.pak 无法加载的问题，将其重定向至游戏根目录并正确设置工作目录
- 修复 Linux 构建加载 Hashlink 库失败的问题，改为生成并引用无版本号的 libhl.so
- 修复 modinfo.json 缺少 version 字段时日志记录引发的崩溃
- 修复 I8/I16 类型映射，改为映射到正确的 Hashlink 无符号整型

---

**代码签名政策**: 免费代码签名由 [SignPath.io](https://about.signpath.io) 提供，证书由 [SignPath Foundation](https://signpath.org) 颁发。
