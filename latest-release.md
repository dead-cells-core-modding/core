# Release Notes - 35.14.1

## Feature

- HashlinkSharp: added a per-thread hashlink type resolution cache and pooled dynamic values for common primitives, reducing allocation and lookup cost in hot marshaling paths.
- HashlinkSharp: closure invocation now uses cached fast invokers, field-name hashes are cached and extra data reads no longer take a reader-writer lock.
- HaxeProxy: streamlined hooked function invocation with cached call info and removed the legacy real-entry lookup path.
- ModCore.Common: event dispatch now reuses cached receiver snapshots and tracks receiver counts, replacing the per-call receiver enumeration.
- ModCore.Common: removed the OnBeforeBroadcastEvent and OnAfterBroadcastEvent extension points as a breaking API change for receivers that used them.
- ModCore: composed hook chains are cached and invalidated when hooks are added or removed, and the hook origin closure is now thread-local.
- ModCore: short strings are cached per thread when converted to Haxe strings.
- Benchmark: added an in-process benchmark project covering marshaling, invocation, HaxeProxy, hooks and event dispatch.
- TestRunner: added automated coverage for the event system, marshaling, hook caching and executable memory allocation.

## Fix

- HashlinkSharp: corrected the exception-handler shellcode layout and buffer alignment so the context buffer is passed correctly to the store routine and stack traces stay hidden.
- HashlinkSharp: callback delegates go through dynamic invoke again while wrapper delegates keep the fast path, restoring correct callback behavior.
- ModCore.Native: executable memory cells now record their owning page, fixing incorrect cell release when a page spans multiple system pages.
- ModCore.Native.Fody: generated native stubs carry the SuppressGCTransition calling convention again, avoiding unnecessary GC transitions.

---

**Code signing policy**: Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

# 更新说明

## Feature

- HashlinkSharp: 新增线程级的 hashlink 类型解析缓存与常用基础类型动态值池，降低热路径编组过程中的分配与查找开销。
- HashlinkSharp: 闭包调用改用缓存的快速调用器，字段名哈希改为缓存，附加数据读取不再使用读写锁。
- HaxeProxy: 通过缓存调用信息精简被挂钩函数的调用流程，并移除旧的真实入口查找路径。
- ModCore.Common: 事件分发复用缓存的接收者快照并跟踪接收者数量，替代每次调用时的接收者枚举。
- ModCore.Common: 移除 OnBeforeBroadcastEvent 与 OnAfterBroadcastEvent 扩展点，对使用过它们的接收者属于破坏性 API 变更。
- ModCore: 组合后的钩子链会被缓存，并在增删钩子时失效，同时钩子原始闭包改为线程级存储。
- ModCore: 短字符串转换为 Haxe 字符串时按线程进行缓存。
- Benchmark: 新增进程内基准测试项目，覆盖编组、调用、HaxeProxy、钩子与事件分发。
- TestRunner: 为事件系统、编组、钩子缓存与可执行内存分配补充自动化测试。

## Fix

- HashlinkSharp: 修正异常处理器的 shellcode 布局与缓冲区对齐，使上下文缓冲区正确传递给存储例程并保持堆栈跟踪隐藏。
- HashlinkSharp: 回调委托重新走动态调用，同时包装委托仍保留快速路径，恢复正确的回调行为。
- ModCore.Native: 可执行内存单元现在记录所属页，修复页跨越多个系统页时的错误释放问题。
- ModCore.Native.Fody: 生成的本地存根重新携带 SuppressGCTransition 调用约定，避免不必要的 GC 转换。

---

**代码签名政策**: 免费代码签名由 [SignPath.io](https://about.signpath.io) 提供，证书由 [SignPath Foundation](https://signpath.org) 颁发。
