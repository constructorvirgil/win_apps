# drvercheck

独立的驱动验收/回归 CLI，用来验证本仓库 `Core/Drivers` 下的输入驱动在当前机器上是否“真的能打出键鼠事件”。

## 核心思路

- `drvercheck selftest` 会启动一个 **Probe** 窗口进程，然后用指定 `IInputDriver` 注入键鼠事件。
- Probe 同时用两种方式收集证据：
  - 窗口/控件消息（尽量做端到端：字符真的进了 TextBox）
  - 全局低级 Hook（`WH_KEYBOARD_LL` / `WH_MOUSE_LL`）观察系统是否产生了注入事件（在某些“无法抢焦点”的环境里更稳定）

## 用法

构建：

```powershell
dotnet build .\tools\drvercheck\drvercheck.csproj -c Debug
```

验收 SendInput：

```powershell
dotnet run --project .\tools\drvercheck\drvercheck.csproj -c Debug -- selftest --driver SendInput
```

验收 Interception：

```powershell
dotnet run --project .\tools\drvercheck\drvercheck.csproj -c Debug -- selftest --driver Interception
```

说明（自动化准备）：

- `drvercheck` 会先检测系统中是否存在 Interception 的内核驱动服务（`sc query keyboard` / `sc query mouse`）。
- 运行 `Interception` 自测时，会优先从系统 DLL 搜索路径加载 `interception.dll`；若加载失败，会尝试从本仓库的构建输出（例如 `.\bin\Debug\net8.0-windows\interception.dll`）自动复制到 `drvercheck` 的输出目录后再加载。

JSON 报告（便于 CI/日志收集）：

```powershell
dotnet run --project .\tools\drvercheck\drvercheck.csproj -c Debug -- selftest --driver SendInput --json
```

仅启动 Probe（用于你手动点窗口、观察消息等）：

```powershell
dotnet run --project .\tools\drvercheck\drvercheck.csproj -c Debug -- probe --pipe drvercheck_manual
```

## Interception 前置条件

`InterceptionDriver` 依赖：

- `interception.dll` 与 `drvercheck.exe` 同目录（或在系统 DLL 搜索路径中）
- Interception 内核驱动已安装（通常需要管理员权限安装，并重启后生效）

若前置条件不满足，`selftest --driver Interception` 会输出 `skip`（不算失败），并提示如何准备环境。

## 运行注意

- 必须在**交互式桌面会话**中运行（不要在 Windows 服务/Session 0 环境跑）。
- 运行时会移动鼠标并模拟点击/键盘输入，请避免在重要操作中运行。
