---

description: 生成 release notes

---

对比当前 HEAD 与上一 tag 的 diff（忽略 commit message），按以下步骤生成 ReleaseInfo：

1. 逐文件分析实际 diff 内容，按功能模块分组！！！
2. 每个模块用一句话描述目的
3. 分类为 Feature / Fix
4. 认真核对 Feature 和 Fix 的区别！！！
5. 认真核对每个模块！！！
6. 无需输出具体的文件名和具体的更改！！！
7. 文件访问仅限于“存在 build.ps1 和 build.sh"的文件夹及其子文件夹（即项目根目录及其子目录）!!!!!!!
8. 请注意：HashlinkNET.* 项目的作用是为了生成"用于生成伪代码"的 dll !!!!!!!!!
9. 请确认 commit 的顺序，减少不必要的 message
10. 使用中文/英文 双语!!!!!!!!!!!!!!!!!!!!
11. ReelaseInfo输出到项目根目录的 bin/ReleaseInfo.[zh/en].md
12. 请确定号版本号
13. 请从Directory.Build.props中获取版本号
14. 输出中永远不要有双引号！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！！请使用单引号代替！！！！

ReleaseInfo.en.md 格式：
```
# Release Notes - <version>

## Feature

- ...

## Fix

- ...

```

ReleaseInfo.zh.md 格式:
```

# 更新说明

...

```

完成以上操作后，拼接 bin/ReleaseInfo.[zh/en].md 输出到 bin/ReleaseInfo.md ，不要把中文和英文混合！！！ ，以用于Github Release

同时，将项目当前版本号以`x.x.x`的形式写入 bin/ModCoreVersion.txt
