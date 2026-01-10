# OneDesk
面向 OneDrive 的 WPF 桌面客户端，基于 Microsoft Graph SDK 构建，内置多用户设备码登录、文件操作任务队列、剪贴板与右键命令体系，以及可配置的主题与冲突处理策略。

## 主要特性
- **多账号设备码登录**：使用设备码流程获取令牌，凭据缓存到可配置的 `user*.json` 文件，支持一键添加/切换/移除账号。
- **文件管理器**：浏览个人根目录与“与我共享”资源；右键菜单支持新建文件夹、重命名、复制/剪切/粘贴（跨驱动器）、删除、查看详情。
- **冲突处理可配置**：支持 `replace` / `rename` / `fail` 三种策略，作用于复制、移动、重命名、新建文件夹等操作。
- **任务调度中心**：所有文件操作统一进入任务系统，按用户隔离队列并发执行，支持优先队列（如新建、重命名）、进度与失败信息展示。
- **剪贴板服务**：内置文件剪贴板，模式区分复制/剪切，用户切换时自动清空。
- **主题与 UI**：WPF-UI 导航界面，支持浅色/深色/跟随系统；文件详情弹窗、导航面包屑、根目录切换等体验优化。

## 运行环境
- Windows 10/11
- .NET 10.0 SDK（预览版，项目目标框架为 `net10.0-windows`）

## 准备工作
1. **注册 Azure AD 应用（公有客户端）**
	- 允许设备码流（Device Code Flow）。
	- Microsoft Graph 委派权限：建议至少授予 `Files.ReadWrite.All` 与 `offline_access`，并管理员同意。
	- 记录应用的 `ClientId`，如需限制租户可填写 `TenantId`。
2. 克隆仓库并还原依赖：
	```bash
	dotnet restore
	```

## 配置
应用首次启动会在可执行目录生成 `appConfig.json`，也可提前创建/修改：

| 键 | 说明 | 默认值 |
| --- | --- | --- |
| `followSystemTheme` | 是否跟随系统主题 | `true` |
| `theme` | 主题（`Light` / `Dark`，跟随系统时会被覆盖为当前值） | `Light` |
| `clientId` | Azure AD 应用 ClientId | 空 |
| `tenantId` | 可选 TenantId（留空则为多租户） | 空 |
| `credentialFolderPath` | 设备码缓存目录，存放 `user*.json` | `users` |
| `activatedUserFileName` | 当前激活用户的缓存文件名 | 空 |
| `conflictBehavior` | 名称冲突策略：`replace` / `rename` / `fail` | `fail` |

> 配置变更会自动防抖保存到 `appConfig.json`。

## 运行
```bash
dotnet run --project OneDesk.csproj
```
启动后：
- 通过主界面左下角添加账号，按提示在浏览器输入设备码完成登录。
- 登录成功后可在左侧导航切换“文件管理”“任务管理”，或在设置页切换主题/冲突策略。

## 目录速览
- `Views/Pages` 与 `ViewModels/Pages`：文件管理、任务管理、设置页面。
- `Views/Windows`：主窗体与文件详情弹窗。
- `Services/Auth`：设备码凭据缓存与多用户管理。
- `Services/FileCommand`：右键命令体系与上下文。
- `Services/Tasks`：任务调度与用户级队列。
- `Services/Clipboard`：文件剪贴板服务。
- `Services/Configuration`：`appConfig.json` 读写与监听。
- `Models/Tasks/Operations`：复制、移动、删除、新建文件夹等具体操作实现。

## 使用提示
- **文件操作**：所有操作都会进入任务队列，可在“任务管理”查看待处理/运行中/已完成/失败任务，部分高优先级任务（重命名、新建文件夹等）完成后自动刷新列表。
- **账号管理**：每个账号对应一个 `userN.json`，切换账号会清空剪贴板并重新加载文件列表，移除账号会删除对应缓存文件。
- **共享内容**：在文件页点击根目录切换，可查看个人根目录与“与我共享”项目。
- **冲突行为**：若目标位置存在同名项，按配置的 `conflictBehavior` 决定覆盖/重命名/失败。

## 致谢
- 头像占位符生成逻辑参考了 DiceBear Initials（helpers/Dicebear/InitialsGenerator.cs）。

## 许可证
[LICENSE](LICENSE)（仓库中为 MIT）
