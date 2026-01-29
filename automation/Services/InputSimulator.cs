using automation.Core.Drivers;
using automation.Core.HidDefinitions;
using automation.Core.Interfaces;
using automation.Core.Native;
using automation.Devices;

namespace automation.Services;

/// <summary>
/// 输入模拟服务
/// 提供统一的输入模拟 API 入口，包括键盘和鼠标操作
/// </summary>
public class InputSimulator
{
    #region 属性

    /// <summary>
    /// 虚拟键盘实例
    /// </summary>
    public IVirtualKeyboard Keyboard { get; }

    /// <summary>
    /// 虚拟鼠标实例
    /// </summary>
    public IVirtualMouse Mouse { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建输入模拟服务实例
    /// </summary>
    public InputSimulator()
    {
        Keyboard = new VirtualKeyboard();
        Mouse = new VirtualMouse();
    }

    /// <summary>
    /// 使用自定义键盘和鼠标实现创建输入模拟服务
    /// </summary>
    public InputSimulator(IVirtualKeyboard keyboard, IVirtualMouse mouse)
    {
        Keyboard = keyboard ?? throw new ArgumentNullException(nameof(keyboard));
        Mouse = mouse ?? throw new ArgumentNullException(nameof(mouse));
    }

    #endregion

    #region 便捷方法 - 鼠标

    /// <summary>
    /// 移动鼠标到指定位置
    /// </summary>
    public InputSimulator MoveTo(int x, int y)
    {
        Mouse.MoveTo(x, y);
        return this;
    }

    /// <summary>
    /// 相对移动鼠标
    /// </summary>
    public InputSimulator MoveBy(int deltaX, int deltaY)
    {
        Mouse.MoveBy(deltaX, deltaY);
        return this;
    }

    /// <summary>
    /// 左键单击
    /// </summary>
    public InputSimulator Click()
    {
        Mouse.LeftClick();
        return this;
    }

    /// <summary>
    /// 在指定位置左键单击
    /// </summary>
    public InputSimulator ClickAt(int x, int y)
    {
        Mouse.LeftClickAt(x, y);
        return this;
    }

    /// <summary>
    /// 双击
    /// </summary>
    public InputSimulator DoubleClick()
    {
        Mouse.LeftDoubleClick();
        return this;
    }

    /// <summary>
    /// 在指定位置双击
    /// </summary>
    public InputSimulator DoubleClickAt(int x, int y)
    {
        Mouse.DoubleClickAt(x, y);
        return this;
    }

    /// <summary>
    /// 右键单击
    /// </summary>
    public InputSimulator RightClick()
    {
        Mouse.RightClick();
        return this;
    }

    /// <summary>
    /// 在指定位置右键单击
    /// </summary>
    public InputSimulator RightClickAt(int x, int y)
    {
        Mouse.RightClickAt(x, y);
        return this;
    }

    /// <summary>
    /// 滚动鼠标滚轮
    /// </summary>
    public InputSimulator Scroll(int delta)
    {
        Mouse.ScrollVertical(delta);
        return this;
    }

    #endregion

    #region 便捷方法 - 键盘

    /// <summary>
    /// 输入文本
    /// </summary>
    public InputSimulator Type(string text)
    {
        Keyboard.TypeText(text);
        return this;
    }

    /// <summary>
    /// 使用 Unicode 输入文本（支持中文等）
    /// </summary>
    public InputSimulator TypeUnicode(string text)
    {
        Keyboard.TypeUnicode(text);
        return this;
    }

    /// <summary>
    /// 按下单个键
    /// </summary>
    public InputSimulator PressKey(ushort keyCode)
    {
        Keyboard.PressKey(keyCode);
        return this;
    }

    /// <summary>
    /// 按下按键名称对应的键
    /// </summary>
    public InputSimulator PressKey(string keyName)
    {
        var keyCode = VirtualKeyCodes.FromName(keyName);
        if (keyCode != 0)
            Keyboard.PressKey(keyCode);
        return this;
    }

    /// <summary>
    /// 按下组合键
    /// </summary>
    public InputSimulator PressKeys(params ushort[] keyCodes)
    {
        Keyboard.PressKeyCombination(keyCodes);
        return this;
    }

    /// <summary>
    /// Ctrl + 键
    /// </summary>
    public InputSimulator Ctrl(ushort key)
    {
        Keyboard.CtrlKey(key);
        return this;
    }

    /// <summary>
    /// Ctrl + 字符
    /// </summary>
    public InputSimulator Ctrl(char key)
    {
        Keyboard.CtrlKey(VirtualKeyCodes.FromChar(key));
        return this;
    }

    /// <summary>
    /// Alt + 键
    /// </summary>
    public InputSimulator Alt(ushort key)
    {
        Keyboard.AltKey(key);
        return this;
    }

    /// <summary>
    /// Shift + 键
    /// </summary>
    public InputSimulator Shift(ushort key)
    {
        Keyboard.ShiftKey(key);
        return this;
    }

    /// <summary>
    /// Ctrl + Shift + 键
    /// </summary>
    public InputSimulator CtrlShift(ushort key)
    {
        Keyboard.CtrlShiftKey(key);
        return this;
    }

    /// <summary>
    /// Win + 键
    /// </summary>
    public InputSimulator Win(ushort key)
    {
        Keyboard.WinKey(key);
        return this;
    }

    #endregion

    #region 异步方法

    /// <summary>
    /// 平滑移动鼠标
    /// </summary>
    public async Task<InputSimulator> MoveToSmoothAsync(int x, int y, int durationMs = 500)
    {
        await Mouse.MoveToSmoothAsync(x, y, durationMs);
        return this;
    }

    /// <summary>
    /// 拖拽
    /// </summary>
    public async Task<InputSimulator> DragToAsync(int x, int y)
    {
        await Mouse.DragToAsync(x, y);
        return this;
    }

    /// <summary>
    /// 从起点拖拽到终点
    /// </summary>
    public async Task<InputSimulator> DragAsync(int fromX, int fromY, int toX, int toY)
    {
        await Mouse.DragAsync(fromX, fromY, toX, toY);
        return this;
    }

    /// <summary>
    /// 带延迟的输入文本
    /// </summary>
    public async Task<InputSimulator> TypeWithDelayAsync(string text, int delayMs = 50)
    {
        await Keyboard.TypeTextWithDelayAsync(text, delayMs);
        return this;
    }

    #endregion

    #region 延迟方法

    /// <summary>
    /// 等待指定毫秒
    /// </summary>
    public async Task<InputSimulator> DelayAsync(int milliseconds)
    {
        await Task.Delay(milliseconds);
        return this;
    }

    /// <summary>
    /// 同步等待
    /// </summary>
    public InputSimulator Delay(int milliseconds)
    {
        Thread.Sleep(milliseconds);
        return this;
    }

    #endregion

    #region 状态查询

    /// <summary>
    /// 获取当前鼠标位置
    /// </summary>
    public (int x, int y) GetMousePosition()
    {
        return Mouse.GetPosition();
    }

    #endregion

    #region 系统操作

    /// <summary>
    /// 锁定工作站（锁屏）
    /// 注意：Win+L 是受保护的快捷键，SendInput 无法模拟，需要使用此方法
    /// </summary>
    public static bool LockScreen()
    {
        return NativeMethods.LockWorkStation();
    }

    #endregion

    #region 驱动管理

    /// <summary>
    /// 获取当前使用的驱动类型
    /// </summary>
    public static DriverType CurrentDriverType => DriverManager.Instance.CurrentDriverType;

    /// <summary>
    /// 获取当前驱动信息
    /// </summary>
    public static IInputDriver CurrentDriver => DriverManager.Instance.CurrentDriver;

    /// <summary>
    /// 切换驱动
    /// </summary>
    /// <param name="driverType">驱动类型</param>
    /// <returns>是否切换成功</returns>
    public static bool SetDriver(DriverType driverType)
    {
        return DriverManager.Instance.SetDriver(driverType);
    }

    /// <summary>
    /// 获取所有可用的驱动信息
    /// </summary>
    public static IEnumerable<DriverInfo> GetAvailableDrivers()
    {
        return DriverManager.Instance.GetAvailableDrivers();
    }

    /// <summary>
    /// 检查指定驱动是否可用
    /// </summary>
    public static bool IsDriverAvailable(DriverType driverType)
    {
        return DriverManager.Instance.IsDriverAvailable(driverType);
    }

    /// <summary>
    /// 自动选择最佳可用驱动（优先使用支持锁屏的驱动）
    /// </summary>
    public static DriverType UseBestDriver()
    {
        return DriverManager.Instance.UseBestAvailableDriver();
    }

    #endregion

    #region 静态工厂方法

    /// <summary>
    /// 创建默认的输入模拟器实例
    /// </summary>
    public static InputSimulator Create()
    {
        return new InputSimulator();
    }

    /// <summary>
    /// 创建输入模拟器实例并指定驱动
    /// </summary>
    public static InputSimulator Create(DriverType driverType)
    {
        SetDriver(driverType);
        return new InputSimulator();
    }

    #endregion
}
