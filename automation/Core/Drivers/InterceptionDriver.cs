using System.Runtime.InteropServices;
using automation.Core.Native;

namespace automation.Core.Drivers;

/// <summary>
/// Interception 驱动实现
/// 使用 Interception 内核驱动进行输入模拟
/// 优点：内核级别，可在锁屏状态下工作
/// 缺点：需要安装 Interception 驱动
/// 
/// 驱动下载：https://github.com/oblitum/Interception/releases
/// 安装方法：以管理员身份运行 install-interception.exe /install
/// </summary>
public class InterceptionDriver : IInputDriver
{
    #region Interception Native API

    private const string DllName = "interception.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr interception_create_context();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void interception_destroy_context(IntPtr context);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int interception_send(IntPtr context, int device, ref KeyStroke stroke, uint nstroke);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int interception_send(IntPtr context, int device, ref MouseStroke stroke, uint nstroke);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int interception_is_keyboard(int device);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int interception_is_mouse(int device);

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyStroke
    {
        public ushort code;
        public ushort state;
        public uint information;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseStroke
    {
        public ushort state;
        public ushort flags;
        public short rolling;
        public int x;
        public int y;
        public uint information;
    }

    // Key states
    private const ushort INTERCEPTION_KEY_DOWN = 0x00;
    private const ushort INTERCEPTION_KEY_UP = 0x01;
    private const ushort INTERCEPTION_KEY_E0 = 0x02;
    private const ushort INTERCEPTION_KEY_E1 = 0x04;

    // Mouse states
    private const ushort INTERCEPTION_MOUSE_LEFT_BUTTON_DOWN = 0x001;
    private const ushort INTERCEPTION_MOUSE_LEFT_BUTTON_UP = 0x002;
    private const ushort INTERCEPTION_MOUSE_RIGHT_BUTTON_DOWN = 0x004;
    private const ushort INTERCEPTION_MOUSE_RIGHT_BUTTON_UP = 0x008;
    private const ushort INTERCEPTION_MOUSE_MIDDLE_BUTTON_DOWN = 0x010;
    private const ushort INTERCEPTION_MOUSE_MIDDLE_BUTTON_UP = 0x020;
    private const ushort INTERCEPTION_MOUSE_BUTTON_4_DOWN = 0x040;
    private const ushort INTERCEPTION_MOUSE_BUTTON_4_UP = 0x080;
    private const ushort INTERCEPTION_MOUSE_BUTTON_5_DOWN = 0x100;
    private const ushort INTERCEPTION_MOUSE_BUTTON_5_UP = 0x200;
    private const ushort INTERCEPTION_MOUSE_WHEEL = 0x400;
    private const ushort INTERCEPTION_MOUSE_HWHEEL = 0x800;

    // Mouse flags
    private const ushort INTERCEPTION_MOUSE_MOVE_RELATIVE = 0x000;
    private const ushort INTERCEPTION_MOUSE_MOVE_ABSOLUTE = 0x001;

    // Device constants
    private const int INTERCEPTION_KEYBOARD_FIRST = 1;
    private const int INTERCEPTION_MOUSE_FIRST = 11;

    #endregion

    private IntPtr _context = IntPtr.Zero;
    private bool _isAvailable;
    private int _keyboardDevice = INTERCEPTION_KEYBOARD_FIRST;
    private int _mouseDevice = INTERCEPTION_MOUSE_FIRST;

    public string Name => "Interception";
    public string Description => "Interception 内核驱动 (支持锁屏状态，需安装驱动)";
    public bool IsAvailable => _isAvailable;
    public bool SupportsLockScreen => true;

    public bool Initialize()
    {
        try
        {
            _context = interception_create_context();
            _isAvailable = _context != IntPtr.Zero;

            if (_isAvailable)
            {
                // 查找第一个可用的键盘和鼠标设备
                for (int i = INTERCEPTION_KEYBOARD_FIRST; i < INTERCEPTION_KEYBOARD_FIRST + 10; i++)
                {
                    if (interception_is_keyboard(i) != 0)
                    {
                        _keyboardDevice = i;
                        break;
                    }
                }

                for (int i = INTERCEPTION_MOUSE_FIRST; i < INTERCEPTION_MOUSE_FIRST + 10; i++)
                {
                    if (interception_is_mouse(i) != 0)
                    {
                        _mouseDevice = i;
                        break;
                    }
                }
            }

            return _isAvailable;
        }
        catch (DllNotFoundException)
        {
            _isAvailable = false;
            return false;
        }
        catch (Exception)
        {
            _isAvailable = false;
            return false;
        }
    }

    #region 鼠标操作

    public void MouseMoveTo(int x, int y)
    {
        if (!_isAvailable) return;

        // 转换为绝对坐标 (0-65535)
        int screenWidth = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
        int screenHeight = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);

        var stroke = new MouseStroke
        {
            state = 0,
            flags = INTERCEPTION_MOUSE_MOVE_ABSOLUTE,
            x = (int)((x * 65535.0) / screenWidth),
            y = (int)((y * 65535.0) / screenHeight),
            rolling = 0,
            information = 0
        };

        interception_send(_context, _mouseDevice, ref stroke, 1);
    }

    public void MouseMoveBy(int deltaX, int deltaY)
    {
        if (!_isAvailable) return;

        var stroke = new MouseStroke
        {
            state = 0,
            flags = INTERCEPTION_MOUSE_MOVE_RELATIVE,
            x = deltaX,
            y = deltaY,
            rolling = 0,
            information = 0
        };

        interception_send(_context, _mouseDevice, ref stroke, 1);
    }

    public void MouseButtonDown(MouseButton button)
    {
        if (!_isAvailable) return;

        var stroke = new MouseStroke
        {
            state = GetMouseDownState(button),
            flags = 0,
            x = 0,
            y = 0,
            rolling = 0,
            information = 0
        };

        interception_send(_context, _mouseDevice, ref stroke, 1);
    }

    public void MouseButtonUp(MouseButton button)
    {
        if (!_isAvailable) return;

        var stroke = new MouseStroke
        {
            state = GetMouseUpState(button),
            flags = 0,
            x = 0,
            y = 0,
            rolling = 0,
            information = 0
        };

        interception_send(_context, _mouseDevice, ref stroke, 1);
    }

    public void MouseWheel(int delta, bool horizontal = false)
    {
        if (!_isAvailable) return;

        var stroke = new MouseStroke
        {
            state = horizontal ? INTERCEPTION_MOUSE_HWHEEL : INTERCEPTION_MOUSE_WHEEL,
            flags = 0,
            x = 0,
            y = 0,
            rolling = (short)(delta * 120),
            information = 0
        };

        interception_send(_context, _mouseDevice, ref stroke, 1);
    }

    public (int x, int y) GetMousePosition()
    {
        // Interception 不提供获取位置的功能，使用系统 API
        if (NativeMethods.GetCursorPos(out NativeMethods.POINT point))
        {
            return (point.X, point.Y);
        }
        return (0, 0);
    }

    #endregion

    #region 键盘操作

    public void KeyDown(ushort keyCode)
    {
        if (!_isAvailable) return;

        // 转换 Virtual Key Code 到 Scan Code
        ushort scanCode = VirtualKeyToScanCode(keyCode);
        ushort state = INTERCEPTION_KEY_DOWN;

        if (IsExtendedKey(keyCode))
        {
            state |= INTERCEPTION_KEY_E0;
        }

        var stroke = new KeyStroke
        {
            code = scanCode,
            state = state,
            information = 0
        };

        interception_send(_context, _keyboardDevice, ref stroke, 1);
    }

    public void KeyUp(ushort keyCode)
    {
        if (!_isAvailable) return;

        ushort scanCode = VirtualKeyToScanCode(keyCode);
        ushort state = INTERCEPTION_KEY_UP;

        if (IsExtendedKey(keyCode))
        {
            state |= INTERCEPTION_KEY_E0;
        }

        var stroke = new KeyStroke
        {
            code = scanCode,
            state = state,
            information = 0
        };

        interception_send(_context, _keyboardDevice, ref stroke, 1);
    }

    public void SendUnicode(char character)
    {
        // Interception 不直接支持 Unicode，需要使用 VK 码
        // 对于复杂字符，回退到 SendInput
        ushort vk = (ushort)NativeMethods.VkKeyScan(character);
        if (vk != 0xFFFF)
        {
            bool needShift = (vk & 0x100) != 0;
            ushort keyCode = (ushort)(vk & 0xFF);

            if (needShift) KeyDown(0x10); // VK_SHIFT
            KeyDown(keyCode);
            KeyUp(keyCode);
            if (needShift) KeyUp(0x10);
        }
        else
        {
            // 无法映射的字符，使用 SendInput 作为后备
            var inputDown = NativeMethods.CreateUnicodeInput(character, keyUp: false);
            var inputUp = NativeMethods.CreateUnicodeInput(character, keyUp: true);
            NativeMethods.SendInput(1, [inputDown], NativeMethods.InputSize);
            NativeMethods.SendInput(1, [inputUp], NativeMethods.InputSize);
        }
    }

    #endregion

    #region 辅助方法

    private static ushort GetMouseDownState(MouseButton button) => button switch
    {
        MouseButton.Left => INTERCEPTION_MOUSE_LEFT_BUTTON_DOWN,
        MouseButton.Right => INTERCEPTION_MOUSE_RIGHT_BUTTON_DOWN,
        MouseButton.Middle => INTERCEPTION_MOUSE_MIDDLE_BUTTON_DOWN,
        MouseButton.XButton1 => INTERCEPTION_MOUSE_BUTTON_4_DOWN,
        MouseButton.XButton2 => INTERCEPTION_MOUSE_BUTTON_5_DOWN,
        _ => INTERCEPTION_MOUSE_LEFT_BUTTON_DOWN
    };

    private static ushort GetMouseUpState(MouseButton button) => button switch
    {
        MouseButton.Left => INTERCEPTION_MOUSE_LEFT_BUTTON_UP,
        MouseButton.Right => INTERCEPTION_MOUSE_RIGHT_BUTTON_UP,
        MouseButton.Middle => INTERCEPTION_MOUSE_MIDDLE_BUTTON_UP,
        MouseButton.XButton1 => INTERCEPTION_MOUSE_BUTTON_4_UP,
        MouseButton.XButton2 => INTERCEPTION_MOUSE_BUTTON_5_UP,
        _ => INTERCEPTION_MOUSE_LEFT_BUTTON_UP
    };

    private static bool IsExtendedKey(ushort keyCode)
    {
        return keyCode is 0x2D or 0x2E or 0x24 or 0x23 or 0x21 or 0x22
            or 0x25 or 0x27 or 0x26 or 0x28
            or 0x90 or 0x2C or 0x6F
            or 0xA3 or 0xA5 or 0x5B or 0x5C or 0x5D;
    }

    /// <summary>
    /// 将 Virtual Key Code 转换为 Scan Code
    /// </summary>
    private static ushort VirtualKeyToScanCode(ushort vk)
    {
        uint scanCode = NativeMethods.MapVirtualKey(vk, 0); // MAPVK_VK_TO_VSC
        return (ushort)scanCode;
    }

    #endregion

    public void Dispose()
    {
        if (_context != IntPtr.Zero)
        {
            interception_destroy_context(_context);
            _context = IntPtr.Zero;
        }
        _isAvailable = false;
    }
}
