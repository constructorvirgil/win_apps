using VirtualHidSimulator.Core.Native;
using static VirtualHidSimulator.Core.Native.NativeMethods;

namespace VirtualHidSimulator.Core.Drivers;

/// <summary>
/// SendInput API 驱动实现
/// 使用 Windows SendInput API 进行输入模拟
/// 优点：无需额外安装，系统自带
/// 缺点：无法在锁屏状态下工作
/// </summary>
public class SendInputDriver : IInputDriver
{
    public string Name => "SendInput";
    public string Description => "Windows SendInput API (用户模式，无法在锁屏状态下工作)";
    public bool IsAvailable => true; // SendInput 始终可用
    public bool SupportsLockScreen => false;

    public bool Initialize()
    {
        // SendInput 不需要初始化
        return true;
    }

    #region 鼠标操作

    public void MouseMoveTo(int x, int y)
    {
        var (absX, absY) = NativeMethods.ToAbsoluteCoordinates(x, y);
        var input = CreateMouseInput(absX, absY, 0,
            MouseEventFlags.MOUSEEVENTF_MOVE | MouseEventFlags.MOUSEEVENTF_ABSOLUTE);
        SendInput(1, [input], InputSize);
    }

    public void MouseMoveBy(int deltaX, int deltaY)
    {
        var input = CreateMouseInput(deltaX, deltaY, 0, MouseEventFlags.MOUSEEVENTF_MOVE);
        SendInput(1, [input], InputSize);
    }

    public void MouseButtonDown(MouseButton button)
    {
        var flags = GetMouseDownFlags(button);
        var input = CreateMouseInput(0, 0, GetXButtonData(button), flags);
        SendInput(1, [input], InputSize);
    }

    public void MouseButtonUp(MouseButton button)
    {
        var flags = GetMouseUpFlags(button);
        var input = CreateMouseInput(0, 0, GetXButtonData(button), flags);
        SendInput(1, [input], InputSize);
    }

    public void MouseWheel(int delta, bool horizontal = false)
    {
        var flags = horizontal ? MouseEventFlags.MOUSEEVENTF_HWHEEL : MouseEventFlags.MOUSEEVENTF_WHEEL;
        var input = CreateMouseInput(0, 0, (uint)(delta * 120), flags);
        SendInput(1, [input], InputSize);
    }

    public (int x, int y) GetMousePosition()
    {
        if (GetCursorPos(out POINT point))
        {
            return (point.X, point.Y);
        }
        return (0, 0);
    }

    #endregion

    #region 键盘操作

    public void KeyDown(ushort keyCode)
    {
        var flags = IsExtendedKey(keyCode) ? KeyEventFlags.KEYEVENTF_EXTENDEDKEY : 0;
        var input = CreateKeyboardInput(keyCode, flags);
        SendInput(1, [input], InputSize);
    }

    public void KeyUp(ushort keyCode)
    {
        var flags = KeyEventFlags.KEYEVENTF_KEYUP;
        if (IsExtendedKey(keyCode))
            flags |= KeyEventFlags.KEYEVENTF_EXTENDEDKEY;

        var input = CreateKeyboardInput(keyCode, flags);
        SendInput(1, [input], InputSize);
    }

    public void SendUnicode(char character)
    {
        var inputDown = CreateUnicodeInput(character, keyUp: false);
        var inputUp = CreateUnicodeInput(character, keyUp: true);

        SendInput(1, [inputDown], InputSize);
        SendInput(1, [inputUp], InputSize);
    }

    #endregion

    #region 辅助方法

    private static MouseEventFlags GetMouseDownFlags(MouseButton button) => button switch
    {
        MouseButton.Left => MouseEventFlags.MOUSEEVENTF_LEFTDOWN,
        MouseButton.Right => MouseEventFlags.MOUSEEVENTF_RIGHTDOWN,
        MouseButton.Middle => MouseEventFlags.MOUSEEVENTF_MIDDLEDOWN,
        MouseButton.XButton1 or MouseButton.XButton2 => MouseEventFlags.MOUSEEVENTF_XDOWN,
        _ => MouseEventFlags.MOUSEEVENTF_LEFTDOWN
    };

    private static MouseEventFlags GetMouseUpFlags(MouseButton button) => button switch
    {
        MouseButton.Left => MouseEventFlags.MOUSEEVENTF_LEFTUP,
        MouseButton.Right => MouseEventFlags.MOUSEEVENTF_RIGHTUP,
        MouseButton.Middle => MouseEventFlags.MOUSEEVENTF_MIDDLEUP,
        MouseButton.XButton1 or MouseButton.XButton2 => MouseEventFlags.MOUSEEVENTF_XUP,
        _ => MouseEventFlags.MOUSEEVENTF_LEFTUP
    };

    private static uint GetXButtonData(MouseButton button) => button switch
    {
        MouseButton.XButton1 => 1,
        MouseButton.XButton2 => 2,
        _ => 0
    };

    private static bool IsExtendedKey(ushort keyCode)
    {
        return keyCode is 0x2D or 0x2E or 0x24 or 0x23 or 0x21 or 0x22 // Insert, Delete, Home, End, PageUp, PageDown
            or 0x25 or 0x27 or 0x26 or 0x28 // Arrow keys
            or 0x90 or 0x2C or 0x6F // NumLock, PrintScreen, Divide
            or 0xA3 or 0xA5 or 0x5B or 0x5C or 0x5D; // RControl, RMenu, LWin, RWin, Apps
    }

    #endregion

    public void Dispose()
    {
        // SendInput 不需要清理
    }
}
