using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using automation.Core.Drivers;
using automation.Core.HidDefinitions;
using automation.Services;

namespace automation;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// 虚拟 HID 输入模拟测试界面
/// </summary>
public partial class MainWindow : Window
{
    private readonly InputSimulator _simulator;

    public MainWindow()
    {
        InitializeComponent();
        _simulator = InputSimulator.Create();
        
        // 初始化驱动选择
        InitializeDriverSelection();
        
        UpdateStatus("输入模拟器已初始化");
    }

    #region 驱动选择

    private bool _isRefreshingDriverList = false;

    private void InitializeDriverSelection()
    {
        RefreshDriverList();
    }

    private void RefreshDriverList()
    {
        if (_isRefreshingDriverList) return;
        
        _isRefreshingDriverList = true;
        try
        {
            var drivers = InputSimulator.GetAvailableDrivers().Select(d => new DriverDisplayItem
            {
                Type = d.Type,
                Name = d.Name,
                Description = d.Description,
                IsAvailable = d.IsAvailable,
                SupportsLockScreen = d.SupportsLockScreen,
                IsCurrent = d.IsCurrent
            }).ToList();

            DriverComboBox.ItemsSource = drivers;
            
            // 选择当前驱动
            var currentDriver = drivers.FirstOrDefault(d => d.IsCurrent);
            if (currentDriver != null)
            {
                DriverComboBox.SelectedItem = currentDriver;
            }
        }
        finally
        {
            _isRefreshingDriverList = false;
        }
    }

    private void DriverComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 防止递归调用
        if (_isRefreshingDriverList) return;
        
        if (DriverComboBox.SelectedItem is DriverDisplayItem item)
        {
            // 如果已经是当前驱动，不做处理
            if (item.IsCurrent) return;
            
            if (!item.IsAvailable)
            {
                MessageBox.Show(
                    $"驱动 '{item.Name}' 不可用。\n\n{GetDriverInstallHelp(item.Type)}",
                    "驱动不可用",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                
                // 恢复到当前驱动
                RefreshDriverList();
                return;
            }

            bool success = InputSimulator.SetDriver(item.Type);
            if (success)
            {
                UpdateStatus($"已切换到 {item.Name} 驱动");
                RefreshDriverList();
            }
            else
            {
                MessageBox.Show($"切换到 {item.Name} 驱动失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                RefreshDriverList();
            }
        }
    }

    private void DriverHelp_Click(object sender, RoutedEventArgs e)
    {
        string helpText = @"=== 驱动说明 ===

【SendInput】(默认)
- Windows 内置 API，无需安装
- 用户模式，无法在锁屏状态下工作
- 适用于：普通自动化场景

【Interception】(推荐用于锁屏解锁)
- 内核模式驱动，可绕过系统安全限制
- 支持在锁屏状态下模拟输入
- 需要安装驱动程序

=== Interception 安装方法 ===

1. 下载驱动：
   https://github.com/oblitum/Interception/releases

2. 以管理员身份运行命令：
   install-interception.exe /install

3. 重启电脑

4. 将 interception.dll 复制到程序目录

=== 注意事项 ===
- Interception 驱动需要管理员权限安装
- 安装后需要重启电脑才能生效
- 部分杀毒软件可能会拦截内核驱动";

        MessageBox.Show(helpText, "驱动帮助", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string GetDriverInstallHelp(DriverType type)
    {
        return type switch
        {
            DriverType.Interception => 
                "Interception 驱动需要安装：\n" +
                "1. 下载：https://github.com/oblitum/Interception/releases\n" +
                "2. 管理员运行：install-interception.exe /install\n" +
                "3. 重启电脑\n" +
                "4. 将 interception.dll 复制到程序目录",
            _ => "该驱动不可用"
        };
    }

    #endregion

    #region 状态更新

    private void UpdateStatus(string message)
    {
        StatusText.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
    }

    #endregion

    #region 鼠标 - 位置

    private void RefreshPosition_Click(object sender, RoutedEventArgs e)
    {
        var (x, y) = _simulator.GetMousePosition();
        CurrentXTextBox.Text = x.ToString();
        CurrentYTextBox.Text = y.ToString();
        UpdateStatus($"当前鼠标位置: ({x}, {y})");
    }

    #endregion

    #region 鼠标 - 移动

    private void MoveTo_Click(object sender, RoutedEventArgs e)
    {
        if (TryParseCoordinates(MoveXTextBox, MoveYTextBox, out int x, out int y))
        {
            _simulator.MoveTo(x, y);
            UpdateStatus($"鼠标已移动到 ({x}, {y})");
        }
    }

    private async void MoveToSmooth_Click(object sender, RoutedEventArgs e)
    {
        if (TryParseCoordinates(MoveXTextBox, MoveYTextBox, out int x, out int y))
        {
            UpdateStatus($"正在平滑移动到 ({x}, {y})...");
            await _simulator.MoveToSmoothAsync(x, y, 500);
            UpdateStatus($"鼠标已平滑移动到 ({x}, {y})");
        }
    }

    private void MoveBy_Click(object sender, RoutedEventArgs e)
    {
        if (TryParseCoordinates(DeltaXTextBox, DeltaYTextBox, out int dx, out int dy))
        {
            _simulator.MoveBy(dx, dy);
            UpdateStatus($"鼠标已相对移动 ({dx}, {dy})");
        }
    }

    #endregion

    #region 鼠标 - 点击

    private void LeftClick_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Click();
        UpdateStatus("执行左键单击");
    }

    private void DoubleClick_Click(object sender, RoutedEventArgs e)
    {
        _simulator.DoubleClick();
        UpdateStatus("执行左键双击");
    }

    private void RightClick_Click(object sender, RoutedEventArgs e)
    {
        _simulator.RightClick();
        UpdateStatus("执行右键单击");
    }

    private void MiddleClick_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Mouse.MiddleClick();
        UpdateStatus("执行中键单击");
    }

    #endregion

    #region 鼠标 - 滚轮

    private void ScrollUp_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(ScrollDeltaTextBox.Text, out int delta))
        {
            _simulator.Scroll(delta);
            UpdateStatus($"向上滚动 {delta} 格");
        }
    }

    private void ScrollDown_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(ScrollDeltaTextBox.Text, out int delta))
        {
            _simulator.Scroll(-delta);
            UpdateStatus($"向下滚动 {delta} 格");
        }
    }

    #endregion

    #region 鼠标 - 拖拽

    private async void Drag_Click(object sender, RoutedEventArgs e)
    {
        if (TryParseCoordinates(DragFromXTextBox, DragFromYTextBox, out int fromX, out int fromY) &&
            TryParseCoordinates(DragToXTextBox, DragToYTextBox, out int toX, out int toY))
        {
            UpdateStatus($"正在从 ({fromX}, {fromY}) 拖拽到 ({toX}, {toY})...");
            await _simulator.DragAsync(fromX, fromY, toX, toY);
            UpdateStatus($"拖拽完成");
        }
    }

    #endregion

    #region 键盘 - 文本输入

    private void TypeText_Click(object sender, RoutedEventArgs e)
    {
        string text = InputTextBox.Text;
        if (!string.IsNullOrEmpty(text))
        {
            _simulator.Type(text);
            UpdateStatus($"已输入文本: {text}");
        }
    }

    private void TypeUnicode_Click(object sender, RoutedEventArgs e)
    {
        string text = InputTextBox.Text;
        if (!string.IsNullOrEmpty(text))
        {
            _simulator.TypeUnicode(text);
            UpdateStatus($"已通过Unicode输入文本: {text}");
        }
    }

    private async void TypeWithDelay_Click(object sender, RoutedEventArgs e)
    {
        string text = InputTextBox.Text;
        if (!string.IsNullOrEmpty(text))
        {
            UpdateStatus($"正在输入文本...");
            await _simulator.TypeWithDelayAsync(text, 100);
            UpdateStatus($"已完成延迟输入: {text}");
        }
    }

    #endregion

    #region 键盘 - 单键

    private void PressEnter_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Keyboard.Enter();
        UpdateStatus("按下 Enter");
    }

    private void PressTab_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Keyboard.Tab();
        UpdateStatus("按下 Tab");
    }

    private void PressEscape_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Keyboard.Escape();
        UpdateStatus("按下 Escape");
    }

    private void PressBackspace_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Keyboard.Backspace();
        UpdateStatus("按下 Backspace");
    }

    private void PressDelete_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Keyboard.Delete();
        UpdateStatus("按下 Delete");
    }

    private void PressSpace_Click(object sender, RoutedEventArgs e)
    {
        _simulator.PressKey(VirtualKeyCodes.VK_SPACE);
        UpdateStatus("按下 空格");
    }

    private void PressUp_Click(object sender, RoutedEventArgs e)
    {
        _simulator.PressKey(VirtualKeyCodes.VK_UP);
        UpdateStatus("按下 ↑");
    }

    private void PressDown_Click(object sender, RoutedEventArgs e)
    {
        _simulator.PressKey(VirtualKeyCodes.VK_DOWN);
        UpdateStatus("按下 ↓");
    }

    private void PressLeft_Click(object sender, RoutedEventArgs e)
    {
        _simulator.PressKey(VirtualKeyCodes.VK_LEFT);
        UpdateStatus("按下 ←");
    }

    private void PressRight_Click(object sender, RoutedEventArgs e)
    {
        _simulator.PressKey(VirtualKeyCodes.VK_RIGHT);
        UpdateStatus("按下 →");
    }

    #endregion

    #region 键盘 - 功能键

    private void PressF1_Click(object sender, RoutedEventArgs e) => PressFunction(1);
    private void PressF2_Click(object sender, RoutedEventArgs e) => PressFunction(2);
    private void PressF3_Click(object sender, RoutedEventArgs e) => PressFunction(3);
    private void PressF4_Click(object sender, RoutedEventArgs e) => PressFunction(4);
    private void PressF5_Click(object sender, RoutedEventArgs e) => PressFunction(5);
    private void PressF6_Click(object sender, RoutedEventArgs e) => PressFunction(6);
    private void PressF7_Click(object sender, RoutedEventArgs e) => PressFunction(7);
    private void PressF8_Click(object sender, RoutedEventArgs e) => PressFunction(8);
    private void PressF9_Click(object sender, RoutedEventArgs e) => PressFunction(9);
    private void PressF10_Click(object sender, RoutedEventArgs e) => PressFunction(10);
    private void PressF11_Click(object sender, RoutedEventArgs e) => PressFunction(11);
    private void PressF12_Click(object sender, RoutedEventArgs e) => PressFunction(12);

    private void PressFunction(int number)
    {
        ushort keyCode = (ushort)(VirtualKeyCodes.VK_F1 + number - 1);
        _simulator.PressKey(keyCode);
        UpdateStatus($"按下 F{number}");
    }

    #endregion

    #region 键盘 - 组合键

    private void CtrlC_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Keyboard.Copy();
        UpdateStatus("按下 Ctrl+C (复制)");
    }

    private void CtrlV_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Keyboard.Paste();
        UpdateStatus("按下 Ctrl+V (粘贴)");
    }

    private void CtrlX_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Keyboard.Cut();
        UpdateStatus("按下 Ctrl+X (剪切)");
    }

    private void CtrlA_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Keyboard.SelectAll();
        UpdateStatus("按下 Ctrl+A (全选)");
    }

    private void CtrlZ_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Keyboard.Undo();
        UpdateStatus("按下 Ctrl+Z (撤销)");
    }

    private void CtrlY_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Keyboard.Redo();
        UpdateStatus("按下 Ctrl+Y (重做)");
    }

    private void CtrlS_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Keyboard.Save();
        UpdateStatus("按下 Ctrl+S (保存)");
    }

    private void CtrlF_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Keyboard.Find();
        UpdateStatus("按下 Ctrl+F (查找)");
    }

    private void AltF4_Click(object sender, RoutedEventArgs e)
    {
        // 警告：这会关闭当前窗口
        _simulator.Alt(VirtualKeyCodes.VK_F4);
        UpdateStatus("按下 Alt+F4 (注意：这可能关闭当前活动窗口)");
    }

    private void AltTab_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Alt(VirtualKeyCodes.VK_TAB);
        UpdateStatus("按下 Alt+Tab");
    }

    private void WinD_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Win(VirtualKeyCodes.VK_D);
        UpdateStatus("按下 Win+D (显示桌面)");
    }

    private void WinE_Click(object sender, RoutedEventArgs e)
    {
        _simulator.Win(VirtualKeyCodes.VK_E);
        UpdateStatus("按下 Win+E (打开资源管理器)");
    }

    private void LockScreen_Click(object sender, RoutedEventArgs e)
    {
        // Win+L 是受保护的快捷键，SendInput 无法模拟
        // 需要直接调用 LockWorkStation API
        bool result = InputSimulator.LockScreen();
        UpdateStatus(result ? "已锁定工作站" : "锁屏失败");
    }

    #endregion

    #region 屏幕解锁

    private CancellationTokenSource? _unlockCancellation;

    private void ShowPassword_Checked(object sender, RoutedEventArgs e)
    {
        // 切换到明文显示
        UnlockPasswordTextBox.Text = UnlockPasswordBox.Password;
        UnlockPasswordBox.Visibility = Visibility.Collapsed;
        UnlockPasswordTextBox.Visibility = Visibility.Visible;
    }

    private void ShowPassword_Unchecked(object sender, RoutedEventArgs e)
    {
        // 切换到密码显示
        UnlockPasswordBox.Password = UnlockPasswordTextBox.Text;
        UnlockPasswordTextBox.Visibility = Visibility.Collapsed;
        UnlockPasswordBox.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// 获取当前密码（兼容两种显示模式）
    /// </summary>
    private string GetUnlockPassword()
    {
        return ShowPasswordCheckBox.IsChecked == true 
            ? UnlockPasswordTextBox.Text 
            : UnlockPasswordBox.Password;
    }

    private void SetUnlockButtonsState(bool isRunning)
    {
        UnlockButton.IsEnabled = !isRunning;
        CancelUnlockButton.IsEnabled = isRunning;
    }

    private void CancelUnlock_Click(object sender, RoutedEventArgs e)
    {
        _unlockCancellation?.Cancel();
        UpdateStatus("已取消解锁操作");
        SetUnlockButtonsState(false);
    }

    private async void UnlockScreen_Click(object sender, RoutedEventArgs e)
    {
        // 获取密码
        string password = GetUnlockPassword();
        if (string.IsNullOrEmpty(password))
        {
            UpdateStatus("请输入密码");
            return;
        }

        // 获取延迟时间
        if (!int.TryParse(UnlockDelayTextBox.Text, out int delaySec) || delaySec < 0)
        {
            delaySec = 2;
        }

        // 创建取消令牌
        _unlockCancellation?.Cancel();
        _unlockCancellation = new CancellationTokenSource();
        var token = _unlockCancellation.Token;

        SetUnlockButtonsState(true);

        try
        {
            // 倒计时显示
            for (int i = delaySec; i > 0; i--)
            {
                UpdateStatus($"将在 {i} 秒后开始输入密码... (点击取消可中止)");
                await Task.Delay(1000, token);
            }

            // 执行解锁（只输入密码，不尝试唤醒）
            await PerformUnlockAsync(password, token);
        }
        catch (OperationCanceledException)
        {
            UpdateStatus("解锁操作已取消");
        }
        finally
        {
            SetUnlockButtonsState(false);
        }
    }

    private async void LockAndUnlock_Click(object sender, RoutedEventArgs e)
    {
        // 获取密码
        string password = GetUnlockPassword();
        if (string.IsNullOrEmpty(password))
        {
            UpdateStatus("请输入密码");
            return;
        }

        // 获取延迟时间
        if (!int.TryParse(UnlockDelayTextBox.Text, out int delaySec) || delaySec < 0)
        {
            delaySec = 2;
        }

        // 创建取消令牌
        _unlockCancellation?.Cancel();
        _unlockCancellation = new CancellationTokenSource();
        var token = _unlockCancellation.Token;

        SetUnlockButtonsState(true);

        try
        {
            // 先锁屏
            UpdateStatus("正在锁定屏幕...");
            InputSimulator.LockScreen();

            // 等待锁屏完成
            await Task.Delay(1000, token);

            // 倒计时显示
            for (int i = delaySec; i > 0; i--)
            {
                UpdateStatus($"屏幕已锁定，将在 {i} 秒后开始输入密码... (请先手动唤醒屏幕!)");
                await Task.Delay(1000, token);
            }

            // 执行解锁
            await PerformUnlockAsync(password, token);
        }
        catch (OperationCanceledException)
        {
            UpdateStatus("解锁操作已取消");
        }
        finally
        {
            SetUnlockButtonsState(false);
        }
    }

    private async Task PerformUnlockAsync(string password, CancellationToken token)
    {
        // 注意：由于 Windows 安全限制，SendInput 无法在锁屏状态下唤醒屏幕
        // 此功能假设用户已经手动唤醒屏幕并显示了密码输入框

        UpdateStatus("正在尝试进入密码输入状态...");

        // 步骤1：尝试激活密码输入（如果屏幕已唤醒）
        _simulator.PressKey(VirtualKeyCodes.VK_ESCAPE);
        await Task.Delay(500, token);
        
        _simulator.PressKey(VirtualKeyCodes.VK_RETURN);
        await Task.Delay(1000, token);

        token.ThrowIfCancellationRequested();

        UpdateStatus("正在输入密码...");

        // 步骤2：输入密码（逐字符输入，便于观察）
        foreach (char c in password)
        {
            token.ThrowIfCancellationRequested();
            _simulator.Keyboard.TypeChar(c);
            await Task.Delay(50, token);
        }

        await Task.Delay(300, token);

        // 步骤3：按 Enter 确认
        UpdateStatus("正在确认...");
        _simulator.Keyboard.Enter();

        UpdateStatus("解锁操作已完成（仅执行一次，无循环）");
    }

    #endregion

    #region 键盘 - 自定义组合键

    private void CustomCombo_Click(object sender, RoutedEventArgs e)
    {
        string combo = CustomComboTextBox.Text.Trim();
        if (string.IsNullOrEmpty(combo))
        {
            UpdateStatus("请输入组合键");
            return;
        }

        try
        {
            var keys = ParseKeyCombo(combo);
            if (keys.Count > 0)
            {
                _simulator.Keyboard.PressKeyCombination(keys.ToArray());
                UpdateStatus($"执行组合键: {combo}");
            }
            else
            {
                UpdateStatus($"无法解析组合键: {combo}");
            }
        }
        catch (Exception ex)
        {
            UpdateStatus($"组合键执行失败: {ex.Message}");
        }
    }

    private List<ushort> ParseKeyCombo(string combo)
    {
        var keys = new List<ushort>();
        var parts = combo.Split('+', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            string key = part.Trim().ToUpper();
            ushort keyCode = key switch
            {
                "CTRL" or "CONTROL" => VirtualKeyCodes.VK_CONTROL,
                "SHIFT" => VirtualKeyCodes.VK_SHIFT,
                "ALT" => VirtualKeyCodes.VK_MENU,
                "WIN" or "WINDOWS" => VirtualKeyCodes.VK_LWIN,
                _ => VirtualKeyCodes.FromName(key)
            };

            if (keyCode != 0)
            {
                keys.Add(keyCode);
            }
        }

        return keys;
    }

    #endregion

    #region 测试区域事件

    private void TestCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(TestCanvas);
        string button = e.ChangedButton.ToString();
        CanvasInfoText.Text = $"鼠标按下: {button} 位置: ({pos.X:F0}, {pos.Y:F0})";
    }

    private void TestCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(TestCanvas);
        CanvasInfoText.Text = $"鼠标移动: ({pos.X:F0}, {pos.Y:F0})";
    }

    #endregion

    #region 辅助方法

    private bool TryParseCoordinates(TextBox xBox, TextBox yBox, out int x, out int y)
    {
        x = y = 0;
        if (int.TryParse(xBox.Text, out x) && int.TryParse(yBox.Text, out y))
        {
            return true;
        }
        UpdateStatus("请输入有效的坐标值");
        return false;
    }

    #endregion
}

/// <summary>
/// 驱动显示项（用于 ComboBox 绑定）
/// </summary>
public class DriverDisplayItem
{
    public DriverType Type { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsAvailable { get; set; }
    public bool SupportsLockScreen { get; set; }
    public bool IsCurrent { get; set; }

    public string StatusText => IsAvailable 
        ? (SupportsLockScreen ? "可用 (支持锁屏)" : "可用") 
        : "未安装";
    
    public Brush StatusColor => IsAvailable 
        ? (SupportsLockScreen ? Brushes.Green : Brushes.DarkGreen)
        : Brushes.Gray;
}
