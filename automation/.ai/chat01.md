# 虚拟 HID 键盘/鼠标模拟器开发总结

## 项目概述

在 `automation` 工程下开发了一个虚拟 HID 键盘/鼠标模拟器，支持：
- 鼠标：移动、点击、滚轮、拖拽
- 键盘：单键、组合键（如 Ctrl+C）、文本输入
- 双驱动架构：SendInput（用户模式）和 Interception（内核模式）

## 项目结构

```
automation/
├── Core/                           # 核心层
│   ├── Drivers/                    # 驱动层
│   │   ├── IInputDriver.cs         # 驱动接口定义
│   │   ├── SendInputDriver.cs      # SendInput API 实现
│   │   ├── InterceptionDriver.cs   # Interception 驱动实现
│   │   └── DriverManager.cs        # 驱动管理器
│   ├── Native/
│   │   └── NativeMethods.cs        # Windows API P/Invoke 封装
│   ├── HidDefinitions/
│   │   └── VirtualKeyCodes.cs      # 虚拟键码定义
│   └── Interfaces/
│       ├── IVirtualKeyboard.cs     # 键盘接口
│       └── IVirtualMouse.cs        # 鼠标接口
├── Devices/                        # 设备实现层
│   ├── VirtualKeyboard.cs          # 键盘设备
│   └── VirtualMouse.cs             # 鼠标设备
├── Services/                       # 服务层
│   ├── InputSimulator.cs           # 统一 API 入口
│   └── InterceptionInstaller.cs    # 驱动自动安装器（新增）
└── MainWindow.xaml/cs              # 测试界面
```

## 双驱动架构

| 驱动 | 级别 | 支持锁屏 | 安装要求 |
|------|------|----------|---------|
| **SendInput** | 用户模式 | ❌ 不支持 | 无需安装（默认） |
| **Interception** | 内核模式 | ✅ 支持 | 需要安装驱动 |

### 使用方式

```csharp
// 创建模拟器
var simulator = InputSimulator.Create();

// 切换驱动
InputSimulator.SetDriver(DriverType.Interception);

// 鼠标操作
simulator.MoveTo(500, 300);
simulator.Click();
simulator.RightClick();
simulator.Scroll(3);

// 键盘操作
simulator.Type("Hello World");
simulator.Ctrl('C');  // Ctrl+C
simulator.Keyboard.CtrlShiftKey(VirtualKeyCodes.VK_S);  // Ctrl+Shift+S

// 锁屏（直接 API，非模拟）
InputSimulator.LockScreen();
```

## 关键技术点

### 1. Win+L 无法模拟
`Win+L` 是 Windows 受保护的安全快捷键，`SendInput` API 无法模拟。解决方案是直接调用 `LockWorkStation()` API。

### 2. 锁屏状态下 SendInput 无效
这是 Windows 的安全设计，`SendInput` 在锁屏状态下发送的输入会被系统忽略。解决方案：
- 使用 Interception 内核驱动
- 或手动唤醒屏幕后再执行自动化

### 3. Interception 驱动安装

**方式一：自动安装（推荐）**

程序内置了自动安装功能，点击界面右上角的「安装驱动」按钮即可自动完成：
1. 从 GitHub 下载驱动包
2. 解压并执行安装程序
3. 复制 DLL 到程序目录
4. 提示重启电脑

> 注意：自动安装需要管理员权限，程序会提示以管理员身份重启。

**方式二：手动安装**
```bash
# 下载：https://github.com/oblitum/Interception/releases
# 管理员运行：
install-interception.exe /install
# 重启电脑
# 将 interception.dll 复制到程序目录
```

## 界面功能

1. **鼠标测试**：移动、点击、滚轮、拖拽
2. **键盘测试**：文本输入、单键、功能键、组合键
3. **屏幕解锁**：预设账号密码，延迟后自动输入解锁
4. **驱动切换**：右上角下拉框选择驱动
5. **驱动安装**：一键自动下载并安装 Interception 驱动
6. **测试目标区域**：验证输入效果

## 修复的问题

1. **显示密码功能**：从 ToolTip 改为直接切换显示/隐藏
2. **解锁等待时间**：增加唤醒后的等待时间
3. **取消按钮**：添加 CancellationToken 支持取消操作
4. **程序启动崩溃**：修复 ComboBox SelectionChanged 事件导致的无限递归

## 注意事项

1. **SendInput 限制**：无法在锁屏状态下工作，无法模拟 Win+L、Ctrl+Alt+Del 等安全快捷键
2. **Interception 需要安装**：需要管理员权限安装驱动并重启
3. **账号字段**：Windows 锁屏解锁通常只需要密码，账号字段保留以防特殊情况

## 驱动自动安装功能

### InterceptionInstaller 服务

新增的 `InterceptionInstaller` 服务类提供：

- **自动下载**：从 GitHub 下载 Interception 驱动包
- **自动解压**：解压到临时目录
- **自动安装**：执行 `install-interception.exe /install`
- **自动复制 DLL**：将正确架构的 DLL 复制到程序目录
- **进度回调**：实时显示安装进度
- **权限检测**：检测并提示管理员权限
- **重启提示**：安装完成后可选择立即重启

### 使用方式

```csharp
var installer = new InterceptionInstaller();
installer.ProgressChanged += msg => Console.WriteLine(msg);

var result = await installer.InstallAsync();
if (result.NeedRestart)
{
    // 提示用户重启
}
```

## 后续可扩展

1. 添加更多驱动支持（如 DD 虚拟驱动）
2. 录制/回放功能
3. 脚本化操作
4. 真正的 VHF 内核驱动实现（需要编写 KMDF 驱动）
5. 驱动安装的镜像下载源（解决 GitHub 访问问题）
