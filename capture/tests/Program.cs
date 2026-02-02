using capture.Core;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Windows.Forms;
using DrawingRectangle = System.Drawing.Rectangle;

namespace WindowCapture.Tests
{
    /// <summary>
    /// 窗口捕获 CLI 测试工具
    /// 
    /// 使用方法：
    /// dotnet run --project tests\WindowCapture.Tests.csproj [command] [args]
    /// 
    /// 命令：
    /// cursor - 获取鼠标位置
    /// window - 获取鼠标位置的窗口信息
    /// deep - 深度搜索鼠标位置的控件
    /// tree [handle] - 显示窗口树（可选指定句柄）
    /// monitor [interval] - 持续监控模式（默认 500ms）
    /// matchscreen [template] [threshold] [--no-canny] [--all] [--monitor N] [--save path] - 截屏 + 模板匹配
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            var command = args[0].ToLower();
            FlaUIWindowCapture? capture = null;

            try
            {
                if (command != "match" && command != "matchscreen")
                    capture = new FlaUIWindowCapture();

                switch (command)
                {
                    case "cursor":
                        TestCursor(capture!);
                        break;
                    case "window":
                        TestWindow(capture!, false);
                        break;
                    case "deep":
                        TestWindow(capture!, true);
                        break;
                    case "tree":
                        var handle = args.Length > 1 ? args[1] : null;
                        TestTree(capture!, handle);
                        break;
                    case "monitor":
                        var interval = args.Length > 1 ? int.Parse(args[1]) : 500;
                        MonitorMode(capture!, interval);
                        break;
                    case "match":
                        {
                            var imagePath = args.Length > 1 ? args[1] : "20260131-170700.jpg";
                            var templatePath = args.Length > 2 ? args[2] : "pic_template1.png";
                            var threshold = args.Length > 3 ? double.Parse(args[3], CultureInfo.InvariantCulture) : 0.80;
                            var useCanny = !args.Any(a => a.Equals("--no-canny", StringComparison.OrdinalIgnoreCase));

                            TestMatch(imagePath, templatePath, threshold, useCanny);
                            break;
                        }
                    case "matchscreen":
                        TestMatchScreen(args.Skip(1).ToArray());
                        break;
                    default:
                        Console.WriteLine($"未知命令: {command}");
                        ShowHelp();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"错误: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                capture?.Dispose();
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("窗口捕获 CLI 测试工具");
            Console.WriteLine();
            Console.WriteLine("命令:");
            Console.WriteLine("  cursor                  - 获取鼠标位置");
            Console.WriteLine("  window                  - 获取鼠标位置的窗口信息");
            Console.WriteLine("  deep                    - 深度搜索鼠标位置的控件");
            Console.WriteLine("  tree [handle]           - 显示窗口树");
            Console.WriteLine("  monitor [interval_ms]   - 持续监控模式（按 Ctrl+C 退出）");
            Console.WriteLine("  match [image] [template] [threshold] [--no-canny] - OpenCV template match (default uses Canny)");
            Console.WriteLine("  matchscreen [template] [threshold] [--no-canny] [--all] [--monitor N] [--save path] - 截屏 + 模板匹配");
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  dotnet run --project tests\\WindowCapture.Tests.csproj cursor");
            Console.WriteLine("  dotnet run --project tests\\WindowCapture.Tests.csproj window");
            Console.WriteLine("  dotnet run --project tests\\WindowCapture.Tests.csproj deep");
            Console.WriteLine("  dotnet run --project tests\\WindowCapture.Tests.csproj tree");
            Console.WriteLine("  dotnet run --project tests\\WindowCapture.Tests.csproj monitor 1000");
            Console.WriteLine("  dotnet run --project tests\\WindowCapture.Tests.csproj match");
            Console.WriteLine("  dotnet run --project tests\\WindowCapture.Tests.csproj matchscreen pic_template1.png 0.80 --all");
        }

        static void TestMatch(string imagePath, string templatePath, double threshold, bool useCanny)
        {
            var img = Path.GetFullPath(imagePath, Directory.GetCurrentDirectory());
            var tpl = Path.GetFullPath(templatePath, Directory.GetCurrentDirectory());

            Console.WriteLine("=== OpenCV Template Match ===");
            Console.WriteLine($"Image:    {img}");
            Console.WriteLine($"Template: {tpl}");
            Console.WriteLine($"Threshold: {threshold.ToString(CultureInfo.InvariantCulture)}");
            Console.WriteLine($"Canny: {useCanny}");
            Console.WriteLine();

            var options = new TemplateMatchOptions
            {
                Threshold = threshold,
                UseCannyEdges = useCanny,
                UseGrayscale = true,
            };

            var result = OpenCvTemplateMatcher.MatchFile(img, tpl, options);

            Console.WriteLine($"Score: {result.Score.ToString("0.0000", CultureInfo.InvariantCulture)}");
            Console.WriteLine($"Location: X={result.Location.X}, Y={result.Location.Y}");
            Console.WriteLine($"MatchRect: Left={result.MatchRect.Left}, Top={result.MatchRect.Top}, Width={result.MatchRect.Width}, Height={result.MatchRect.Height}");
            Console.WriteLine(result.IsMatch(threshold) ? "OK: matched" : "FAIL: below threshold");
        }

        sealed class ScreenCaptureResult
        {
            public required Mat Image { get; init; }
            public required DrawingRectangle Bounds { get; init; }
            public required string Description { get; init; }
        }

        static void TestMatchScreen(string[] args)
        {
            // Positional args: [template] [threshold] (skip flag values like --save <path>)
            var positional = new List<string>();
            for (var i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (a.StartsWith("--", StringComparison.Ordinal))
                {
                    if (a.Equals("--monitor", StringComparison.OrdinalIgnoreCase) || a.Equals("--save", StringComparison.OrdinalIgnoreCase))
                        i++; // skip value
                    continue;
                }

                positional.Add(a);
            }

            var templatePath = positional.Count >= 1 ? positional[0] : "pic_template1.png";
            var threshold = 0.80;
            if (positional.Count >= 2 && !double.TryParse(positional[1], NumberStyles.Float, CultureInfo.InvariantCulture, out threshold))
                throw new ArgumentException($"Invalid threshold: {positional[1]}");

            var useCanny = !args.Any(a => a.Equals("--no-canny", StringComparison.OrdinalIgnoreCase));
            var captureAll = args.Any(a => a.Equals("--all", StringComparison.OrdinalIgnoreCase));

            int? monitorIndex = null;
            for (var i = 0; i < args.Length; i++)
            {
                if (!args[i].Equals("--monitor", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (i + 1 >= args.Length)
                    throw new ArgumentException("--monitor requires an index");

                monitorIndex = int.Parse(args[i + 1], CultureInfo.InvariantCulture);
                break;
            }

            string? savePath = null;
            for (var i = 0; i < args.Length; i++)
            {
                if (!args[i].Equals("--save", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (i + 1 >= args.Length)
                    throw new ArgumentException("--save requires a file path");

                savePath = args[i + 1];
                break;
            }

            var resolvedTemplatePath = ResolveExistingPath(templatePath) ?? Path.GetFullPath(templatePath, Directory.GetCurrentDirectory());
            if (!File.Exists(resolvedTemplatePath))
                throw new FileNotFoundException($"Template file not found: {templatePath}", templatePath);

            var options = new TemplateMatchOptions
            {
                Threshold = threshold,
                UseGrayscale = true,
                UseCannyEdges = useCanny,
            };

            var capture = CaptureTargetScreen(captureAll, monitorIndex);
            using var screenMat = capture.Image;

            if (!string.IsNullOrWhiteSpace(savePath))
            {
                var fullSavePath = Path.GetFullPath(savePath, Directory.GetCurrentDirectory());
                using var bmp = BitmapConverter.ToBitmap(screenMat);
                bmp.Save(fullSavePath, ImageFormat.Png);
                Console.WriteLine($"Saved screenshot: {fullSavePath}");
            }

            using var templateMat = Cv2.ImRead(resolvedTemplatePath, ImreadModes.Color);
            if (templateMat.Empty())
                throw new FileNotFoundException($"Failed to read template: {resolvedTemplatePath}", resolvedTemplatePath);

            var result = OpenCvTemplateMatcher.Match(screenMat, templateMat, options);

            // Convert from captured-image coordinates to absolute screen coordinates.
            var absX = capture.Bounds.Left + result.Location.X;
            var absY = capture.Bounds.Top + result.Location.Y;
            var centerX = absX + result.MatchRect.Width / 2;
            var centerY = absY + result.MatchRect.Height / 2;

            Console.WriteLine("=== OpenCV Match From Screen ===");
            Console.WriteLine($"Capture: {capture.Description}");
            Console.WriteLine($"Bounds: Left={capture.Bounds.Left}, Top={capture.Bounds.Top}, Width={capture.Bounds.Width}, Height={capture.Bounds.Height}");
            Console.WriteLine($"Template: {resolvedTemplatePath}");
            Console.WriteLine($"Threshold: {threshold.ToString(CultureInfo.InvariantCulture)}; Canny: {useCanny}");
            Console.WriteLine();
            Console.WriteLine($"Score: {result.Score.ToString("0.0000", CultureInfo.InvariantCulture)}");
            Console.WriteLine($"ImageLocation: X={result.Location.X}, Y={result.Location.Y}");
            Console.WriteLine($"ScreenLocation: X={absX}, Y={absY}");
            Console.WriteLine($"ScreenCenter: X={centerX}, Y={centerY}");
            Console.WriteLine(result.IsMatch(threshold) ? "OK: matched" : "FAIL: below threshold");
        }

        static ScreenCaptureResult CaptureTargetScreen(bool captureAll, int? monitorIndex)
        {
            if (captureAll && monitorIndex != null)
                throw new ArgumentException("Use either --all or --monitor, not both");

            if (captureAll)
            {
                var virtualBounds = SystemInformation.VirtualScreen;
                return new ScreenCaptureResult
                {
                    Bounds = virtualBounds,
                    Description = "VirtualScreen (all monitors)",
                    Image = CaptureScreenToMat(virtualBounds),
                };
            }

            if (monitorIndex != null)
            {
                var screens = Screen.AllScreens;
                if (monitorIndex < 0 || monitorIndex >= screens.Length)
                    throw new ArgumentOutOfRangeException(nameof(monitorIndex), $"Monitor index out of range. Available: 0..{screens.Length - 1}");

                var s = screens[monitorIndex.Value];
                return new ScreenCaptureResult
                {
                    Bounds = s.Bounds,
                    Description = $"Monitor[{monitorIndex.Value}] {s.DeviceName}",
                    Image = CaptureScreenToMat(s.Bounds),
                };
            }

            // Default: current monitor (mouse cursor)
            var cursor = Cursor.Position;
            var screen = Screen.FromPoint(cursor);
            return new ScreenCaptureResult
            {
                Bounds = screen.Bounds,
                Description = $"CurrentMonitor {screen.DeviceName} (Cursor at {cursor.X},{cursor.Y})",
                Image = CaptureScreenToMat(screen.Bounds),
            };
        }

        static Mat CaptureScreenToMat(DrawingRectangle bounds)
        {
            using var bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, new System.Drawing.Size(bounds.Width, bounds.Height), CopyPixelOperation.SourceCopy);
            }

            // Clone so the returned Mat does not depend on Bitmap lifetime.
            using var tmp = BitmapConverter.ToMat(bmp);
            return tmp.Clone();
        }

        static string? ResolveExistingPath(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            if (File.Exists(input))
                return Path.GetFullPath(input);

            // Allow providing a bare filename like "pic_template1.png" when running from bin/...
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, input);
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }

        static void TestCursor(IWindowCapture capture)
        {
            Console.WriteLine("=== 鼠标位置测试 ===");
            Console.WriteLine("将在 3 秒后获取鼠标位置，请移动鼠标到目标位置...");
            Thread.Sleep(3000);

            var pos = capture.GetCursorPosition();
            Console.WriteLine($"鼠标位置: X={pos.X}, Y={pos.Y}");
        }

        static void TestWindow(IWindowCapture capture, bool deepSearch)
        {
            Console.WriteLine($"=== 窗口信息测试 (深度搜索: {deepSearch}) ===");
            Console.WriteLine("将在 3 秒后捕获鼠标位置的窗口，请移动鼠标到目标窗口...");
            Thread.Sleep(3000);

            var pos = capture.GetCursorPosition();
            Console.WriteLine($"鼠标位置: X={pos.X}, Y={pos.Y}");
            Console.WriteLine();

            var window = capture.GetWindowFromPoint(pos, deepSearch);
            if (window == null)
            {
                Console.WriteLine("未找到窗口");
                return;
            }

            PrintWindowInfo(window);

            // 获取根窗口
            var root = capture.GetRootWindow(window);
            if (root != null && !capture.IsSameWindow(window, root))
            {
                Console.WriteLine();
                Console.WriteLine("=== 根窗口信息 ===");
                PrintWindowInfo(root);
            }

            // 计算相对坐标
            var clientPos = capture.ScreenToClient(window, pos);
            Console.WriteLine();
            Console.WriteLine($"窗口内坐标: X={clientPos.X}, Y={clientPos.Y}");
        }

        static void TestTree(IWindowCapture capture, string? handleStr)
        {
            WindowInfo? window;

            if (string.IsNullOrEmpty(handleStr))
            {
                Console.WriteLine("=== 窗口树测试 ===");
                Console.WriteLine("将在 3 秒后捕获鼠标位置的窗口，请移动鼠标到目标窗口...");
                Thread.Sleep(3000);

                var pos = capture.GetCursorPosition();
                window = capture.GetWindowFromPoint(pos, false);
                
                if (window != null)
                {
                    window = capture.GetRootWindow(window);
                }
            }
            else
            {
                Console.WriteLine($"=== 窗口树测试 (句柄: {handleStr}) ===");
                window = capture.GetWindowInfo(handleStr);
            }

            if (window == null)
            {
                Console.WriteLine("未找到窗口");
                return;
            }

            Console.WriteLine($"根窗口: {window.Title} (句柄: {window.Handle})");
            Console.WriteLine();

            var tree = capture.BuildWindowTree(window);
            if (tree == null)
            {
                Console.WriteLine("无法构建窗口树");
                return;
            }

            PrintTree(tree, 0);
        }

        static void MonitorMode(IWindowCapture capture, int interval)
        {
            Console.WriteLine($"=== 持续监控模式 (间隔: {interval}ms) ===");
            Console.WriteLine("按 Ctrl+C 退出");
            Console.WriteLine();

            var lastHandle = "";

            while (true)
            {
                try
                {
                    var pos = capture.GetCursorPosition();
                    var window = capture.GetWindowFromPoint(pos, true);

                    if (window != null && window.Handle != lastHandle)
                    {
                        lastHandle = window.Handle;
                        Console.Clear();
                        Console.WriteLine($"=== 监控中 (间隔: {interval}ms) === 按 Ctrl+C 退出");
                        Console.WriteLine($"鼠标位置: X={pos.X}, Y={pos.Y}");
                        Console.WriteLine();
                        PrintWindowInfo(window);

                        var root = capture.GetRootWindow(window);
                        if (root != null && !capture.IsSameWindow(window, root))
                        {
                            Console.WriteLine();
                            Console.WriteLine("--- 根窗口 ---");
                            Console.WriteLine($"标题: {root.Title}");
                            Console.WriteLine($"句柄: {root.Handle}");
                            Console.WriteLine($"类名: {root.ClassName}");
                        }
                    }

                    Thread.Sleep(interval);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"错误: {ex.Message}");
                    Thread.Sleep(interval);
                }
            }
        }

        static void PrintWindowInfo(WindowInfo window)
        {
            Console.WriteLine($"句柄:       {window.Handle} ({window.NativeWindowHandle.ToInt64()})");
            Console.WriteLine($"标题:       {window.Title}");
            Console.WriteLine($"类名:       {window.ClassName}");
            Console.WriteLine($"控件类型:   {window.ControlType}");
            Console.WriteLine($"位置:       Left={window.Bounds.Left}, Top={window.Bounds.Top}, Right={window.Bounds.Right}, Bottom={window.Bounds.Bottom}");
            Console.WriteLine($"大小:       {window.Bounds.Width} x {window.Bounds.Height}");
            Console.WriteLine($"进程ID:     {window.ProcessId}");
            Console.WriteLine($"进程名称:   {window.ProcessName}");
            Console.WriteLine($"父句柄:     {window.ParentHandle ?? "无"}");
            Console.WriteLine($"可见:       {window.IsVisible}");
            Console.WriteLine($"样式:       {window.StyleDescription}");
            
            if (!string.IsNullOrEmpty(window.AutomationId))
                Console.WriteLine($"AutomationId: {window.AutomationId}");
            
            if (!string.IsNullOrEmpty(window.FrameworkId))
                Console.WriteLine($"框架:       {window.FrameworkId}");
            
            if (window.SupportedPatterns.Count > 0)
                Console.WriteLine($"Patterns:   {string.Join(", ", window.SupportedPatterns)}");
            
            if (!string.IsNullOrEmpty(window.HelpText))
                Console.WriteLine($"帮助文本:   {window.HelpText}");
        }

        static void PrintTree(WindowTreeNode node, int depth)
        {
            var indent = new string(' ', depth * 2);
            Console.WriteLine($"{indent}{node.DisplayText}");

            foreach (var child in node.Children)
            {
                PrintTree(child, depth + 1);
            }
        }
    }
}
