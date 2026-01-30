# SendInput vs “Inception”(Interception) 在本工程中的区别与底层实现梳理

> 说明：仓库代码中没有出现 `Inception` 相关实现或引用；实际存在的是 **Interception**（键鼠内核驱动方案）。下文将按“SendInput vs Interception”进行对比，并在关键处标注工程内的具体落点。

## 1. 工程内的输入架构（从上到下）

- 入口/门面：`src/VirtualHidSimulator.Core/Services/InputSimulator.cs`
  - 对外暴露 `InputSimulator.Create(...)`、`SetDriver(...)`、`UseBestDriver()` 等静态方法。
- 设备抽象：
  - `src/VirtualHidSimulator.Core/Devices/VirtualKeyboard.cs`：键盘动作（单键、组合键、文本输入、Unicode 输入等）
  - `src/VirtualHidSimulator.Core/Devices/VirtualMouse.cs`：鼠标动作（移动、点击、滚轮、拖拽等）
- 驱动抽象：`src/VirtualHidSimulator.Core/Core/Drivers/IInputDriver.cs`
  - 统一了键鼠能力：`MouseMoveTo/By`、`MouseButtonDown/Up`、`MouseWheel`、`KeyDown/Up`、`SendUnicode`
  - 用 `SupportsLockScreen` 区分是否支持锁屏/安全桌面场景
- 驱动选择/切换：`src/VirtualHidSimulator.Core/Core/Drivers/DriverManager.cs`
  - 注册两种驱动：`SendInputDriver`、`InterceptionDriver`
  - 默认：`SendInput`
  - “最佳驱动”：优先 `Interception`，失败回退 `SendInput`

## 2. 两种方案的本质区别（概念层）

### 2.1 SendInput（用户态注入）

- 本质：调用 `user32.dll!SendInput`，把“模拟输入事件”放入系统输入流（用户态 API）。
- 典型限制：
  - **锁屏/安全桌面**：用户态注入无法在锁屏界面可靠工作（工程内也明确标注 `SupportsLockScreen=false`）。
  - **受保护快捷键**：例如 Win+L；工程里直接提示 SendInput 不能模拟 Win+L，改用 `LockWorkStation()`（见 `src/VirtualHidSimulator.Core/Core/Native/NativeMethods.cs` 与 `src/VirtualHidSimulator.Core/Services/InputSimulator.cs`）。

### 2.2 Interception（内核驱动 + 用户态 DLL）

- 本质：安装 **Interception 内核驱动**，并通过 `interception.dll` 提供的 C API 与驱动交互，把键鼠事件以更“底层”的方式发送到设备栈。
- 对应项目：
  - 项目主页（源码仓库）：`https://github.com/oblitum/Interception`
  - Releases（本工程下载/安装来源）：`https://github.com/oblitum/Interception/releases`
- 工程里对应：
  - 运行时 P/Invoke：`src/VirtualHidSimulator.Core/Core/Drivers/InterceptionDriver.cs`（`DllImport("interception.dll")`）
  - 自动安装/下载：`src/VirtualHidSimulator.Core/Services/InterceptionInstaller.cs`（从 GitHub 下载 zip、解压、运行 `install-interception.exe /install`、复制 `interception.dll`）
- 典型特性：
  - **更可能在锁屏状态下工作**（工程标注 `SupportsLockScreen=true`）
  - 但需要安装驱动/管理员权限/重启，并且依赖 `interception.dll` 在程序目录可用

## 3. 本工程中 SendInput 的底层实现细节

实现文件：`src/VirtualHidSimulator.Core/Core/Drivers/SendInputDriver.cs`、`src/VirtualHidSimulator.Core/Core/Native/NativeMethods.cs`

### 3.1 P/Invoke 与结构体

`src/VirtualHidSimulator.Core/Core/Native/NativeMethods.cs` 中定义了：

- `SendInput(uint nInputs, INPUT[] pInputs, int cbSize)`
- `INPUT` + `InputUnion`（显式布局 union）
  - `MOUSEINPUT`：`dx/dy/mouseData/dwFlags/time/dwExtraInfo`
  - `KEYBDINPUT`：`wVk/wScan/dwFlags/time/dwExtraInfo`
- 关键 flag：
  - 鼠标：`MOUSEEVENTF_MOVE / ... / MOUSEEVENTF_ABSOLUTE`
  - 键盘：`KEYEVENTF_KEYUP / KEYEVENTF_EXTENDEDKEY / KEYEVENTF_UNICODE / KEYEVENTF_SCANCODE`

### 3.2 鼠标（绝对/相对坐标）

- 绝对移动：`MouseMoveTo(x,y)`
  - 调用 `NativeMethods.ToAbsoluteCoordinates()` 把屏幕坐标映射到 `0..65535`
  - 发送 `MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE`
- 相对移动：`MouseMoveBy(dx,dy)` 直接 `MOUSEEVENTF_MOVE`
- 滚轮：`MouseWheel(delta)` 内部乘以 `120`（滚轮刻度单位）

### 3.3 键盘（VK 与扩展键）

工程实现采用 **Virtual-Key (VK)** 方式：

- `KeyDown/KeyUp`：使用 `CreateKeyboardInput(keyCode, flags)`
  - `IsExtendedKey(...)` 为部分按键加 `KEYEVENTF_EXTENDEDKEY`（如方向键、Insert/Delete、Win、RControl/RAlt 等）

### 3.4 Unicode 输入（工程里是 SendInput 的强项）

- `SendInputDriver.SendUnicode(char)`
  - 通过 `CreateUnicodeInput(character, keyUp: false/true)` 发送一对按下/抬起事件
  - 底层是 `KEYEVENTF_UNICODE` + `wVk=0` + `wScan=character`

## 4. 本工程中 Interception 的底层实现细节

实现文件：`src/VirtualHidSimulator.Core/Core/Drivers/InterceptionDriver.cs`

### 4.1 Interception DLL 的 C API（P/Invoke）

工程里调用了以下 native 函数（cdecl）：

- `interception_create_context()` / `interception_destroy_context(context)`
- `interception_send(context, device, ref KeyStroke, n)` / `interception_send(context, device, ref MouseStroke, n)`
- `interception_is_keyboard(device)` / `interception_is_mouse(device)`

对应的结构体：

- `KeyStroke { ushort code; ushort state; uint information; }`
- `MouseStroke { ushort state; ushort flags; short rolling; int x; int y; uint information; }`

### 4.2 设备选择策略（工程实现）

- 常量：
  - 键盘设备起始：`INTERCEPTION_KEYBOARD_FIRST = 1`
  - 鼠标设备起始：`INTERCEPTION_MOUSE_FIRST = 11`
- `Initialize()`：
  - `interception_create_context()` 成功后，从起始编号向后最多扫描 10 个设备，取第一个 `interception_is_keyboard(...) != 0` / `interception_is_mouse(...) != 0`
- 可用性判定：
  - 捕获 `DllNotFoundException`：说明 `interception.dll` 不在加载路径，`IsAvailable=false`

### 4.3 鼠标事件（与 SendInput 的差异点）

- 绝对移动：`MouseMoveTo(x,y)`
  - 依旧使用 `0..65535` 坐标，但映射逻辑在 `InterceptionDriver` 内部自行计算
  - 通过 `MouseStroke.flags = INTERCEPTION_MOUSE_MOVE_ABSOLUTE`，并 `interception_send(...)`
- 相对移动：`INTERCEPTION_MOUSE_MOVE_RELATIVE`
- 按键：通过 `MouseStroke.state` 选择 `LEFT/RIGHT/MIDDLE/X1/X2` 的 down/up 常量
- 滚轮：通过 `MouseStroke.rolling = (short)(delta * 120)`，并设置 `MouseStroke.state = INTERCEPTION_MOUSE_WHEEL/HWHEEL`
- 取位置：Interception 本身不提供“读当前位置”，工程直接回退到 `GetCursorPos`（user32）

### 4.4 键盘事件：Scan Code + 扩展键（E0）

Interception 发送的是 **Scan Code**：

- `VirtualKeyToScanCode(vk)`：调用 `user32!MapVirtualKey(vk, 0)`（MAPVK_VK_TO_VSC）
- `KeyDown/KeyUp`：
  - `KeyStroke.code = scanCode`
  - `KeyStroke.state = INTERCEPTION_KEY_DOWN / INTERCEPTION_KEY_UP`
  - 如果 `IsExtendedKey(vk)`，则额外 OR 上 `INTERCEPTION_KEY_E0`

### 4.5 Unicode 输入：Interception 的“短板”和本工程的回退策略

Interception **不直接支持 Unicode 注入**，工程实现为：

1. 尝试 `VkKeyScan(character)` 映射到 VK（并处理是否需要 Shift）
2. 若映射失败（返回 `0xFFFF`），则 **回退到 SendInput 的 `KEYEVENTF_UNICODE**`：
  - `NativeMethods.CreateUnicodeInput(...)`
  - `NativeMethods.SendInput(...)`

重要影响：

- 即使当前驱动选了 Interception，只要遇到无法 `VkKeyScan` 的字符（例如部分复杂/非布局字符），最终仍会走 SendInput；
  - 这意味着在锁屏/安全桌面等场景下，这部分字符输入仍可能失败或不一致。

## 5. 对比总结（工程视角）


| 维度         | SendInput                  | Interception                             |
| ---------- | -------------------------- | ---------------------------------------- |
| 层级         | 用户态 `user32!SendInput`     | 内核驱动 + 用户态 `interception.dll`            |
| 安装依赖       | 无                          | 需要安装驱动、`interception.dll`                |
| 管理员/重启     | 不需要                        | 安装通常需要管理员，工程提示安装后需重启                     |
| 锁屏支持       | `SupportsLockScreen=false` | `SupportsLockScreen=true`（工程目标）          |
| 键盘编码       | VK（部分加 EXTENDED）           | Scan Code + E0 扩展标记                      |
| Unicode 支持 | 直接 `KEYEVENTF_UNICODE`     | 仅能 VK 映射；失败回退 SendInput                  |
| 获取鼠标位置     | `GetCursorPos`             | 工程同样用 `GetCursorPos`（Interception 不提供读取） |


## 6. 工程内的选型与运行时行为

### 6.1 默认/推荐策略

- 默认：`DriverManager` 启动时 `SetDriver(SendInput)`
- “最佳驱动”：`UseBestAvailableDriver()` 优先切到 `Interception`，失败回退 `SendInput`

### 6.2 UI/安装流程（InterceptionInstaller）

实现文件：`src/VirtualHidSimulator.Core/Services/InterceptionInstaller.cs`

- 对应上游项目：
  - `https://github.com/oblitum/Interception`
- 下载：固定 URL `.../v1.0.1/Interception.zip`
- 解压：`ZipFile.ExtractToDirectory(...)`
- 查找：
  - `install-interception.exe`（用于 `/install` 与 `/uninstall`）
  - `interception.dll`（优先匹配当前进程架构 `x64/x86` 路径）
- 执行：
  - 安装：运行 `install-interception.exe /install`（等待最多 60s）
  - 卸载：运行 `install-interception.exe /uninstall`
- 复制 DLL：将 `interception.dll` 复制到 `AppDomain.CurrentDomain.BaseDirectory`
- 权限：安装/卸载前会检查 `IsRunningAsAdmin()`

## 7. 实践建议与注意事项（结合当前代码）

- 文本输入若强依赖“完整 Unicode”（尤其是锁屏场景），需要特别关注 `InterceptionDriver.SendUnicode` 的回退路径：无法映射时会退回 `SendInput`。
- `DriverManager.GetAvailableDrivers()` / `IsDriverAvailable()` 会调用 `driver.Initialize()`；对 `InterceptionDriver` 来说可能导致重复创建 context（当前实现没有在 `Initialize()` 前主动释放旧 context），建议在排查资源/稳定性问题时关注这一点。
