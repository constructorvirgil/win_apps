using capture.Core;

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
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            using var capture = new FlaUIWindowCapture();

            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            var command = args[0].ToLower();

            try
            {
                switch (command)
                {
                    case "cursor":
                        TestCursor(capture);
                        break;
                    case "window":
                        TestWindow(capture, false);
                        break;
                    case "deep":
                        TestWindow(capture, true);
                        break;
                    case "tree":
                        var handle = args.Length > 1 ? args[1] : null;
                        TestTree(capture, handle);
                        break;
                    case "monitor":
                        var interval = args.Length > 1 ? int.Parse(args[1]) : 500;
                        MonitorMode(capture, interval);
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
            Console.WriteLine();
            Console.WriteLine("示例:");
            Console.WriteLine("  dotnet run --project tests\\WindowCapture.Tests.csproj cursor");
            Console.WriteLine("  dotnet run --project tests\\WindowCapture.Tests.csproj window");
            Console.WriteLine("  dotnet run --project tests\\WindowCapture.Tests.csproj deep");
            Console.WriteLine("  dotnet run --project tests\\WindowCapture.Tests.csproj tree");
            Console.WriteLine("  dotnet run --project tests\\WindowCapture.Tests.csproj monitor 1000");
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
