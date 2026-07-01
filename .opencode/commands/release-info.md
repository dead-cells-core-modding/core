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
15. 请检查语序是否正确！！！!
16. 删除重复的条目
17. 删除feature 和 fix之间重复的条目，仅保留一个
18. commit message可作为参考，但请注！意！：一个commit ！中！可！能！存！在！多！个!修!改!，commit 和！修！改！可！能！不！对！应！，仅！供！参！考！
19. 忽略 CI 相关的更改
20. 忽略 3rd/hashlink 的更改
21. 忽略 latest-release.md 的更改

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

完成以上操作后，拼接 bin/ReleaseInfo.[zh/en].md 输出到 bin/ReleaseInfo.md，要求：
1.不要把中文和英文混合！！！
2.检查语序是否正确，修复不正确的语序 
3.删除或合并重复的条目
4.在保证语序正确的前提下，统一条目语序结构
5.以用于Github Release

同时，将项目当前版本号以`x.x.x`的形式写入 bin/ModCoreVersion.txt
