# Windows 下使用 Cursor Agent，并让 Goose 通过它运行（WSL 方案）

## 适用场景

- 你在 Windows 上使用 Goose
- Goose 的 `CURSOR_AGENT_COMMAND` 需要一个 **可被 spawn 的 `.exe`**
- Cursor 官方安装脚本更适合跑在 WSL（Linux）里

本方案通过一个很小的 **Windows wrapper 可执行文件**（`cursor-agent.exe`）去调用 WSL 里的 `cursor-agent`，让 Goose 在 Windows 上稳定启动 Cursor Agent。

## 重要提醒（必读）

- Cursor Agent 的响应可能比较慢
- Goose UI 里卡在 **“goose is working on it”** 一段时间是正常现象，尤其是首次调用、网络抖动、或模型负载高的时候

## 一次性安装步骤

### 1) 确认 WSL 发行版

确保你有一个正常的 Linux 发行版（推荐 `Ubuntu-24.04`）：

```powershell
wsl.exe -l -v
```

如没有，可安装：

```powershell
wsl.exe --install -d Ubuntu-24.04
```

### 2) 在 WSL 里安装 Cursor Agent（官方脚本）

在 Windows PowerShell 里用 root 跑（避免交互）：

```powershell
wsl.exe -d Ubuntu-24.04 -u root -- bash -lc "apt-get update -y && apt-get install -y curl ca-certificates && curl -fsSL https://cursor.com/install | bash"
```

验证：

```powershell
wsl.exe -d Ubuntu-24.04 -u root -- /root/.local/bin/cursor-agent --version
```

### 3) 生成并安装 Windows wrapper（给 Goose 用）

在本仓库根目录执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\\goose-cursor-agent-windows\\scripts\\build-and-install-wrapper.ps1
```

它会：

- 编译 `goose-cursor-agent-windows\\wrapper\\cursor-agent-wrapper.cs`
- 生成 `cursor-agent.exe`
- 复制到 `%LOCALAPPDATA%\\Microsoft\\WindowsApps\\cursor-agent.exe`（通常已在 PATH）

验证：

```powershell
cursor-agent --help
cursor-agent --version
```

## Goose 配置

Goose 配置文件默认路径：

- `%APPDATA%\\Block\\goose\\config\\config.yaml`

建议加两行（或确认存在）：

```yaml
GOOSE_PROVIDER: cursor-agent
CURSOR_AGENT_COMMAND: cursor-agent
```

说明：

- `CURSOR_AGENT_COMMAND` 必须是 **单个可执行文件**（`.exe`）或其在 PATH 中的名字
- 不要在这里写 `wsl.exe -d ...` 这种“命令 + 参数”的整串内容，Goose 通常不会按 shell 方式解析

## 常见问题

### 1) `cursor-agent --help` 没输出 / WSL 报超时

有时 WSL 会卡住，执行：

```powershell
wsl.exe --shutdown
```

然后再试：

```powershell
cursor-agent --help
```

### 2) 想在别的电脑复用

本方案的 wrapper 默认写死了：

- WSL 发行版名：`Ubuntu-24.04`
- WSL 用户：`root`
- WSL 内 cursor-agent 路径：`/root/.local/bin/cursor-agent`

如果目标电脑不同，请修改 `wrapper\\cursor-agent-wrapper.cs` 顶部的默认值或用环境变量覆盖（见源码注释），然后重新运行构建脚本即可。

