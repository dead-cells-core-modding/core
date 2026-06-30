---
description: 生成 commit message
---



为当前未提交的文件生成 commit message，要求：

1. 使用英文!!!
2. 逐文件分析实际 diff 内容，按功能模块分组！！！
3. 认真核对每个模块！！！
4. 认真核对 feature 和 fix 和 chore 和 ci 和 refactor 的区别！！！
5. 文件访问仅限于“存在 build.ps1 和 build.sh"的文件夹及其子文件夹（即项目根目录及其子目录）!!!!!!!
6. 请注意：HashlinkNET.* 项目的作用是为了生成"用于生成伪代码"的 dll !!!!!!!!!
7. 生成 subject 和 body
8. body 简要概括每个模块发生了什么变化

commit message格式为:


```
feat/fix/chore/ci/refactor: ...
...

```



