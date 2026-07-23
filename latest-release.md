# Release Notes - 35.12.5

## Feature

- Added HarmonyX integration with a custom Hashlink function patcher, enabling mods to use HarmonyX attributes for hooking game functions
- Added alternative delegate type generation for functions with by-reference parameters in the Hashlink pseudo-code compiler and the runtime proxy system
- Added `tmx collapse` and `tmx expand` commands to DCCMTool for converting tile map XML files to binary format and vice versa
- Added a manual crash log generation button in the core modding menu for debugging purposes
- Expanded the default mod reference list in MDK build targets to include additional core assemblies

## Fix

- Fixed `CommandBase.Execute` method visibility from `public` to `protected` in DCCMTool to prevent unintended external access
- Fixed anonymous delegate creation for DynamicMethods with target objects in HashlinkSharp to avoid incorrect parameter skipping
- Enabled `WINDOWS_EXPORT_ALL_SYMBOLS` in native CMake build to ensure all symbols are exported from the native DLL on Windows

---

# 更新说明 - 35.12.5

## 新增功能

- 新增 HarmonyX 集成与自定义 Hashlink 函数修补器，支持使用 HarmonyX 属性钩取游戏函数
- Hashlink 伪代码编译器与运行时代理系统新增对含引用参数函数的替代委托类型生成支持
- DCCMTool 新增 `tmx collapse` 与 `tmx expand` 命令，用于将瓦片地图 XML 文件与二进制格式相互转换
- 核心 Modding 菜单新增手动生成崩溃日志按钮，便于调试
- MDK 构建目标中扩展了默认 Mod 引用列表，包含更多核心程序集

## 修复

- 修复 DCCMTool 中 `CommandBase.Execute` 方法可见性从 `public` 改为 `protected`，防止意外的外部调用
- 修复 HashlinkSharp 中带目标对象的 DynamicMethod 创建匿名委托时参数跳过错误的问题
- 在原生 CMake 构建中启用 `WINDOWS_EXPORT_ALL_SYMBOLS`，确保 Windows 平台原生 DLL 导出所有符号
