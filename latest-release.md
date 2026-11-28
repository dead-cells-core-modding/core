# Release Notes - 35.16.2

## Feature

- No new features in this release.

## Fix

- ModCore: Register hero, score, error handler, and splash screen hooks during advanced module initialization so game hooks are installed at the correct stage, always install the error handler hook, and remove obsolete assertions from the boot main loop.
- HaxeProxy: Avoid writing to a null field value cache when reading nullable value-type proxy fields, so non-null and null nullable fields return correct values.

---

**Code signing policy**: Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

---

# 更新说明

## Feature

- 本次更新无新增功能。

## Fix

- ModCore：将英雄、分数、错误处理与启动画面钩子的注册移至高级模块初始化阶段，以保证钩子在正确的时机安装；错误处理钩子改为始终安装，并移除启动主循环中已过时的断言。
- HaxeProxy：修复读取可空值类型代理字段时向空字段缓存写入的问题，使非空与空的可空字段都能返回正确的值。

---

**代码签名政策**: 免费代码签名由 [SignPath.io](https://about.signpath.io) 提供，证书由 [SignPath Foundation](https://signpath.org) 颁发。
