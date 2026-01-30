using System.Runtime.InteropServices;

namespace drvercheck.Probe;

internal sealed class ProbeWindow : Form
{
    private readonly TextBox _textBox;
    private readonly object _lock = new();
    private readonly List<ProbeEvent> _events = new();
    private IMessageFilter? _filter;
    private LowLevelHooks? _hooks;

    internal ProbeWindow()
    {
        Text = "drvercheck probe";
        Width = 640;
        Height = 240;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;

        _textBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            Font = new Font(FontFamily.GenericMonospace, 12),
        };

        Controls.Add(_textBox);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        _filter ??= new ProbeMessageFilter(this, AddEvent);
        Application.AddMessageFilter(_filter);

        _hooks ??= new LowLevelHooks(AddLowLevelEvent);
        _hooks.Install();
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        try
        {
            if (_filter != null) Application.RemoveMessageFilter(_filter);
        }
        catch
        {
            // best effort
        }

        try { _hooks?.Dispose(); } catch { }

        base.OnHandleDestroyed(e);
    }

    internal void ClearState()
    {
        if (InvokeRequired)
        {
            Invoke(ClearState);
            return;
        }

        lock (_lock) _events.Clear();
        _textBox.Clear();
    }

    internal void EnsureForegroundAndFocus()
    {
        if (InvokeRequired)
        {
            Invoke(EnsureForegroundAndFocus);
            return;
        }

        TopMost = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
        _textBox.Focus();
        _textBox.Select();

        try
        {
            SetForegroundWindow(Handle);
        }
        catch
        {
            // best effort
        }
    }

    internal ProbeSnapshot Snapshot()
    {
        if (InvokeRequired)
        {
            return (ProbeSnapshot)Invoke(Snapshot)!;
        }

        List<ProbeEvent> copy;
        lock (_lock) copy = _events.ToList();

        var rect = GetWindowRectOrEmpty();
        var target = GetTargetPoint();

        return new ProbeSnapshot
        {
            Text = _textBox.Text ?? "",
            CapturedAt = DateTimeOffset.Now,
            Events = copy,
            WindowRect = rect,
            TargetPoint = target,
            WindowHandle = Handle.ToInt64(),
            ProcessId = Environment.ProcessId
        };
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
    }

    private void AddEvent(uint msg, IntPtr wParam, IntPtr lParam)
    {
        lock (_lock)
        {
            _events.Add(new ProbeEvent
            {
                Ts = DateTimeOffset.Now,
                Kind = KindFor((int)msg),
                Msg = msg,
                WParam = wParam.ToInt64(),
                LParam = lParam.ToInt64()
            });
        }
    }

    private void AddLowLevelEvent(string kind, uint msg, long wParam, long lParam)
    {
        lock (_lock)
        {
            _events.Add(new ProbeEvent
            {
                Ts = DateTimeOffset.Now,
                Kind = kind,
                Msg = msg,
                WParam = wParam,
                LParam = lParam
            });
        }
    }

    private ProbeRect GetWindowRectOrEmpty()
    {
        if (!GetWindowRect(Handle, out RECT r)) return default;
        return new ProbeRect(r.Left, r.Top, r.Right, r.Bottom);
    }

    private ProbePoint GetTargetPoint()
    {
        // Screen coords of textbox center; click target.
        var center = new Point(_textBox.Width / 2, _textBox.Height / 2);
        var screen = _textBox.PointToScreen(center);
        return new ProbePoint(screen.X, screen.Y);
    }

    private static string KindFor(int msg) => msg switch
    {
        WM_KEYDOWN => "key_down",
        WM_KEYUP => "key_up",
        WM_CHAR => "char",
        WM_LBUTTONDOWN => "mouse_left_down",
        WM_LBUTTONUP => "mouse_left_up",
        WM_RBUTTONDOWN => "mouse_right_down",
        WM_RBUTTONUP => "mouse_right_up",
        WM_MBUTTONDOWN => "mouse_middle_down",
        WM_MBUTTONUP => "mouse_middle_up",
        WM_MOUSEWHEEL => "mouse_wheel",
        WM_MOUSEHWHEEL => "mouse_hwheel",
        _ => "msg"
    };

    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_CHAR = 0x0102;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MBUTTONUP = 0x0208;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_MOUSEHWHEEL = 0x020E;
    private const int GA_ROOT = 2;
    private const int VK_PACKET = 0xE7;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, int gaFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private sealed class ProbeMessageFilter : IMessageFilter
    {
        private readonly ProbeWindow _owner;
        private readonly Action<uint, IntPtr, IntPtr> _log;

        public ProbeMessageFilter(ProbeWindow owner, Action<uint, IntPtr, IntPtr> log)
        {
            _owner = owner;
            _log = log;
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (!_owner.IsHandleCreated) return false;

            // Only log messages for this window tree (form + children).
            var root = GetAncestor(m.HWnd, GA_ROOT);
            if (root != _owner.Handle) return false;

            if (m.Msg is WM_KEYDOWN or WM_KEYUP or WM_CHAR
                or WM_LBUTTONDOWN or WM_LBUTTONUP
                or WM_RBUTTONDOWN or WM_RBUTTONUP
                or WM_MBUTTONDOWN or WM_MBUTTONUP
                or WM_MOUSEWHEEL or WM_MOUSEHWHEEL)
            {
                _log((uint)m.Msg, m.WParam, m.LParam);
            }

            return false;
        }
    }

    private sealed class LowLevelHooks : IDisposable
    {
        private readonly Action<string, uint, long, long> _log;
        private HookProc? _keyboardProc;
        private HookProc? _mouseProc;
        private IntPtr _keyboardHook = IntPtr.Zero;
        private IntPtr _mouseHook = IntPtr.Zero;

        public LowLevelHooks(Action<string, uint, long, long> log)
        {
            _log = log;
        }

        public void Install()
        {
            if (_keyboardHook != IntPtr.Zero || _mouseHook != IntPtr.Zero) return;

            _keyboardProc = KeyboardHookCallback;
            _mouseProc = MouseHookCallback;

            var module = GetModuleHandle(null);
            _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, module, 0);
            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, module, 0);
        }

        public void Dispose()
        {
            if (_keyboardHook != IntPtr.Zero) UnhookWindowsHookEx(_keyboardHook);
            if (_mouseHook != IntPtr.Zero) UnhookWindowsHookEx(_mouseHook);
            _keyboardHook = IntPtr.Zero;
            _mouseHook = IntPtr.Zero;
            _keyboardProc = null;
            _mouseProc = null;
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var msg = (uint)wParam.ToInt64();
                var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                var kind = msg switch
                {
                    WM_KEYDOWN => "ll_key_down",
                    WM_KEYUP => "ll_key_up",
                    0x0104 => "ll_syskey_down", // WM_SYSKEYDOWN
                    0x0105 => "ll_syskey_up",   // WM_SYSKEYUP
                    _ => "ll_key"
                };

                // Store vkCode in WParam, and (flags | scanCode<<16) in LParam for quick diagnostics.
                long packed = (long)data.flags | ((long)data.scanCode << 16);
                _log(kind, msg, data.vkCode, packed);
            }

            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var msg = (uint)wParam.ToInt64();
                var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                var kind = msg switch
                {
                    0x0201 => "ll_mouse_left_down",  // WM_LBUTTONDOWN
                    0x0202 => "ll_mouse_left_up",    // WM_LBUTTONUP
                    0x0204 => "ll_mouse_right_down", // WM_RBUTTONDOWN
                    0x0205 => "ll_mouse_right_up",   // WM_RBUTTONUP
                    0x0207 => "ll_mouse_middle_down",// WM_MBUTTONDOWN
                    0x0208 => "ll_mouse_middle_up",  // WM_MBUTTONUP
                    0x020A => "ll_mouse_wheel",      // WM_MOUSEWHEEL
                    0x0200 => "ll_mouse_move",       // WM_MOUSEMOVE
                    _ => "ll_mouse"
                };

                long packed = ((long)data.pt.x & 0xFFFFFFFFL) | ((long)data.pt.y << 32);
                _log(kind, msg, data.mouseData, packed);
            }

            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);
    }
}
