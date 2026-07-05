# Release Notes - v35.11.6

## Feature

- Added HLC (HashLink Compiled) compilation and execution support. Bytecode is compiled to native code via crashlink and TCC, enabling native execution as an alternative to JIT compilation.
- Added symbol resolution and debugging support in HLC mode, including enhanced stack traces with function name resolution and PDB generation.
- Added a ProcessUtils utility for redirecting child process standard output and error to the logging system.
- Updated third-party open-source library license documentation with a comprehensive library listing.

## Fix

- Fixed an incorrect array length variable used in loop iteration in UtilityDelegates, which could cause wrong parameter counts for dynamically created delegates.
- Fixed the issue where the native module resolver did not correctly return libhl for std and builtin internal modules.
- Fixed a potential out-of-bounds crash when an exception stack trace exceeds 128 frames.
- Disabled CETCompat to improve compatibility with the HashLink runtime.
- Improved test framework stability by moving GameContext initialization to an assembly fixture and increasing the long-running test timeout.

---

# 更新说明 - v35.11.6

## 新功能

- 新增 HLC (HashLink Compiled) 编译和执行支持，字节码通过 crashlink 生成 C 代码，再由 TCC 编译为原生库执行，作为 JIT 编译的替代方案。
- 新增 HLC 模式下的符号解析和调试支持，包含增强的堆栈跟踪、函数名解析和 PDB 生成。
- 新增 ProcessUtils 工具类，支持将子进程的标准输出和错误输出重定向到日志系统。
- 更新第三方开源库许可文档，新增完整的三方库列表。

## 修复

- 修复 UtilityDelegates 中动态委托创建时使用了错误的数组长度变量，可能导致参数数量不正确的问题。
- 修复原生模块解析器未能正确返回 std 和 builtin 内部模块的 libhl 句柄的问题。
- 修复异常堆栈跟踪在帧数超过 128 时可能越界崩溃的问题。
- 禁用 CETCompat 以提升与 HashLink 运行时的兼容性。
- 改进测试框架稳定性，将 GameContext 初始化移至 AssemblyFixture，并增加长时间测试的超时限制。
