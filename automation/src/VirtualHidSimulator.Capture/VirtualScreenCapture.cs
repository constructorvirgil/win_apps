using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace VirtualHidSimulator.Capture;

public sealed record CaptureOptions(bool IncludeCursor = true, int JpegQuality = 85);

public sealed record CaptureBounds(int Left, int Top, int Width, int Height);

public static class VirtualScreenCapture
{
    public static (BitmapSource image, CaptureBounds bounds) CaptureVirtualScreen(CaptureOptions? options = null)
    {
        options ??= new CaptureOptions();

        int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
        int top = GetSystemMetrics(SM_YVIRTUALSCREEN);
        int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException($"Invalid virtual screen size: {width}x{height}.");
        }

        var desktopWnd = GetDesktopWindow();
        var desktopDc = GetWindowDC(desktopWnd);
        if (desktopDc == IntPtr.Zero)
        {
            desktopWnd = IntPtr.Zero;
            desktopDc = GetDC(IntPtr.Zero);
        }
        if (desktopDc == IntPtr.Zero) throw new InvalidOperationException("Failed to acquire desktop DC.");

        var memoryDc = CreateCompatibleDC(desktopDc);
        if (memoryDc == IntPtr.Zero)
        {
            ReleaseDC(desktopWnd, desktopDc);
            throw new InvalidOperationException("CreateCompatibleDC failed.");
        }

        var hBitmap = CreateCompatibleBitmap(desktopDc, width, height);
        if (hBitmap == IntPtr.Zero)
        {
            DeleteDC(memoryDc);
            ReleaseDC(desktopWnd, desktopDc);
            throw new InvalidOperationException($"CreateCompatibleBitmap failed. Win32Error={Marshal.GetLastWin32Error()}");
        }

        var old = SelectObject(memoryDc, hBitmap);
        try
        {
            // CAPTUREBLT can fail with ERROR_ACCESS_DENIED in some environments. Retry without it.
            if (!BitBlt(memoryDc, 0, 0, width, height, desktopDc, left, top, SRCCOPY | CAPTUREBLT))
            {
                var err1 = Marshal.GetLastWin32Error();
                if (!BitBlt(memoryDc, 0, 0, width, height, desktopDc, left, top, SRCCOPY))
                {
                    var err2 = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException($"BitBlt failed. Win32Error={err1}, retry_without_captureblt={err2}");
                }
            }

            if (options.IncludeCursor)
            {
                DrawCursor(memoryDc, left, top);
            }

            var image = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            image.Freeze();

            return (image, new CaptureBounds(left, top, width, height));
        }
        finally
        {
            SelectObject(memoryDc, old);
            DeleteObject(hBitmap);
            DeleteDC(memoryDc);
            ReleaseDC(desktopWnd, desktopDc);
        }
    }

    public static byte[] EncodeJpeg(BitmapSource image, int quality = 85)
    {
        quality = Math.Clamp(quality, 1, 100);

        var encoder = new JpegBitmapEncoder { QualityLevel = quality };
        encoder.Frames.Add(BitmapFrame.Create(image));

        using var ms = new MemoryStream();
        encoder.Save(ms);
        return ms.ToArray();
    }

    private static void DrawCursor(IntPtr targetDc, int virtualLeft, int virtualTop)
    {
        var cursorInfo = new CURSORINFO { cbSize = Marshal.SizeOf<CURSORINFO>() };
        if (!GetCursorInfo(ref cursorInfo)) return;
        if ((cursorInfo.flags & CURSOR_SHOWING) == 0) return;

        int x = cursorInfo.ptScreenPos.x - virtualLeft;
        int y = cursorInfo.ptScreenPos.y - virtualTop;

        int hotspotX = 0;
        int hotspotY = 0;
        if (GetIconInfo(cursorInfo.hCursor, out var iconInfo))
        {
            hotspotX = iconInfo.xHotspot;
            hotspotY = iconInfo.yHotspot;
            if (iconInfo.hbmMask != IntPtr.Zero) DeleteObject(iconInfo.hbmMask);
            if (iconInfo.hbmColor != IntPtr.Zero) DeleteObject(iconInfo.hbmColor);
        }

        x -= hotspotX;
        y -= hotspotY;

        DrawIconEx(targetDc, x, y, cursorInfo.hCursor, 0, 0, 0, IntPtr.Zero, DI_NORMAL);
    }

    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    private const int SRCCOPY = 0x00CC0020;
    private const int CAPTUREBLT = 0x40000000;

    private const int CURSOR_SHOWING = 0x00000001;
    private const int DI_NORMAL = 0x0003;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSrc, int xSrc, int ySrc, int rop);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr ho);

    [DllImport("user32.dll")]
    private static extern bool GetCursorInfo(ref CURSORINFO pci);

    [DllImport("user32.dll")]
    private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);

    [DllImport("user32.dll")]
    private static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyWidth, int istepIfAniCur, IntPtr hbrFlickerFreeDraw, int diFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CURSORINFO
    {
        public int cbSize;
        public int flags;
        public IntPtr hCursor;
        public POINT ptScreenPos;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }


}
