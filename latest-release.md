# Release Notes - 35.12.1

## Feature

- Added HLC (Hashlink-to-C) compilation support: compile Hashlink bytecode to native C code via TCC compiler, with dynamic loading and type system integration at runtime
- Added HLC debugging support with stack trace capture, symbol resolution, and custom longjump exception handling
- Re-enabled virtual field accessors with hl_obj_lookup fallback for better field resolution on dynamic objects
- Added startup logo splash screen skip via constructor hook
- Migrated from SDL2 to SDL3 graphics library
- Added hl_same_type type comparison and hl_null_access_op null access error native bindings

## Fix

- Moved VerifyGCValidity check to after marking loop completion to prevent premature GC validation
- Fixed exception stack index overflow protection and empty symbol name handling in stack traces
- Disabled CETCompat to resolve Windows Control-flow Enforcement Technology compatibility issues

---

# 更新说明 - 35.12.1

## 新功能

- 新增 HLC（Hashlink 到 C）编译支持：可通过 TCC 编译器将字节码编译为原生 C 代码，支持运行时动态加载及类型系统集成
- 新增 HLC 调试支持：包括堆栈追踪、符号解析及自定义 longjump 异常处理
- 重新启用虚拟字段访问器，并新增 hl_obj_lookup 后备查找以改善动态对象上的字段解析
- 新增启动 Logo 跳过功能：通过构造器 Hook 直接隐藏启动 Logo 画面
- 由 SDL2 迁移至 SDL3 图形库
- 新增 hl_same_type 类型比较绑定及 hl_null_access_op 空访问错误原生绑定

## 修复

- 将 VerifyGCValidity 检查移至标记循环完成后执行，以避免过早验证导致 GC 错误
- 修复异常堆栈遍历时的索引越界保护及空符号名导致的崩溃问题
- 禁用 CETCompat 以修复 Windows 控制流强制技术的兼容性问题
