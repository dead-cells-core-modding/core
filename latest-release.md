# Release Notes - 35.13.2

## Feature

- Move error reporting from the Steam launcher into DCCMShell
- Add a Serilog configuration extension point and an error output redirection option to log initialization, so the error reporter can capture game logs
- Add an overload to WorkerProcessUtils that configures whether the worker process exits with its parent, keeping the error reporting process alive

## Fix

- Fix HashlinkSharp pseudo-code generation failing when a type name collision causes DefineType to throw, by retrying with a new assembly
- Fix the Steam launcher not proactively subscribing to and downloading the DCCM workshop item, which delayed MAPI updates

---

**Code signing policy**: Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

---

# 更新说明

## Feature

- 错误报告功能从 Steam 启动器迁移至 DCCMShell 
- 日志初始化新增 Serilog 配置扩展点与错误输出重定向选项，供错误报告器捕获游戏日志
- WorkerProcessUtils 新增可配置子进程是否随父进程退出的重载，用于保持错误报告进程常驻

## Fix

- 修复 HashlinkSharp 伪代码生成时因类型名冲突导致 DefineType 抛出异常的问题，改为使用新程序集重试
- 修复 Steam 启动器未主动订阅并下载 DCCM Workshop 条目导致 MAPI 更新不及时的问题

---

**代码签名政策**: 免费代码签名由 [SignPath.io](https://about.signpath.io) 提供，证书由 [SignPath Foundation](https://signpath.org) 颁发。
