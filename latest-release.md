# Release Notes - 35.11.3

## Feature

- Added full Linux platform support including native interop, TLS handling, library loading, build pipeline, and test CI
- Added HLVM trap/exception handling in pseudocode compiler for generating .NET try-catch exception handlers
- Added automated workshop publish pipeline with platform-specific content layout and release note changelog

## Fix

- Fixed trap magic number to use runtime-allocated value preventing potential memory conflicts across native modules
- Fixed jmp_buf struct size mismatch between Windows and Linux causing struct layout corruption during exception handling
- Fixed library base address resolution on Linux where dlopen returns a link_map pointer rather than a direct base address
- Fixed native library loading to use libhl.so.1 on Linux and added crash protection around SteamPlatformModule initialization and SteamAPI callbacks
- Fixed release workflow push order and PublishMAPI dependency ordering
- Fixed version string trimming when reading ModCoreVersion.txt
- Updated Goldberg emulator binaries for Linux compatibility

---

# 更新说明

## 新功能

- 新增完整的 Linux 平台支持，包括原生互操作、TLS 处理、库加载、构建流水线和测试 CI
- 新增伪代码编译器中的 HLVM 陷阱/异常处理支持，可生成 .NET try-catch 异常处理器
- 新增自动化创意工坊发布流水线，支持平台特定的内容布局和更新日志

## 修复

- 修复了陷阱魔数使用运行时分配的值，避免跨原生模块的潜在内存冲突
- 修复了 Windows 和 Linux 之间 jmp_buf 结构体大小不匹配导致异常处理时结构布局损坏的问题
- 修复了 Linux 上库基地址解析问题，dlopen 返回的是 link_map 指针而非直接基地址
- 修复了 Linux 上原生库加载使用 libhl.so.1 文件名，并为 SteamPlatformModule 初始化和 SteamAPI 回调添加崩溃保护
- 修复了发布工作流的推送顺序和 PublishMAPI 依赖排序
- 修复了读取 ModCoreVersion.txt 时版本字符串未去除空白字符的问题
- 更新了 Goldberg 模拟器二进制文件以支持 Linux 兼容性
