# Release Notes - 35.14.2

## Feature

- Tests: Expanded integration test coverage for custom Haxe types, primitive marshaling, hook parameter forwarding, hook exception propagation, and GC concurrency behavior.

## Fix

- HashlinkSharp: Normalized boolean marshaling so any non-zero Hashlink byte is read as true instead of only the value 1.
- HashlinkSharp: Made object type member caches thread-safe to prevent races during concurrent reflection access.
- ModCore.Native: Corrected the GC mark-threads-active flag to the proper integer type to match the native memory layout.
- Event system: Removed a debug assertion in the receiver cache that could fail under concurrent event dispatch.

---

**Code signing policy**: Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

---

# 更新说明

## Feature

- 测试: 扩展了针对自定义 Haxe 类型、基础类型封送、Hook 参数转发、Hook 异常传播以及 GC 并发行为的集成测试覆盖。

## Fix

- HashlinkSharp: 规范化布尔值封送，任何非零的 Hashlink 字节都会被读取为 true，而不再仅限数值 1。
- HashlinkSharp: 使对象类型成员缓存线程安全，避免并发反射访问时出现竞态。
- ModCore.Native: 将 GC 标记线程活动标志修正为正确的整数类型，以匹配原生内存布局。
- 事件系统: 移除了接收者缓存中可能在并发事件分发时触发的调试断言。

---

**代码签名政策**: 免费代码签名由 [SignPath.io](https://about.signpath.io) 提供，证书由 [SignPath Foundation](https://signpath.org) 颁发。
