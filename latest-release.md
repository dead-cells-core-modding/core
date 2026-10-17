# Release Notes - 35.13.9

## Feature

- No new features in this release.

## Fix

- HashlinkSharp: register the Hashlink thread before allocation and marshaling operations, preventing crashes when Hashlink objects are created from threads that are not yet registered.
- ModCore.Native: always terminate the process immediately on a Hashlink fatal error instead of continuing into the original fatal error handler.
- ModCore: set the synchronization context for queued synchronized tasks and restore it afterwards, so tasks and their continuations run under the expected context.

---

**Code signing policy**: Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

---

# 更新说明

## Feature

- 本版本无新增功能。

## Fix

- HashlinkSharp：在内存分配和编组操作前注册 Hashlink 线程，避免从未注册的线程创建 Hashlink 对象时发生崩溃。
- ModCore.Native：Hashlink 发生致命错误时始终立即终止进程，不再继续执行原有的致命错误处理逻辑。
- ModCore：为队列中的同步任务设置同步上下文并在处理结束后恢复，确保任务及其延续在预期的上下文中运行。

---

**代码签名政策**: 免费代码签名由 [SignPath.io](https://about.signpath.io) 提供，证书由 [SignPath Foundation](https://signpath.org) 颁发。
