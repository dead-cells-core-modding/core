# Release Notes - 35.12.10

## Feature

- Steam launcher: add a check-only mode via the DCCM_SHELL_CHECK_ONLY environment variable that validates the environment and exits without launching the game
- Launchers and shells: embed debug symbols and report DOTNET_ROOT diagnostics to ease troubleshooting of startup failures
- Tests: add coverage for .NET exception-to-string conversion across the Hashlink boundary
- Docs: document the SignPath code signing policy applied to distributed binaries

## Fix

- Framework and toolchain: make string operations culture-invariant to eliminate locale-dependent behavior
- ModCore: update SDL3-CS package to 3.4.14

---

**Code signing policy**: Free code signing provided by [SignPath.io](https://about.signpath.io), certificate by [SignPath Foundation](https://signpath.org).

---

# 更新说明

## Feature

- Steam 启动器：新增仅检查模式，通过 DCCM_SHELL_CHECK_ONLY 环境变量验证环境后直接退出，不启动游戏
- 启动器与 Shell：嵌入调试符号并输出 DOTNET_ROOT 诊断信息，便于排查启动失败问题
- 测试：新增 .NET 异常跨 Hashlink 边界字符串转换的覆盖测试
- 文档：补充分发二进制所应用的 SignPath 代码签名政策说明

## Fix

- 框架与工具链：将字符串操作改为区域性无关，消除因区域设置不同产生的行为差异
- ModCore：更新 SDL3-CS 包至 3.4.14

---

**代码签名政策**: 免费代码签名由 [SignPath.io](https://about.signpath.io) 提供，证书由 [SignPath Foundation](https://signpath.org) 颁发。
