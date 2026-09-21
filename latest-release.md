# Release Notes - 35.13.6

## Feature

- ModCore: add a menu toggle to disable DCCM automatic updates and an action to update DCCM immediately through Steam, and restrict the Workshop menu entries to the Steam platform.
- SteamLauncher: the auto-updater now respects a locally disabled Workshop item and skips automatic updates when the user has disabled them.
- ModCore.Assets: add Chinese translations for the new Steam Workshop menu options and refine existing strings.

## Fix

- ModCore.Native / HashlinkSharp: correct the GC allocation hook to intercept hl_gc_alloc_gen and validate the dynamic object memory size during marshaling.
- DCCMTool: fix invalid Spectre.Console markup in the unknown Workshop tag warning.
- Build tooling: update Microsoft.Build and CsWin32 dependency versions.

---

**Code signing policy**: Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

---

# 更新说明

## Feature

- ModCore：新增禁用 DCCM 自动更新的菜单开关，以及通过 Steam 立即更新 DCCM 的菜单操作，并将创意工坊相关菜单项限制为仅在 Steam 平台显示。
- SteamLauncher：自动更新器现在会识别被本地禁用的创意工坊项目，在用户禁用自动更新时跳过更新。
- ModCore.Assets：为新增的 Steam 创意工坊菜单选项补充中文翻译，并优化已有翻译文案。

## Fix

- ModCore.Native / HashlinkSharp：修正 GC 分配钩子，改为拦截 hl_gc_alloc_gen，并在封送处理时校验动态对象的内存大小。
- DCCMTool：修复未知创意工坊标签警告中无效的 Spectre.Console 标记。
- 构建工具：更新 Microsoft.Build 与 CsWin32 依赖版本。

---

**代码签名政策**: 免费代码签名由 [SignPath.io](https://about.signpath.io) 提供，证书由 [SignPath Foundation](https://signpath.org) 颁发。
