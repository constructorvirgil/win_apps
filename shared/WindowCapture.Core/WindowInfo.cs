namespace capture.Core
{
    /// <summary>
    /// 窗口信息模型
    /// </summary>
    public class WindowInfo
    {
        /// <summary>
        /// 窗口句柄（IntPtr 或字符串形式的标识符）
        /// </summary>
        public string Handle { get; set; } = string.Empty;

        /// <summary>
        /// 窗口标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 窗口类名
        /// </summary>
        public string ClassName { get; set; } = string.Empty;

        /// <summary>
        /// 窗口位置和大小
        /// </summary>
        public Rectangle Bounds { get; set; }

        /// <summary>
        /// 进程 ID
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// 进程名称
        /// </summary>
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// 父窗口句柄
        /// </summary>
        public string? ParentHandle { get; set; }

        /// <summary>
        /// 是否可见
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// 窗口样式描述
        /// </summary>
        public string StyleDescription { get; set; } = string.Empty;

        /// <summary>
        /// UI Automation ID（FlaUI 特有）
        /// </summary>
        public string? AutomationId { get; set; }

        /// <summary>
        /// 控件类型（FlaUI 特有）
        /// </summary>
        public string? ControlType { get; set; }

        /// <summary>
        /// 框架 ID（FlaUI 特有，如 WinForms、WPF、Win32）
        /// </summary>
        public string? FrameworkId { get; set; }

        /// <summary>
        /// 支持的 Patterns（FlaUI 特有）
        /// </summary>
        public List<string> SupportedPatterns { get; set; } = new();

        /// <summary>
        /// 元素帮助文本
        /// </summary>
        public string? HelpText { get; set; }

        /// <summary>
        /// 原生窗口句柄（IntPtr）
        /// </summary>
        public IntPtr NativeWindowHandle { get; set; }
    }

    /// <summary>
    /// 矩形区域
    /// </summary>
    public struct Rectangle
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }

        public int Width => Right - Left;
        public int Height => Bottom - Top;

        public Rectangle(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public override string ToString()
        {
            return $"({Left}, {Top}, {Right}, {Bottom})";
        }
    }

    /// <summary>
    /// 点坐标
    /// </summary>
    public struct Point
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }

    /// <summary>
    /// 窗口树节点
    /// </summary>
    public class WindowTreeNode
    {
        public string Handle { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
        public string ToolTipText { get; set; } = string.Empty;
        public List<WindowTreeNode> Children { get; set; } = new();
        public IntPtr NativeWindowHandle { get; set; }
    }
}
