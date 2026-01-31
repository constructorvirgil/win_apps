using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;
using VirtualHidSimulator.Capture;

namespace snapcheck;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine(
                """
                snapcheck - screenshot stability verifier

                Commands:
                  snapcheck capture --out <file.jpg> [--cursor 0|1] [--quality 1-100] [--timing 0|1]
                  snapcheck clipboard [--cursor 0|1] [--timing 0|1]
                  snapcheck selftest [--count N] [--cursor 0|1] [--clipboard 0|1] [--timing 0|1]
                  snapcheck check [--count N] [--cursor 0|1] [--maxms 1000] [--clipboard 0|1] [--clipms 400] [--keep 0|1]

                Notes:
                  - Captures the virtual screen (all monitors stitched) as-is.
                  - clipboard mode writes an image to the clipboard and reads it back.
                """);
            return 0;
        }

        try
        {
            var cmd = args[0].ToLowerInvariant();
            return cmd switch
            {
                "capture" => RunCapture(args.Skip(1).ToArray()),
                "clipboard" => RunClipboard(args.Skip(1).ToArray()),
                "selftest" => RunSelfTest(args.Skip(1).ToArray()),
                "check" => RunCheck(args.Skip(1).ToArray()),
                _ => Fail($"Unknown command: {args[0]}")
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static int RunCapture(string[] args)
    {
        var outPath = GetOption(args, "--out");
        if (string.IsNullOrWhiteSpace(outPath))
            return Fail("Missing --out <file.jpg>");

        var includeCursor = GetBool(args, "--cursor", defaultValue: true);
        var quality = GetInt(args, "--quality", 85);
        var timing = GetBool(args, "--timing", defaultValue: true);

        var swTotal = Stopwatch.StartNew();
        var sw = Stopwatch.StartNew();
        var (image, bounds) = VirtualScreenCapture.CaptureVirtualScreen(new CaptureOptions(includeCursor, quality));
        var captureMs = sw.ElapsedMilliseconds;

        sw.Restart();
        var bytes = VirtualScreenCapture.EncodeJpeg(image, quality);
        var encodeMs = sw.ElapsedMilliseconds;

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        sw.Restart();
        File.WriteAllBytes(outPath, bytes);
        var writeMs = sw.ElapsedMilliseconds;
        swTotal.Stop();

        Console.WriteLine($"ok: wrote {bytes.Length} bytes to {outPath}");
        Console.WriteLine($"bounds: left={bounds.Left} top={bounds.Top} size={bounds.Width}x{bounds.Height}");
        Console.WriteLine($"image: {image.PixelWidth}x{image.PixelHeight}");
        if (timing)
        {
            Console.WriteLine($"timing_ms: capture={captureMs} encode={encodeMs} write={writeMs} total={swTotal.ElapsedMilliseconds}");
        }
        return 0;
    }

    private static int RunClipboard(string[] args)
    {
        var includeCursor = GetBool(args, "--cursor", defaultValue: true);
        var timing = GetBool(args, "--timing", defaultValue: true);

        var swTotal = Stopwatch.StartNew();
        var sw = Stopwatch.StartNew();
        var (image, bounds) = VirtualScreenCapture.CaptureVirtualScreen(new CaptureOptions(includeCursor, 85));
        var captureMs = sw.ElapsedMilliseconds;

        sw.Restart();
        var clipboardOk = TrySetClipboardImage(image, TimeSpan.FromSeconds(5));
        var clipboardMs = sw.ElapsedMilliseconds;

        if (!clipboardOk)
            return Fail("clipboard: failed to write (clipboard busy). Close clipboard managers and retry.");

        if (!Clipboard.ContainsImage())
            return Fail("clipboard: Clipboard.ContainsImage() == false after set");

        var back = Clipboard.GetImage();
        if (back == null)
            return Fail("clipboard: Clipboard.GetImage() returned null");

        swTotal.Stop();
        Console.WriteLine("ok: clipboard image set and read back");
        Console.WriteLine($"bounds: left={bounds.Left} top={bounds.Top} size={bounds.Width}x{bounds.Height}");
        Console.WriteLine($"image(back): {back.PixelWidth}x{back.PixelHeight}");
        if (timing)
        {
            Console.WriteLine($"timing_ms: capture={captureMs} clipboard={clipboardMs} total={swTotal.ElapsedMilliseconds}");
        }
        return 0;
    }

    private static int RunSelfTest(string[] args)
    {
        var count = GetInt(args, "--count", 10);
        var includeCursor = GetBool(args, "--cursor", defaultValue: true);
        var doClipboard = GetBool(args, "--clipboard", defaultValue: false);
        var timing = GetBool(args, "--timing", defaultValue: true);

        long totalCaptureMs = 0;
        long totalEncodeMs = 0;
        long totalDecodeMs = 0;
        long clipboardMs = -1;

        for (int i = 1; i <= count; i++)
        {
            var sw = Stopwatch.StartNew();
            var (image, bounds) = VirtualScreenCapture.CaptureVirtualScreen(new CaptureOptions(includeCursor, 85));
            sw.Stop();
            totalCaptureMs += sw.ElapsedMilliseconds;

            if (image.PixelWidth != bounds.Width || image.PixelHeight != bounds.Height)
                return Fail($"selftest: size mismatch at iteration {i}: image={image.PixelWidth}x{image.PixelHeight} bounds={bounds.Width}x{bounds.Height}");

            sw.Restart();
            var bytes = VirtualScreenCapture.EncodeJpeg(image, 85);
            sw.Stop();
            totalEncodeMs += sw.ElapsedMilliseconds;
            if (bytes.Length < 1024)
                return Fail($"selftest: JPEG too small at iteration {i}: {bytes.Length} bytes");

            // Decode back to ensure encoder output is valid.
            sw.Restart();
            using var ms = new MemoryStream(bytes);
            var decoded = new JpegBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var frame = decoded.Frames[0];
            sw.Stop();
            totalDecodeMs += sw.ElapsedMilliseconds;
            if (frame.PixelWidth != image.PixelWidth || frame.PixelHeight != image.PixelHeight)
                return Fail($"selftest: decode mismatch at iteration {i}: decoded={frame.PixelWidth}x{frame.PixelHeight} expected={image.PixelWidth}x{image.PixelHeight}");

            if (doClipboard && i == 1)
            {
                sw.Restart();
                var ok = TrySetClipboardImage(image, TimeSpan.FromSeconds(5));
                sw.Stop();
                clipboardMs = sw.ElapsedMilliseconds;

                if (!ok)
                {
                    Console.WriteLine("skip: clipboard check (clipboard busy)");
                }
                else if (!Clipboard.ContainsImage())
                {
                    return Fail("selftest: Clipboard.ContainsImage() == false after set");
                }
            }

            Console.WriteLine($"pass {i}/{count}: {bounds.Width}x{bounds.Height}, jpeg={bytes.Length} bytes");
        }

        Console.WriteLine("ok: selftest passed");
        if (timing && count > 0)
        {
            Console.WriteLine($"timing_ms_avg: capture={totalCaptureMs / count} encode={totalEncodeMs / count} decode={totalDecodeMs / count}");
            if (doClipboard && clipboardMs >= 0) Console.WriteLine($"timing_ms: clipboard_first={clipboardMs}");
        }
        return 0;
    }

    private static int RunCheck(string[] args)
    {
        var count = GetInt(args, "--count", 10);
        var includeCursor = GetBool(args, "--cursor", defaultValue: true);
        var maxMs = GetInt(args, "--maxms", 1000);
        var doClipboard = GetBool(args, "--clipboard", defaultValue: true);
        var clipMs = GetInt(args, "--clipms", 400);
        var keep = GetBool(args, "--keep", defaultValue: false);

        var tempDir = Path.Combine(Path.GetTempPath(), "snapcheck");
        Directory.CreateDirectory(tempDir);

        long worst = 0;
        long sum = 0;
        int clipboardOkCount = 0;
        int fileFallbackCount = 0;

        for (int i = 1; i <= count; i++)
        {
            var swTotal = Stopwatch.StartNew();

            var (image, _) = VirtualScreenCapture.CaptureVirtualScreen(new CaptureOptions(includeCursor, 85));

            bool clipboardOk = false;
            if (doClipboard)
            {
                clipboardOk = TrySetClipboardImage(image, TimeSpan.FromMilliseconds(Math.Max(0, clipMs)));
                if (clipboardOk) clipboardOkCount++;
            }

            string? savedPath = null;
            if (!clipboardOk)
            {
                // Fast fallback: encode+write a JPEG so we still complete under budget.
                var bytes = VirtualScreenCapture.EncodeJpeg(image, 85);
                savedPath = Path.Combine(tempDir, $"snapcheck_{DateTime.Now:yyyyMMdd_HHmmss_fff}_{i}.jpg");
                File.WriteAllBytes(savedPath, bytes);
                fileFallbackCount++;
            }

            swTotal.Stop();
            var totalMs = swTotal.ElapsedMilliseconds;
            sum += totalMs;
            worst = Math.Max(worst, totalMs);

            if (totalMs > maxMs)
            {
                Console.WriteLine($"fail {i}/{count}: total={totalMs}ms > {maxMs}ms (clipboardOk={clipboardOk})");
                if (savedPath != null) Console.WriteLine($"saved: {savedPath}");
                return 1;
            }

            Console.WriteLine($"pass {i}/{count}: total={totalMs}ms (clipboardOk={clipboardOk})");

            if (!keep && savedPath != null)
            {
                try { File.Delete(savedPath); } catch { }
            }
        }

        Console.WriteLine($"ok: check passed (max_total_ms={worst}, avg_total_ms={sum / Math.Max(1, count)}, clipboard_ok={clipboardOkCount}, file_fallback={fileFallbackCount})");
        return 0;
    }

    private static bool TrySetClipboardImage(BitmapSource image, TimeSpan timeout)
    {
        return ClipboardImageWriter.TrySetImage(image, timeout, out var err) && err == null;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine("Run `snapcheck --help` for usage.");
        return 2;
    }

    private static string? GetOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }
        return null;
    }

    private static int GetInt(string[] args, string name, int defaultValue)
    {
        var v = GetOption(args, name);
        return int.TryParse(v, out var parsed) ? parsed : defaultValue;
    }

    private static bool GetBool(string[] args, string name, bool defaultValue)
    {
        var v = GetOption(args, name);
        if (v == null) return defaultValue;
        return v is "1" or "true" or "yes" or "y" or "on";
    }
}
