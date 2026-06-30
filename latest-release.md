# Release Notes - 35.11.2

## Feature

- Automated release info generation: the CI pipeline now generates AI-powered release notes during tagged builds and uses them as the GitHub Release body.
- Steam Workshop update enhancement: the Workshop item update description now uses the full release notes content instead of a simple version string.

## Fix

- CMake build failure detection: CMake configure and build steps now properly report errors, preventing silent failures that previously went undetected.

---

# 更新说明 - 35.11.2

## 新功能

- 自动生成发布说明：CI 流水线现可在标签构建时自动生成 AI 驱动的发布说明，并将其用作 GitHub Release 正文。
- Steam 创意工坊上传增强：创意工坊物品更新描述现改用完整发布说明内容，替代了原先简短的版本号字符串。

## 修复

- CMake 构建失败检测：CMake 配置与构建步骤现在会正确报告错误，避免此前构建失败被静默忽略的问题。
