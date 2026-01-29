using System.Runtime.InteropServices;

namespace automation.Core.Native;

/// <summary>
/// Windows Native API P/Invoke 封装
/// 提供底层输入模拟所需的 Windows API
/// </summary>
public static class NativeMethods
{
    #region SendInput API

    /// <summary>
    /// 发送输入事件到系统
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    #endregion

    #region 系统功能 API

    /// <summary>
    /// 锁定工作站（锁屏）
    /// 注意：Win+L 是受保护的快捷键，SendInput 无法模拟，需要直接调用此 API
    /// </summary>
    [DllImport("user32.dll")]
    public static extern bool LockWorkStation();

    /// <summary>
    /// 获取系统指标
    /// </summary>
    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    /// <summary>
    /// 获取当前光标位置
    /// </summary>
    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    /// <summary>
    /// 设置光标位置
    /// </summary>
    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);

    /// <summary>
    /// 将虚拟键码转换为扫描码
    /// </summary>
    [DllImport("user32.dll")]
    public static extern uint MapVirtualKey(uint uCode, uint uMapType);

    /// <summary>
    /// 将字符转换为虚拟键码
    /// </summary>
    [DllImport("user32.dll")]
    public static extern short VkKeyScan(char ch);

    #endregion

    #region 系统指标常量

    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;

    #endregion

    #region 输入类型常量

    public const uint INPUT_MOUSE = 0;
    public const uint INPUT_KEYBOARD = 1;
    public const uint INPUT_HARDWARE = 2;

    #endregion

    #region 鼠标事件标志

    [Flags]
    public enum MouseEventFlags : uint
    {
        MOUSEEVENTF_MOVE = 0x0001,
        MOUSEEVENTF_LEFTDOWN = 0x0002,
        MOUSEEVENTF_LEFTUP = 0x0004,
        MOUSEEVENTF_RIGHTDOWN = 0x0008,
        MOUSEEVENTF_RIGHTUP = 0x0010,
        MOUSEEVENTF_MIDDLEDOWN = 0x0020,
        MOUSEEVENTF_MIDDLEUP = 0x0040,
        MOUSEEVENTF_XDOWN = 0x0080,
        MOUSEEVENTF_XUP = 0x0100,
        MOUSEEVENTF_WHEEL = 0x0800,
        MOUSEEVENTF_HWHEEL = 0x1000,
        MOUSEEVENTF_MOVE_NOCOALESCE = 0x2000,
        MOUSEEVENTF_VIRTUALDESK = 0x4000,
        MOUSEEVENTF_ABSOLUTE = 0x8000
    }

    #endregion

    #region 键盘事件标志

    [Flags]
    public enum KeyEventFlags : uint
    {
        KEYEVENTF_EXTENDEDKEY = 0x0001,
        KEYEVENTF_KEYUP = 0x0002,
        KEYEVENTF_UNICODE = 0x0004,
        KEYEVENTF_SCANCODE = 0x0008
    }

    #endregion

    #region 输入结构体

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 获取 INPUT 结构体大小
    /// </summary>
    public static int InputSize => Marshal.SizeOf<INPUT>();

    /// <summary>
    /// 将屏幕坐标转换为绝对坐标（0-65535范围）
    /// </summary>
    public static (int x, int y) ToAbsoluteCoordinates(int screenX, int screenY)
    {
        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);

        int absoluteX = (screenX * 65535) / screenWidth;
        int absoluteY = (screenY * 65535) / screenHeight;

        return (absoluteX, absoluteY);
    }

    /// <summary>
    /// 创建鼠标输入
    /// </summary>
    public static INPUT CreateMouseInput(int dx, int dy, uint mouseData, MouseEventFlags flags)
    {
        return new INPUT
        {
            type = INPUT_MOUSE,
            U = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = dx,
                    dy = dy,
                    mouseData = mouseData,
                    dwFlags = (uint)flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    /// <summary>
    /// 创建键盘输入
    /// </summary>
    public static INPUT CreateKeyboardInput(ushort keyCode, KeyEventFlags flags)
    {
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = keyCode,
                    wScan = 0,
                    dwFlags = (uint)flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    /// <summary>
    /// 创建 Unicode 字符输入
    /// </summary>
    public static INPUT CreateUnicodeInput(char character, bool keyUp = false)
    {
        var flags = KeyEventFlags.KEYEVENTF_UNICODE;
        if (keyUp) flags |= KeyEventFlags.KEYEVENTF_KEYUP;

        return new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = character,
                    dwFlags = (uint)flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    #endregion
}
