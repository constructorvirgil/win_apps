namespace automation.Core.Drivers;

/// <summary>
/// 输入驱动接口
/// 定义底层输入模拟的抽象接口，支持不同的驱动实现
/// </summary>
public interface IInputDriver : IDisposable
{
    /// <summary>
    /// 驱动名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 驱动描述
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 驱动是否可用（已安装/已初始化）
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// 是否支持在锁屏状态下工作
    /// </summary>
    bool SupportsLockScreen { get; }

    /// <summary>
    /// 初始化驱动
    /// </summary>
    bool Initialize();

    #region 鼠标操作

    /// <summary>
    /// 移动鼠标到绝对位置
    /// </summary>
    void MouseMoveTo(int x, int y);

    /// <summary>
    /// 相对移动鼠标
    /// </summary>
    void MouseMoveBy(int deltaX, int deltaY);

    /// <summary>
    /// 鼠标按键按下
    /// </summary>
    void MouseButtonDown(MouseButton button);

    /// <summary>
    /// 鼠标按键释放
    /// </summary>
    void MouseButtonUp(MouseButton button);

    /// <summary>
    /// 鼠标滚轮
    /// </summary>
    void MouseWheel(int delta, bool horizontal = false);

    /// <summary>
    /// 获取当前鼠标位置
    /// </summary>
    (int x, int y) GetMousePosition();

    #endregion

    #region 键盘操作

    /// <summary>
    /// 按键按下
    /// </summary>
    void KeyDown(ushort keyCode);

    /// <summary>
    /// 按键释放
    /// </summary>
    void KeyUp(ushort keyCode);

    /// <summary>
    /// 输入 Unicode 字符
    /// </summary>
    void SendUnicode(char character);

    #endregion
}

/// <summary>
/// 鼠标按键枚举
/// </summary>
public enum MouseButton
{
    Left,
    Right,
    Middle,
    XButton1,
    XButton2
}
