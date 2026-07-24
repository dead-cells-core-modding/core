# Release Notes - 35.12.6

## Feature

- HashlinkSharp: Added Copy() support for virtual and dynamic objects via native hl_obj_copy and hl_virtual_make_value
- HashlinkSharp: Added type size validation in HashlinkModule for early platform incompatibility detection
- HashlinkSharp: Added SizeOf property to HashlinkType for querying native type sizes
- ModCore.Native: Added P/Invoke declarations for hl_virtual_make_value and hl_obj_copy native functions
- Tests: Added comprehensive HashlinkSharp test suite covering objects, functions, globals, types, and marshal operations
- Tests: Added HaxeProxy advanced test suite covering enums, virtual types, DynObj, Ref&lt;T&gt;, and native functions
- Tests: Added dedicated test suite for nullable int handling in HaxeProxy layer
- Build: Workshop publish pipeline now uses release builds instead of debug builds

## Fix

- HashlinkSharp: Fixed GetValue() not iterating through the full virtual chain in HashlinkVirtual
- HashlinkSharp: Fixed dynamic value marshaling to write to the correct val field offset
- HashlinkNET.Compiler: Fixed IL emit from Unbox to Unbox_Any for Null return type handling in pseudocode generation
- HaxeProxy: Fixed HaxeDynObj copy constructor to use HashlinkDynObj.Copy() instead of raw hl_make_dyn
- HaxeProxy: Fixed HaxeEnum.Equals() to use hl_dyn_compare for proper hashlink value comparison
- HaxeProxy: Fixed HaxeEnum&lt;TEnum,TIndex&gt;.Equals() to delegate comparison to the base class
- HaxeProxy: Added missing unsafe modifier to HaxeEnum base and generic classes
- NonPublicNativeMembers: Suppressed CA1416 platform compatibility warning for the Linux native member scanning branch
- ModCore: Updated Discord invite URL to the current server link
- Haxe2CSharp: Removed unused constructor parameter and dead code in RuntimeHelperRef

---

# 更新说明 - 35.12.6

## Feature

- HashlinkSharp: 新增虚拟对象和动态对象的 Copy() 支持，通过原生 hl_obj_copy 和 hl_virtual_make_value 实现
- HashlinkSharp: 新增 HashlinkModule 类型大小校验，用于提前检测平台不兼容问题
- HashlinkSharp: 新增 HashlinkType.SizeOf 属性，用于查询原生类型大小
- ModCore.Native: 新增 hl_virtual_make_value 和 hl_obj_copy 原生函数的 P/Invoke 声明
- 测试: 新增 HashlinkSharp 综合测试套件，覆盖对象、函数、全局变量、类型和 marshal 操作
- 测试: 新增 HaxeProxy 高级测试套件，覆盖枚举、虚拟类型、DynObj、Ref&lt;T&gt; 和原生函数
- 测试: 新增 HaxeProxy 层 nullable int 处理的专项测试套件
- 构建: Workshop 发布流程现使用 release 构建而非 debug 构建

## Fix

- HashlinkSharp: 修复 GetValue() 未遍历完整虚拟链的问题
- HashlinkSharp: 修复动态值 marshal 写入到正确的 val 字段偏移
- HashlinkNET.Compiler: 修复伪代码生成中 Null 返回类型的 IL 指令从 Unbox 改为 Unbox_Any
- HaxeProxy: 修复 HaxeDynObj 拷贝构造函数改用 HashlinkDynObj.Copy() 替代原始 hl_make_dyn
- HaxeProxy: 修复 HaxeEnum.Equals() 使用 hl_dyn_compare 进行正确的 hashlink 值比较
- HaxeProxy: 修复 HaxeEnum&lt;TEnum,TIndex&gt;.Equals() 将比较委托给基类
- HaxeProxy: 补充 HaxeEnum 基类和泛型类缺失的 unsafe 修饰符
- NonPublicNativeMembers: 抑制 Linux 原生成员扫描分支的 CA1416 平台兼容性警告
- ModCore: 更新 Discord 邀请链接为当前服务器地址
- Haxe2CSharp: 移除 RuntimeHelperRef 中未使用的构造函数参数和死代码
