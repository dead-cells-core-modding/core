# Release Notes - 35.16.1

## Feature

- HashlinkSharp: support replacing game functions with .NET compiled code while tracking the original function so existing hooks keep working.
- HashlinkNET.Compiler: rework the pseudocode compiler to emit ref-based IL and add an option to export the generated pseudocode assembly.
- HashlinkNET.Compiler: add custom cast, safe cast, side-effect tracking and constructor merging support to pseudocode generation.
- HaxeProxy: add fast cached field accessors and helpers for safe casts, constructor invocation and C# overrides of game methods.
- ModCore: print a startup banner with version, repository and license information.
- ModCore: add a skip splash logo option and list loaded mods in the Steam options menu.
- ModCore: add a Core Modding entry to the main options menu.
- ModCore: register side resource mods as regular mods and route resource initialization through the hook system.
- ModCore: unify console close and cursor lock interception and centralize platform native library loading.
- ModCore: add configurable legacy game audio effects with 7.1 output and mono gain compensation.
- ModCore.Utilities: add Bytes creation helpers and dynamic array enumeration.
- Localization: add Chinese translations for the new menu entries.

## Fix

- HashlinkSharp: correct bool, raw pointer and managed pointer type classification, along with null, dynamic and bytes marshaling, by-ref parameters, closure delegate caching, return pointer cleanup and declaring type propagation.
- HashlinkNET.Compiler: normalize exception regions, drop unreachable and no-op instructions, add default returns to truncated bodies, fix handler branches and pin pointer locals to stabilize emitted IL.
- HashlinkNET.Compiler: fix type token emission, boxing and generic instantiation, and remove duplicated assembly references so GameProxy passes JIT validation.
- HaxeProxy: fix field offset lookup and per-instance value caching, and correct bool handling.
- ModCore.Hooks: route only hooked .NET compiled functions through the hook chain.
- ModCore: read and write modded save data through the game file API.
- ModCore: resolve placeholders on the localized template.
- ModCore: keep the Delayer task list sorted on insertion.
- ModCore.Native: always mark managed roots alive during the GC scan.

---

**Code signing policy**: Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

---

# 更新说明

## Feature

- HashlinkSharp：支持用 .NET 编译代码替换游戏函数，并跟踪原始函数以保证现有 hook 正常工作。
- HashlinkNET.Compiler：重写伪代码编译器，改用基于 ref 的 IL 输出，并支持导出生成的伪代码程序集。
- HashlinkNET.Compiler：为伪代码生成新增自定义类型转换、安全转换、副作用跟踪和构造函数合并能力。
- HaxeProxy：新增字段快速访问器，以及安全转换、构造函数调用和用 C# 覆盖游戏方法的辅助接口。
- ModCore：启动时打印包含版本、仓库和许可证信息的横幅。
- ModCore：新增跳过启动徽标选项，并在 Steam 选项菜单中显示已加载的 Mod。
- ModCore：在主选项菜单中新增 Core Modding 入口。
- ModCore：将附属资源 Mod 注册为普通 Mod，并改用 hook 处理资源初始化。
- ModCore：统一控制台关闭和光标锁定的原生拦截，并集中处理平台原生库加载。
- ModCore：新增可配置的旧版游戏音效，支持 7.1 输出和单声道增益补偿。
- ModCore.Utilities：新增 Bytes 创建辅助方法和动态数组枚举支持。
- 本地化：补充新增菜单项的中文翻译。

## Fix

- HashlinkSharp：修正 bool、裸指针和托管指针的类型分类，以及空值、动态值、字节数据的封送、引用参数、闭包委托缓存、返回指针清理和声明类型传递。
- HashlinkNET.Compiler：规范化异常区域，删除不可达代码和无用指令，为截断方法补充默认返回，修复异常处理跳转并固定指针局部变量，提升生成 IL 的稳定性。
- HashlinkNET.Compiler：修正类型标记、装箱和泛型实例化，并去除重复的程序集引用，使生成的 GameProxy 通过 JIT 校验。
- HaxeProxy：修正字段偏移查找和按实例缓存取值，并修正 bool 处理。
- ModCore.Hooks：仅让已被 hook 的 .NET 编译函数走 hook 链。
- ModCore：改为通过游戏文件 API 读写 Mod 存档数据。
- ModCore：修正本地化模板中占位符的替换。
- ModCore：修正 Delayer 任务列表的插入排序。
- ModCore.Native：GC 扫描时始终标记托管根对象存活。

---

**代码签名政策**: 免费代码签名由 [SignPath.io](https://about.signpath.io) 提供，证书由 [SignPath Foundation](https://signpath.org) 颁发。
