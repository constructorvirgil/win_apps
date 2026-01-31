# 快速开始指南

## 1. 快速验证 - 使用 CLI 测试工具

最快的验证方式是使用 CLI 工具：

### 测试鼠标位置捕获
```powershell
cd d:\UGit\virgil\win_apps\capture
dotnet run --project tests\WindowCapture.Tests.csproj cursor
# 3秒后会显示鼠标位置
```

### 测试窗口信息捕获
```powershell
dotnet run --project tests\WindowCapture.Tests.csproj window
# 3秒后将鼠标移动到任意窗口，会显示完整的窗口信息
```

### 测试深度控件捕获
```powershell
dotnet run --project tests\WindowCapture.Tests.csproj deep
# 3秒后将鼠标移动到窗口中的按钮、文本框等控件
```

### 持续监控模式
```powershell
dotnet run --project tests\WindowCapture.Tests.csproj monitor 500
# 实时监控，移动鼠标会自动更新信息
# 按 Ctrl+C 退出
```

## 2. 运行主程序

```powershell
cd d:\UGit\virgil\win_apps\capture
dotnet run --project capture.csproj
```

## 3. 主程序使用步骤

1. 点击"开始捕获"按钮
2. 移动鼠标到目标窗口上
3. 查看右侧显示的窗口信息：
   - 鼠标位置
   - 控件信息（如果勾选了"捕获控件"）
   - 窗口信息（包括 AutomationId、ControlType、FrameworkId）
   - 窗口树（切换到"窗口树"标签页查看）

### 快捷键提示

- **Ctrl**: 按住时只捕获顶级窗口，不捕获子控件
- **Shift**: 按住时停止捕获并保留当前显示的信息

## 4. 验证 FlaUI 特有功能

FlaUI 相比传统 Win32 API 的优势体现在以下字段：

- **AutomationId**: 控件的自动化标识符（用于自动化测试）
- **ControlType**: 控件类型（Button、TextBox、CheckBox 等）
- **FrameworkId**: UI 框架标识（WPF、WinForms、Win32、Qt 等）
- **SupportedPatterns**: 支持的 UI Automation Patterns

### 测试不同类型的应用

试着捕获以下类型的窗口：

1. **WPF 应用**（如 Visual Studio）
   - FrameworkId 会显示 "WPF"
   - AutomationId 通常有值
   
2. **WinForms 应用**
   - FrameworkId 会显示 "WinForms"
   
3. **Win32 应用**（如记事本）
   - FrameworkId 会显示 "Win32"
   - AutomationId 通常为空

4. **浏览器**（Chrome、Edge）
   - 可以捕获到网页中的控件

## 5. 架构优势

如果将来需要切换底层实现，只需：

```csharp
// 在 MainWindow.xaml.cs 的构造函数中
// 从 FlaUI 切换到其他实现
_windowCapture = new FlaUIWindowCapture();  // 当前
// _windowCapture = new Win32WindowCapture();  // 切换到 Win32
```

所有业务逻辑无需改动！

## 常用 CLI 命令速查

```powershell
# 获取鼠标位置
dotnet run --project tests\WindowCapture.Tests.csproj cursor

# 获取窗口信息
dotnet run --project tests\WindowCapture.Tests.csproj window

# 深度捕获控件
dotnet run --project tests\WindowCapture.Tests.csproj deep

# 查看窗口树
dotnet run --project tests\WindowCapture.Tests.csproj tree

# 监控模式（500ms刷新）
dotnet run --project tests\WindowCapture.Tests.csproj monitor 500
```

## 故障排除

### 编译错误
```powershell
# 清理并重新编译
dotnet clean
dotnet build
```

### 权限问题
某些系统窗口需要管理员权限才能访问，以管理员身份运行 PowerShell 再执行命令。
