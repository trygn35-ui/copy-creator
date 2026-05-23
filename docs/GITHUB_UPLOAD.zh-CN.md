# 第一次上传 GitHub 指南

## 1. 仓库公开还是私有

上传到 GitHub 不等于开源。

- 选择 `Private`：私有仓库，别人看不到。
- 选择 `Public`：公开仓库，别人能看到。
- 暂时不添加 `LICENSE`：代表你还没有授权别人使用这份代码。

第一次上传建议先选 `Private`。

## 2. 上传源码

在项目目录执行：

```powershell
cd "E:\Vibe Coding\copy-creator"
git status --ignored
git add .
git status
git commit -m "Initial commit"
git branch -M main
git remote add origin https://github.com/你的用户名/copy-creator.git
git push -u origin main
```

执行 `git status` 时，不应该看到这些文件进入提交：

```text
node_modules/
dist/
release/
desktop/bin/
desktop/obj/
data/
*.log
.env
```

## 3. 上传运行文件

运行文件不要作为源码提交。正确做法是：

1. 在 GitHub 仓库页面打开 `Releases`。
2. 点击 `Draft a new release`。
3. 填写版本号，例如 `v0.1.0`。
4. 上传运行包 zip，例如 `CopyCreator-win-x64-portable.zip`。

源码放仓库，运行文件放 Release 附件。

## 4. 如果推送失败

常见原因：

- 没有登录 GitHub。
- 仓库地址写错。
- GitHub 上还没创建仓库。
- 本地 remote 已经存在旧地址。

查看 remote：

```powershell
git remote -v
```

修改 remote：

```powershell
git remote set-url origin https://github.com/你的用户名/copy-creator.git
```

