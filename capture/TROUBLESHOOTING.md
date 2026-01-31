# 故障排查指南

## 症状
点击"开始捕获"后，控件信息和窗口信息区域没有显示任何内容。

## 可能的原因和解决方案

### 1. 鼠标在自己的窗口上
**症状**: 鼠标位置在本程序窗口上时，不会显示信息（这是预期行为）

**解决方案**: 将鼠标移动到其他窗口上（如记事本、浏览器等）

---

### 2. FlaUI 无法访问目标窗口
**症状**: CLI 测试正常，但主程序不显示信息

**可能原因**:
- 权限不足
- UI Automation 服务未启动
- 目标窗口不支持 UI Automation

**测试方法**:
```powershell
# 运行 CLI 测试
cd d:\UGit\virgil\win_apps\capture
dotnet run --project tests\WindowCapture.Tests.csproj window
```

如果 CLI 测试能显示窗口信息，说明 FlaUI 本身工作正常。

---

### 3. 查看调试输出

运行程序时，在输出窗口查找以下信息：

#### 正常情况（程序启动时）:
```
FlaUI Test: Cursor at (1256, 697)
FlaUI Test: Successfully captured window 'Chrome'
```

#### 异常情况:
```
FlaUI Test: Failed to capture window (returned null)
FlaUI: FromPoint returned null at (1256, 697)
FlaUI: ConvertToWindowInfo returned null
```

#### 捕获时的调试信息:
```
Error in Timer_Tick: xxx
FlaUI: GetWindowFromPoint exception: xxx
```

---

### 4. 检查是否正确跳过了自己的窗口

在 `Timer_Tick` 方法中添加临时调试：

```csharp
// 在 Timer_Tick 方法的第 100 行附近
if (IsOwnWindow(window.NativeWindowHandle))
{
    System.Diagnostics.Debug.WriteLine("Skipped own window");
    return;
}
```

如果一直显示 "Skipped own window"，说明鼠标一直在自己窗口上。

---

### 5. 常见解决方案

#### 方案 A: 确保鼠标移出程序窗口
将程序窗口移到屏幕一侧，然后将鼠标移动到其他窗口上。

#### 方案 B: 以管理员身份运行
某些系统窗口需要管理员权限：

```powershell
# 以管理员身份打开 PowerShell
cd d:\UGit\virgil\win_apps\capture
dotnet run --project capture.csproj
```

#### 方案 C: 检查 UI Automation 服务
Windows 设置 → 服务 → UI Automation Service → 确保正在运行

#### 方案 D: 测试不同的目标窗口
尝试不同类型的窗口：
- ✅ 记事本（Win32 应用）
- ✅ 浏览器（Chrome/Edge）
- ✅ 文件资源管理器
- ⚠️ 系统设置窗口（可能需要管理员权限）

---

### 6. 验证步骤

1. **运行 CLI 测试**（3秒后将鼠标移到记事本）:
   ```powershell
   dotnet run --project tests\WindowCapture.Tests.csproj window
   ```
   预期输出：显示记事本的窗口信息

2. **运行主程序**:
   ```powershell
   dotnet run --project capture.csproj
   ```

3. **点击"开始捕获"**

4. **将程序窗口移到屏幕左侧**

5. **打开记事本，将鼠标移到记事本窗口上**

6. **观察**:
   - 鼠标位置应该实时更新
   - 控件信息区域应该显示记事本的信息
   - 窗口信息区域应该显示记事本的完整信息

---

### 7. 如何查看调试输出

#### 方法 1: 使用 DebugView (推荐)
下载 [DebugView](https://learn.microsoft.com/en-us/sysinternals/downloads/debugview) 工具

#### 方法 2: 使用 Visual Studio
1. 在 Visual Studio 中打开项目
2. 按 F5 启动调试
3. 查看"输出"窗口

#### 方法 3: 使用 VS Code
1. 配置 launch.json
2. 按 F5 启动调试
3. 查看"调试控制台"

---

## 快速自检清单

- [ ] CLI 测试能正常捕获窗口信息
- [ ] 主程序启动时看到 "FlaUI Test: Successfully captured window"
- [ ] 鼠标在其他窗口上（不在程序自己窗口上）
- [ ] 点击了"开始捕获"按钮（按钮变成红色"停止捕获"）
- [ ] 鼠标位置数字在实时变化
- [ ] 勾选了"显示高亮边框"（应该看到红色边框）

---

## 报告问题时请提供

1. CLI 测试的完整输出
2. 调试输出中的错误信息
3. 您测试的目标窗口类型（记事本、浏览器等）
4. 是否以管理员身份运行
5. 截图（如果可能）
