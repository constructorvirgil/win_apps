using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VirtualHidSimulator.Capture;

public static class ClipboardImageWriter
{
    public static bool TrySetImage(BitmapSource image, TimeSpan timeout, out string? error)
    {
        error = null;

        if (image.PixelWidth <= 0 || image.PixelHeight <= 0)
        {
            error = "invalid image size";
            return false;
        }

        var bgr32 = EnsureBgr32(image);
        var width = bgr32.PixelWidth;
        var height = bgr32.PixelHeight;
        var stride = checked(width * 4);
        var topDownPixels = new byte[checked(stride * height)];
        bgr32.CopyPixels(topDownPixels, stride, 0);

        // CF_DIB expects bottom-up pixel order for BI_RGB. Many consumers (including WPF/OLE)
        // reject top-down DIBs even though Win32 supports negative heights.
        var pixels = new byte[topDownPixels.Length];
        for (int y = 0; y < height; y++)
        {
            Buffer.BlockCopy(
                topDownPixels,
                y * stride,
                pixels,
                (height - 1 - y) * stride,
                stride);
        }

        IntPtr hMem = IntPtr.Zero;
        IntPtr ptr = IntPtr.Zero;

        try
        {
            var header = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = height,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = BI_RGB,
                biSizeImage = 0,
                biXPelsPerMeter = 0,
                biYPelsPerMeter = 0,
                biClrUsed = 0,
                biClrImportant = 0,
            };

            var headerSize = Marshal.SizeOf<BITMAPINFOHEADER>();
            var totalSize = checked(headerSize + pixels.Length);
            hMem = GlobalAlloc(GMEM_MOVEABLE, (nuint)totalSize);
            if (hMem == IntPtr.Zero)
            {
                error = $"GlobalAlloc failed (err={Marshal.GetLastWin32Error()})";
                return false;
            }

            ptr = GlobalLock(hMem);
            if (ptr == IntPtr.Zero)
            {
                error = $"GlobalLock failed (err={Marshal.GetLastWin32Error()})";
                return false;
            }

            Marshal.StructureToPtr(header, ptr, fDeleteOld: false);
            Marshal.Copy(pixels, 0, IntPtr.Add(ptr, headerSize), pixels.Length);
            GlobalUnlock(hMem);
            ptr = IntPtr.Zero;

            var sw = Stopwatch.StartNew();
            while (true)
            {
                if (OpenClipboard(IntPtr.Zero))
                    break;

                if (sw.Elapsed >= timeout)
                {
                    error = "OpenClipboard timeout (clipboard busy)";
                    return false;
                }

                Thread.Sleep(10);
            }

            try
            {
                if (!EmptyClipboard())
                {
                    error = $"EmptyClipboard failed (err={Marshal.GetLastWin32Error()})";
                    return false;
                }

                if (SetClipboardData(CF_DIB, hMem) == IntPtr.Zero)
                {
                    error = $"SetClipboardData(CF_DIB) failed (err={Marshal.GetLastWin32Error()})";
                    return false;
                }

                // Clipboard now owns the memory handle.
                hMem = IntPtr.Zero;
                return true;
            }
            finally
            {
                CloseClipboard();
            }
        }
        finally
        {
            if (ptr != IntPtr.Zero) GlobalUnlock(hMem);
            if (hMem != IntPtr.Zero) GlobalFree(hMem);
        }
    }

    private static BitmapSource EnsureBgr32(BitmapSource image)
    {
        if (image.Format == PixelFormats.Bgr32)
            return image;

        var converted = new FormatConvertedBitmap();
        converted.BeginInit();
        converted.Source = image;
        converted.DestinationFormat = PixelFormats.Bgr32;
        converted.EndInit();
        converted.Freeze();
        return converted;
    }

    private const uint CF_DIB = 8;
    private const uint BI_RGB = 0;
    private const uint GMEM_MOVEABLE = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, nuint dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);
}
