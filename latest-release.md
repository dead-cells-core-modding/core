# Release Notes - 35.12.7

## Feature

- Added Discord announcement build target for automated release notifications via webhook
- Added IOnResolveHashlinkType event interface allowing modules to customize Hashlink type resolution
- HashlinkMarshal constructor now performs platform compatibility validation with roundtrip conversion checks
- HaxeProxyManager integrates with the type resolution event system and adds nint type mapping for proxy types
- Enhanced hook tests to cover multiple simultaneous handler chains and dynamic delegate roundtrip scenarios

## Fix

- Generic type instances are now correctly wrapped during Hashlink bytecode pseudo-code import
- Delegate calli emitter now uses the correct return type when generating Haxe-to-CLR marshaling stubs
- Proxy type resolution properly delegates to the marshal system with proper fallback when no custom type is registered

---

# 更新说明 - 35.12.7

## Feature

- 新增 Discord 发布公告构建目标，支持通过 Webhook 自动发送版本更新通知
- 新增 IOnResolveHashlinkType 事件接口，允许模块自定义 Hashlink 类型解析逻辑
- HashlinkMarshal 构造函数现在会进行平台兼容性验证，包含往返转换检查
- HaxeProxyManager 集成了类型解析事件系统，并新增了 nint 类型的代理映射
- 增强了 Hook 测试，覆盖多处理器链式调用和动态委托往返转换场景

## Fix

- 修复 Hashlink 字节码伪代码导入时泛型类型实例未正确包装的问题
- 修复委托 calli 发射器在生成 Haxe-to-CLR 封送桩代码时使用了错误的返回类型
- 修复代理类型解析在无自定义类型注册时未正确回退到封送系统的问题
