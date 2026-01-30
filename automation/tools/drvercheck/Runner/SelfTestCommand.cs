using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using VirtualHidSimulator.Core.Drivers;
using VirtualHidSimulator.Core.HidDefinitions;
using drvercheck.Probe;

namespace drvercheck.Runner;

internal static class SelfTestCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        var driverName = GetOption(args, "--driver") ?? "SendInput";
        var pipeName = GetOption(args, "--pipe") ?? $"drvercheck_{Guid.NewGuid():N}";
        var json = args.Any(a => a.Equals("--json", StringComparison.OrdinalIgnoreCase));
        var config = GetOption(args, "--config");

        var report = new TestReport { Driver = driverName, StartedAt = DateTimeOffset.Now };

        using var probeProcess = StartProbe(pipeName);
        await using var probe = await PipeClient.ConnectAsync(pipeName, TimeSpan.FromSeconds(15));

        await EnsureProbeForegroundAsync(probe);

        IInputDriver driver;
        if (driverName.Equals("Interception", StringComparison.OrdinalIgnoreCase))
        {
            var kernel = InterceptionEnvironment.GetKernelDriverStatus();
            if (!kernel.KeyboardServiceExists || !kernel.MouseServiceExists)
            {
                report.Results.Add(new TestResult("initialize", "skip",
                    "Interception kernel driver services not detected (sc query keyboard/mouse failed). Install the Interception driver and reboot."));
                return Finish(report, json, probe, probeProcess);
            }

            if (!InterceptionEnvironment.TryLoadInterceptionDll(out _))
            {
                var dllPath = InterceptionEnvironment.EnsureInterceptionDllPresent(config);
                if (dllPath == null)
                {
                    report.Results.Add(new TestResult("initialize", "skip",
                        "interception.dll not found in process DLL search path, and no local copy source was found. Build the main app once or place interception.dll next to drvercheck."));
                    return Finish(report, json, probe, probeProcess);
                }
            }

            driver = new InterceptionDriver();
        }
        else
        {
            driver = new SendInputDriver();
        }

        if (!driver.Initialize() || !driver.IsAvailable)
        {
            report.Results.Add(new TestResult("initialize", "skip",
                "Driver not available. For Interception, ensure interception.dll is present and the driver is installed."));
            report.FinishedAt = DateTimeOffset.Now;
            if (json) WriteJson(report);
            else WriteHuman(report);
            TryShutdownProbe(probe, probeProcess);
            return 0;
        }

        report.Results.Add(new TestResult("initialize", "pass"));

        await RunCase(report, probe, driver, "unicode_basic", async () =>
        {
            await probe.SendAsync("clear");
            await EnsureProbeForegroundAsync(probe);

            driver.SendUnicode('a');
            var snap = await WaitForAsync(probe, s =>
                    s.Text.EndsWith("a", StringComparison.Ordinal) || HasVk(s, "ll_key_", VirtualKeyCodes.VK_A) || HasUnicode(s, 'a'),
                TimeSpan.FromSeconds(2));

            // Prefer end-to-end (textbox) if possible; otherwise accept evidence from low-level hooks.
            AssertTrue(
                snap.Text.EndsWith("a", StringComparison.Ordinal)
                || (driver is SendInputDriver && HasUnicode(snap, 'a'))
                || (driver is InterceptionDriver && HasVk(snap, "ll_key_", VirtualKeyCodes.VK_A)),
                $"Expected textbox to end with 'a' or observe injected key evidence. Text: '{snap.Text}'");
        });

        await RunCase(report, probe, driver, "unicode_unmappable", async () =>
        {
            await probe.SendAsync("clear");
            await EnsureProbeForegroundAsync(probe);

            driver.SendUnicode('中');
            var snap = await WaitForAsync(probe, s => s.Text.Contains('中') || HasUnicode(s, '中'), TimeSpan.FromSeconds(3));
            AssertTrue(
                snap.Text.Contains('中') || HasUnicode(snap, '中'),
                $"Expected textbox to contain '中' or observe injected unicode evidence. Text: '{snap.Text}'");
        });

        await RunCase(report, probe, driver, "key_down_up_A", async () =>
        {
            await probe.SendAsync("clear");
            await EnsureProbeForegroundAsync(probe);

            driver.KeyDown(VirtualKeyCodes.VK_A);
            driver.KeyUp(VirtualKeyCodes.VK_A);
            var snap = await WaitForAsync(
                probe,
                s => HasKind(s, "ll_key_down", VirtualKeyCodes.VK_A) && HasKind(s, "ll_key_up", VirtualKeyCodes.VK_A),
                TimeSpan.FromSeconds(2));
            AssertTrue(HasKind(snap, "ll_key_down", VirtualKeyCodes.VK_A), "Expected ll_key_down for VK_A.");
            AssertTrue(HasKind(snap, "ll_key_up", VirtualKeyCodes.VK_A), "Expected ll_key_up for VK_A.");
        });

        await RunCase(report, probe, driver, "mouse_click", async () =>
        {
            await probe.SendAsync("clear");
            await EnsureProbeForegroundAsync(probe);

            var before = await probe.SnapshotAsync();
            driver.MouseMoveTo(before.TargetPoint.X, before.TargetPoint.Y);
            await Task.Delay(150);
            driver.MouseButtonDown(MouseButton.Left);
            driver.MouseButtonUp(MouseButton.Left);
            var after = await WaitForAsync(
                probe,
                s => s.Events.Any(e => e.Kind == "ll_mouse_left_down") && s.Events.Any(e => e.Kind == "ll_mouse_left_up"),
                TimeSpan.FromSeconds(2));
            AssertTrue(after.Events.Any(e => e.Kind == "ll_mouse_left_down"), "Expected ll_mouse_left_down.");
            AssertTrue(after.Events.Any(e => e.Kind == "ll_mouse_left_up"), "Expected ll_mouse_left_up.");
        });

        report.FinishedAt = DateTimeOffset.Now;

        if (json) WriteJson(report);
        else WriteHuman(report);

        TryShutdownProbe(probe, probeProcess);

        return report.FailedCount == 0 ? 0 : 1;
    }

    private static int Finish(TestReport report, bool json, PipeClient probe, Process probeProcess)
    {
        report.FinishedAt = DateTimeOffset.Now;
        if (json) WriteJson(report);
        else WriteHuman(report);
        TryShutdownProbe(probe, probeProcess);
        return 0;
    }

    private static async Task RunCase(TestReport report, PipeClient probe, IInputDriver driver, string name, Func<Task> run)
    {
        try
        {
            await run();
            report.Results.Add(new TestResult(name, "pass"));
        }
        catch (Exception ex)
        {
            report.Results.Add(new TestResult(name, "fail", ex.Message));
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task EnsureProbeForegroundAsync(PipeClient probe)
    {
        // Ask probe to try from its side first.
        await probe.SendAsync("focus");
        await Task.Delay(150);

        // Then force foreground from the runner side (foreground process).
        var snap = await probe.SnapshotAsync();
        if (snap.WindowHandle != 0)
        {
            try
            {
                AllowSetForegroundWindow(snap.ProcessId);
            }
            catch
            {
                // ignore
            }

            try
            {
                var hwnd = new IntPtr(snap.WindowHandle);
                ShowWindow(hwnd, SW_RESTORE);
                SetForegroundWindow(hwnd);
                BringWindowToTop(hwnd);
            }
            catch
            {
                // ignore
            }
        }

        await probe.SendAsync("focus");
        await Task.Delay(200);
    }

    private static async Task<ProbeSnapshot> WaitForAsync(PipeClient probe, Func<ProbeSnapshot, bool> predicate, TimeSpan timeout)
    {
        var start = DateTimeOffset.Now;
        ProbeSnapshot last = await probe.SnapshotAsync();

        while (DateTimeOffset.Now - start < timeout)
        {
            if (predicate(last)) return last;
            await Task.Delay(100);
            last = await probe.SnapshotAsync();
        }

        return last;
    }

    private static bool HasVk(ProbeSnapshot snap, string kindPrefix, ushort vk)
    {
        return snap.Events.Any(e => e.Kind.StartsWith(kindPrefix, StringComparison.Ordinal) && e.WParam == vk);
    }

    private static bool HasKind(ProbeSnapshot snap, string kind, ushort vk)
    {
        return snap.Events.Any(e => e.Kind == kind && e.WParam == vk);
    }

    private static bool HasUnicode(ProbeSnapshot snap, char c)
    {
        const long VkPacket = 0xE7;
        var code = (ushort)c;

        return snap.Events.Any(e =>
        {
            if (!e.Kind.StartsWith("ll_key", StringComparison.Ordinal)) return false;

            // Either VK_PACKET-based injection...
            if (e.WParam == VkPacket) return true;

            // ...or the scan code carries the unicode (KEYEVENTF_UNICODE path).
            var scanCode = (e.LParam >> 16) & 0xFFFF;
            return scanCode == code;
        });
    }

    private static Process StartProbe(string pipeName)
    {
        var exe = Environment.ProcessPath ?? throw new InvalidOperationException("ProcessPath not available.");

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = $"probe --pipe {pipeName}",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        return Process.Start(psi) ?? throw new InvalidOperationException("Failed to start probe process.");
    }

    private static void TryShutdownProbe(PipeClient probe, Process probeProcess)
    {
        try { probe.SendAsync("shutdown").GetAwaiter().GetResult(); } catch { }
        try
        {
            if (!probeProcess.HasExited)
            {
                probeProcess.WaitForExit(3000);
            }
        }
        catch { }
    }

    private static void WriteHuman(TestReport report)
    {
        Console.WriteLine($"Driver: {report.Driver}");
        foreach (var r in report.Results)
        {
            var suffix = string.IsNullOrWhiteSpace(r.Message) ? "" : $" - {r.Message}";
            Console.WriteLine($"  [{r.Status}] {r.Name}{suffix}");
        }
        Console.WriteLine($"Summary: pass={report.PassedCount} fail={report.FailedCount} skip={report.SkippedCount}");
    }

    private static void WriteJson(TestReport report)
    {
        var opts = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        Console.WriteLine(JsonSerializer.Serialize(report, opts));
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

    private const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);
}
