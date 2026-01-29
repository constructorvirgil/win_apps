namespace automation.Core.HidDefinitions;

/// <summary>
/// Windows 虚拟键码定义
/// 参考: https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
/// </summary>
public static class VirtualKeyCodes
{
    #region 鼠标按键

    public const ushort VK_LBUTTON = 0x01;    // 鼠标左键
    public const ushort VK_RBUTTON = 0x02;    // 鼠标右键
    public const ushort VK_CANCEL = 0x03;     // Ctrl+Break
    public const ushort VK_MBUTTON = 0x04;    // 鼠标中键
    public const ushort VK_XBUTTON1 = 0x05;   // 鼠标X1键
    public const ushort VK_XBUTTON2 = 0x06;   // 鼠标X2键

    #endregion

    #region 控制键

    public const ushort VK_BACK = 0x08;       // Backspace
    public const ushort VK_TAB = 0x09;        // Tab
    public const ushort VK_CLEAR = 0x0C;      // Clear
    public const ushort VK_RETURN = 0x0D;     // Enter
    public const ushort VK_SHIFT = 0x10;      // Shift
    public const ushort VK_CONTROL = 0x11;    // Ctrl
    public const ushort VK_MENU = 0x12;       // Alt
    public const ushort VK_PAUSE = 0x13;      // Pause
    public const ushort VK_CAPITAL = 0x14;    // Caps Lock
    public const ushort VK_ESCAPE = 0x1B;     // Esc
    public const ushort VK_SPACE = 0x20;      // 空格

    #endregion

    #region 导航键

    public const ushort VK_PRIOR = 0x21;      // Page Up
    public const ushort VK_NEXT = 0x22;       // Page Down
    public const ushort VK_END = 0x23;        // End
    public const ushort VK_HOME = 0x24;       // Home
    public const ushort VK_LEFT = 0x25;       // 左箭头
    public const ushort VK_UP = 0x26;         // 上箭头
    public const ushort VK_RIGHT = 0x27;      // 右箭头
    public const ushort VK_DOWN = 0x28;       // 下箭头
    public const ushort VK_SELECT = 0x29;     // Select
    public const ushort VK_PRINT = 0x2A;      // Print
    public const ushort VK_EXECUTE = 0x2B;    // Execute
    public const ushort VK_SNAPSHOT = 0x2C;   // Print Screen
    public const ushort VK_INSERT = 0x2D;     // Insert
    public const ushort VK_DELETE = 0x2E;     // Delete
    public const ushort VK_HELP = 0x2F;       // Help

    #endregion

    #region 数字键 (主键盘)

    public const ushort VK_0 = 0x30;
    public const ushort VK_1 = 0x31;
    public const ushort VK_2 = 0x32;
    public const ushort VK_3 = 0x33;
    public const ushort VK_4 = 0x34;
    public const ushort VK_5 = 0x35;
    public const ushort VK_6 = 0x36;
    public const ushort VK_7 = 0x37;
    public const ushort VK_8 = 0x38;
    public const ushort VK_9 = 0x39;

    #endregion

    #region 字母键

    public const ushort VK_A = 0x41;
    public const ushort VK_B = 0x42;
    public const ushort VK_C = 0x43;
    public const ushort VK_D = 0x44;
    public const ushort VK_E = 0x45;
    public const ushort VK_F = 0x46;
    public const ushort VK_G = 0x47;
    public const ushort VK_H = 0x48;
    public const ushort VK_I = 0x49;
    public const ushort VK_J = 0x4A;
    public const ushort VK_K = 0x4B;
    public const ushort VK_L = 0x4C;
    public const ushort VK_M = 0x4D;
    public const ushort VK_N = 0x4E;
    public const ushort VK_O = 0x4F;
    public const ushort VK_P = 0x50;
    public const ushort VK_Q = 0x51;
    public const ushort VK_R = 0x52;
    public const ushort VK_S = 0x53;
    public const ushort VK_T = 0x54;
    public const ushort VK_U = 0x55;
    public const ushort VK_V = 0x56;
    public const ushort VK_W = 0x57;
    public const ushort VK_X = 0x58;
    public const ushort VK_Y = 0x59;
    public const ushort VK_Z = 0x5A;

    #endregion

    #region Windows 键

    public const ushort VK_LWIN = 0x5B;       // 左Windows键
    public const ushort VK_RWIN = 0x5C;       // 右Windows键
    public const ushort VK_APPS = 0x5D;       // 应用程序键

    #endregion

    #region 数字小键盘

    public const ushort VK_NUMPAD0 = 0x60;
    public const ushort VK_NUMPAD1 = 0x61;
    public const ushort VK_NUMPAD2 = 0x62;
    public const ushort VK_NUMPAD3 = 0x63;
    public const ushort VK_NUMPAD4 = 0x64;
    public const ushort VK_NUMPAD5 = 0x65;
    public const ushort VK_NUMPAD6 = 0x66;
    public const ushort VK_NUMPAD7 = 0x67;
    public const ushort VK_NUMPAD8 = 0x68;
    public const ushort VK_NUMPAD9 = 0x69;
    public const ushort VK_MULTIPLY = 0x6A;   // 小键盘 *
    public const ushort VK_ADD = 0x6B;        // 小键盘 +
    public const ushort VK_SEPARATOR = 0x6C;  // 分隔符
    public const ushort VK_SUBTRACT = 0x6D;   // 小键盘 -
    public const ushort VK_DECIMAL = 0x6E;    // 小键盘 .
    public const ushort VK_DIVIDE = 0x6F;     // 小键盘 /

    #endregion

    #region 功能键

    public const ushort VK_F1 = 0x70;
    public const ushort VK_F2 = 0x71;
    public const ushort VK_F3 = 0x72;
    public const ushort VK_F4 = 0x73;
    public const ushort VK_F5 = 0x74;
    public const ushort VK_F6 = 0x75;
    public const ushort VK_F7 = 0x76;
    public const ushort VK_F8 = 0x77;
    public const ushort VK_F9 = 0x78;
    public const ushort VK_F10 = 0x79;
    public const ushort VK_F11 = 0x7A;
    public const ushort VK_F12 = 0x7B;
    public const ushort VK_F13 = 0x7C;
    public const ushort VK_F14 = 0x7D;
    public const ushort VK_F15 = 0x7E;
    public const ushort VK_F16 = 0x7F;
    public const ushort VK_F17 = 0x80;
    public const ushort VK_F18 = 0x81;
    public const ushort VK_F19 = 0x82;
    public const ushort VK_F20 = 0x83;
    public const ushort VK_F21 = 0x84;
    public const ushort VK_F22 = 0x85;
    public const ushort VK_F23 = 0x86;
    public const ushort VK_F24 = 0x87;

    #endregion

    #region 锁定键

    public const ushort VK_NUMLOCK = 0x90;    // Num Lock
    public const ushort VK_SCROLL = 0x91;     // Scroll Lock

    #endregion

    #region 修饰键（区分左右）

    public const ushort VK_LSHIFT = 0xA0;     // 左Shift
    public const ushort VK_RSHIFT = 0xA1;     // 右Shift
    public const ushort VK_LCONTROL = 0xA2;   // 左Ctrl
    public const ushort VK_RCONTROL = 0xA3;   // 右Ctrl
    public const ushort VK_LMENU = 0xA4;      // 左Alt
    public const ushort VK_RMENU = 0xA5;      // 右Alt

    #endregion

    #region 浏览器控制键

    public const ushort VK_BROWSER_BACK = 0xA6;
    public const ushort VK_BROWSER_FORWARD = 0xA7;
    public const ushort VK_BROWSER_REFRESH = 0xA8;
    public const ushort VK_BROWSER_STOP = 0xA9;
    public const ushort VK_BROWSER_SEARCH = 0xAA;
    public const ushort VK_BROWSER_FAVORITES = 0xAB;
    public const ushort VK_BROWSER_HOME = 0xAC;

    #endregion

    #region 媒体控制键

    public const ushort VK_VOLUME_MUTE = 0xAD;
    public const ushort VK_VOLUME_DOWN = 0xAE;
    public const ushort VK_VOLUME_UP = 0xAF;
    public const ushort VK_MEDIA_NEXT_TRACK = 0xB0;
    public const ushort VK_MEDIA_PREV_TRACK = 0xB1;
    public const ushort VK_MEDIA_STOP = 0xB2;
    public const ushort VK_MEDIA_PLAY_PAUSE = 0xB3;

    #endregion

    #region OEM 键

    public const ushort VK_OEM_1 = 0xBA;      // ; :
    public const ushort VK_OEM_PLUS = 0xBB;   // = +
    public const ushort VK_OEM_COMMA = 0xBC;  // , <
    public const ushort VK_OEM_MINUS = 0xBD;  // - _
    public const ushort VK_OEM_PERIOD = 0xBE; // . >
    public const ushort VK_OEM_2 = 0xBF;      // / ?
    public const ushort VK_OEM_3 = 0xC0;      // ` ~
    public const ushort VK_OEM_4 = 0xDB;      // [ {
    public const ushort VK_OEM_5 = 0xDC;      // \ |
    public const ushort VK_OEM_6 = 0xDD;      // ] }
    public const ushort VK_OEM_7 = 0xDE;      // ' "
    public const ushort VK_OEM_8 = 0xDF;      // 其他OEM特定

    #endregion

    #region 辅助方法

    /// <summary>
    /// 从字符获取虚拟键码
    /// </summary>
    public static ushort FromChar(char c)
    {
        // 数字
        if (c >= '0' && c <= '9')
            return (ushort)(VK_0 + (c - '0'));

        // 字母（转大写）
        char upper = char.ToUpper(c);
        if (upper >= 'A' && upper <= 'Z')
            return (ushort)(VK_A + (upper - 'A'));

        // 特殊字符
        return c switch
        {
            ' ' => VK_SPACE,
            '\t' => VK_TAB,
            '\n' or '\r' => VK_RETURN,
            '-' or '_' => VK_OEM_MINUS,
            '=' or '+' => VK_OEM_PLUS,
            '[' or '{' => VK_OEM_4,
            ']' or '}' => VK_OEM_6,
            '\\' or '|' => VK_OEM_5,
            ';' or ':' => VK_OEM_1,
            '\'' or '"' => VK_OEM_7,
            ',' or '<' => VK_OEM_COMMA,
            '.' or '>' => VK_OEM_PERIOD,
            '/' or '?' => VK_OEM_2,
            '`' or '~' => VK_OEM_3,
            _ => 0
        };
    }

    /// <summary>
    /// 检查字符是否需要 Shift 键
    /// </summary>
    public static bool NeedsShift(char c)
    {
        return c switch
        {
            >= 'A' and <= 'Z' => true,
            '!' or '@' or '#' or '$' or '%' or '^' or '&' or '*' or '(' or ')' => true,
            '_' or '+' or '{' or '}' or '|' or ':' or '"' or '<' or '>' or '?' or '~' => true,
            _ => false
        };
    }

    /// <summary>
    /// 从按键名称获取虚拟键码
    /// </summary>
    public static ushort FromName(string name)
    {
        return name.ToUpper() switch
        {
            "ENTER" or "RETURN" => VK_RETURN,
            "TAB" => VK_TAB,
            "SPACE" => VK_SPACE,
            "BACKSPACE" or "BACK" => VK_BACK,
            "ESC" or "ESCAPE" => VK_ESCAPE,
            "DELETE" or "DEL" => VK_DELETE,
            "INSERT" or "INS" => VK_INSERT,
            "HOME" => VK_HOME,
            "END" => VK_END,
            "PAGEUP" or "PGUP" => VK_PRIOR,
            "PAGEDOWN" or "PGDN" => VK_NEXT,
            "UP" => VK_UP,
            "DOWN" => VK_DOWN,
            "LEFT" => VK_LEFT,
            "RIGHT" => VK_RIGHT,
            "SHIFT" => VK_SHIFT,
            "CTRL" or "CONTROL" => VK_CONTROL,
            "ALT" or "MENU" => VK_MENU,
            "WIN" or "WINDOWS" or "LWIN" => VK_LWIN,
            "CAPSLOCK" or "CAPS" => VK_CAPITAL,
            "NUMLOCK" => VK_NUMLOCK,
            "SCROLLLOCK" => VK_SCROLL,
            "PRINTSCREEN" or "PRTSC" => VK_SNAPSHOT,
            "PAUSE" => VK_PAUSE,
            "F1" => VK_F1,
            "F2" => VK_F2,
            "F3" => VK_F3,
            "F4" => VK_F4,
            "F5" => VK_F5,
            "F6" => VK_F6,
            "F7" => VK_F7,
            "F8" => VK_F8,
            "F9" => VK_F9,
            "F10" => VK_F10,
            "F11" => VK_F11,
            "F12" => VK_F12,
            _ when name.Length == 1 => FromChar(name[0]),
            _ => 0
        };
    }

    #endregion
}
