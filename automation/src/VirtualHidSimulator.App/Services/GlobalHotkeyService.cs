using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace VirtualHidSimulator.App.Services;

internal sealed class GlobalHotkeyService : IDisposable
{
    private readonly HwndSource _source;
    private readonly Action<int> _onHotkey;
    private bool _disposed;

    public GlobalHotkeyService(HwndSource source, Action<int> onHotkey)
    {
        _source = source;
        _onHotkey = onHotkey;
        _source.AddHook(WndProc);
    }

    public bool TryRegister(int id, uint modifiers, uint virtualKey)
    {
        return RegisterHotKey(_source.Handle, id, modifiers, virtualKey);
    }

    public void Unregister(int id)
    {
        UnregisterHotKey(_source.Handle, id);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            _onHotkey(id);
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _source.RemoveHook(WndProc);
    }

    private const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

