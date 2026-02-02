# 窗口信息获取工具

基于 FlaUI 的 Windows 窗口信息捕获工具，支持精确的窗口和控件识别。

## 功能特性

- ✨ 基于 UI Automation 的精确窗口识别
- 🎯 深度控件捕获功能
- 🌲 窗口层次结构树形展示
- 🔍 实时鼠标位置追踪
- 📊 详细的窗口属性信息（AutomationId、ControlType、FrameworkId 等）
- 🎨 可视化高亮边框

## 项目结构

```
capture/
├── Core/                          # 核心类库
│   ├── IWindowCapture.cs         # 窗口捕获接口
│   ├── FlaUIWindowCapture.cs     # FlaUI 实现
│   ├── WindowInfo.cs             # 窗口信息模型
│   └── WindowCapture.Core.csproj
├── tests/                         # CLI 测试工具
│   ├── Program.cs
│   └── WindowCapture.Tests.csproj
├── MainWindow.xaml               # WPF 主窗口
├── MainWindow.xaml.cs
├── HighlightWindow.xaml          # 高亮边框窗口
└── capture.csproj
```

## 快速开始

### 运行主程序

```powershell
dotnet run --project capture.csproj
```

### 使用 CLI 测试工具

CLI 工具提供快速验证窗口捕获功能的命令：

```powershell
# 获取鼠标位置
dotnet run --project tests\WindowCapture.Tests.csproj cursor

# 获取鼠标位置的窗口信息
dotnet run --project tests\WindowCapture.Tests.csproj window

# 深度搜索控件
dotnet run --project tests\WindowCapture.Tests.csproj deep

# 显示窗口树
dotnet run --project tests\WindowCapture.Tests.csproj tree

# 持续监控模式（500ms 间隔）
dotnet run --project tests\WindowCapture.Tests.csproj monitor 500
```

### CLI 命令说明

- **cursor**: 3秒后获取一次鼠标位置
- **window**: 3秒后获取鼠标位置的窗口信息（包括根窗口）
- **deep**: 3秒后深度搜索鼠标位置的最深层控件
- **tree [handle]**: 显示窗口的层次结构树（可选指定句柄）
- **monitor [interval]**: 持续监控模式，实时显示鼠标位置的窗口信息（按 Ctrl+C 退出）

## 使用说明

### 主界面功能

1. **开始/停止捕获**: 点击按钮开始实时捕获鼠标位置的窗口信息
2. **显示高亮边框**: 勾选后会在目标窗口周围显示红色边框
3. **捕获控件**: 勾选后会捕获子控件，否则只捕获顶级窗口

### 快捷键

- **Ctrl**: 按住时临时禁用控件捕获，只捕获顶级窗口
- **Shift**: 按住时停止捕获并保留当前信息

### 显示信息

#### 鼠标位置
- 屏幕坐标：相对于整个屏幕的坐标
- 窗口坐标：相对于窗口客户区的坐标

#### 控件信息（仅在捕获控件时显示）
- 控件句柄
- 控件类名
- 控件文本
- 控件位置和大小

#### 窗口信息
- 窗口句柄
- 窗口标题
- 窗口类名
- 窗口位置和大小
- 进程 ID 和名称
- 父窗口句柄
- 窗口样式
- **AutomationId**: UI Automation 标识符（FlaUI 特有）
- **控件类型**: 控件的类型（如 Button、TextBox、Window 等）
- **框架ID**: 窗口所使用的 UI 框架（如 WPF、WinForms、Win32）

#### 窗口树
显示当前窗口的完整层次结构，包括所有子窗口和控件。

## 架构设计

### 抽象层设计

项目采用接口抽象设计，便于切换不同的窗口捕获底层实现：

```csharp
IWindowCapture                    # 窗口捕获接口
└── FlaUIWindowCapture           # FlaUI 实现（基于 UI Automation）
```

### 核心接口

`IWindowCapture` 提供以下核心方法：

- `GetCursorPosition()`: 获取鼠标位置
- `GetWindowFromPoint()`: 从坐标获取窗口
- `GetRootWindow()`: 获取根窗口
- `ScreenToClient()`: 坐标转换
- `BuildWindowTree()`: 构建窗口树
- `GetChildWindows()`: 获取子窗口列表

### 扩展其他实现

如果需要切换到其他窗口捕获技术（如 Win32 API、Accessibility API 等），只需：

1. 实现 `IWindowCapture` 接口
2. 在 `MainWindow` 构造函数中替换实现：

```csharp
// 当前使用 FlaUI
_windowCapture = new FlaUIWindowCapture();

// 切换到其他实现（如 Win32）
_windowCapture = new Win32WindowCapture();
```

## 技术栈

- **.NET 8.0 (Windows)**
- **WPF**: Windows Presentation Foundation
- **FlaUI 5.0**: UI Automation 库
  - FlaUI.Core
  - FlaUI.UIA3

## 开发指南

### 编译项目

```powershell
# 编译核心类库
dotnet build ..\shared\WindowCapture.Core\WindowCapture.Core.csproj

# 编译测试工具
dotnet build tests\WindowCapture.Tests.csproj

# 编译主程序
dotnet build capture.csproj
```

### 添加新的捕获实现

1. 在 `Core` 目录下创建新类，实现 `IWindowCapture` 接口
2. 实现所有必需的方法
3. 在 `MainWindow.xaml.cs` 中切换实现

示例：

```csharp
public class CustomWindowCapture : IWindowCapture
{
    public Point GetCursorPosition() { /* 实现 */ }
    public WindowInfo? GetWindowFromPoint(Point screenPoint, bool deepSearch = false) { /* 实现 */ }
    // ... 其他方法
}
```

## 常见问题

### Q: 为什么有些窗口无法捕获？
A: 某些应用程序可能有提升的权限或使用了特殊的窗口技术。尝试以管理员身份运行本工具。

### Q: AutomationId 显示为"(无)"？
A: 并非所有控件都设置了 AutomationId，这取决于应用程序开发者的实现。

### Q: 如何切换回 Win32 API？
A: 实现一个新的 `IWindowCapture` 类使用 Win32 API，然后在 `MainWindow` 构造函数中切换即可。

## License

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request！
