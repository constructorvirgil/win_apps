using System.Windows;
using WinApps.MessageCenter.Core;

namespace WinApps.MessageCenter;

internal sealed class OverlayWindowManager
{
    private readonly List<OverlayToastWindow> _windows = new();
    private const int MaxWindows = 3;

    public bool HasOpenWindows => _windows.Any(w => w.IsVisible);

    public void Show(OverlayNotification notification)
    {
        CleanupClosed();

        while (_windows.Count >= MaxWindows)
        {
            var oldest = _windows.FirstOrDefault(w => w.IsVisible);
            if (oldest is null) break;
            oldest.Close();
            CleanupClosed();
        }

        var window = new OverlayToastWindow(notification);
        window.Closed += (_, _) =>
        {
            CleanupClosed();
            Reposition();
        };

        _windows.Add(window);
        window.Show();
        Reposition();
    }

    private void CleanupClosed()
    {
        _windows.RemoveAll(w => !w.IsVisible);
    }

    private void Reposition()
    {
        var workArea = SystemParameters.WorkArea;
        const double margin = 12;
        const double gap = 10;

        var width = workArea.Width / 7.0;
        var height = workArea.Height / 7.0;

        var x = workArea.Right - margin - width;
        var y = workArea.Bottom - margin;

        // Stack upward from bottom-right (newest at bottom).
        for (var i = _windows.Count - 1; i >= 0; i--)
        {
            var w = _windows[i];
            if (!w.IsVisible) continue;

            y -= height;
            w.SetBounds(left: x, top: y, width: width, height: height);
            y -= gap;
        }
    }
}
