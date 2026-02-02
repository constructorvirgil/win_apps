using System.Diagnostics;
using System.Runtime.InteropServices;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace capture.Core
{
    /// <summary>
    /// FlaUI 实现的窗口捕获
    /// </summary>
    public class FlaUIWindowCapture : IWindowCapture, IDisposable
    {
        private readonly UIA3Automation _automation;

        #region Windows API for cursor position
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }
        #endregion

        public FlaUIWindowCapture()
        {
            _automation = new UIA3Automation();
        }

        public Point GetCursorPosition()
        {
            GetCursorPos(out POINT point);
            return new Point(point.X, point.Y);
        }

        public WindowInfo? GetWindowFromPoint(Point screenPoint, bool deepSearch = false)
        {
            try
            {
                var element = _automation.FromPoint(new System.Drawing.Point(screenPoint.X, screenPoint.Y));
                if (element == null)
                {
                    System.Diagnostics.Debug.WriteLine($"FlaUI: FromPoint returned null at ({screenPoint.X}, {screenPoint.Y})");
                    return null;
                }

                // 如果需要深度搜索，尝试获取最深层的元素
                if (deepSearch)
                {
                    var deepestElement = GetDeepestElementAtPoint(element, screenPoint);
                    if (deepestElement != null)
                        element = deepestElement;
                }

                var result = ConvertToWindowInfo(element);
                if (result == null)
                {
                    System.Diagnostics.Debug.WriteLine($"FlaUI: ConvertToWindowInfo returned null");
                }
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FlaUI: GetWindowFromPoint exception: {ex.Message}");
                return null;
            }
        }

        public WindowInfo? GetRootWindow(WindowInfo window)
        {
            try
            {
                var element = _automation.FromHandle(window.NativeWindowHandle);
                if (element == null)
                    return null;

                // 获取顶级窗口
                var parent = element;
                while (true)
                {
                    try
                    {
                        var nextParent = parent.Parent;
                        if (nextParent == null || nextParent.ControlType == FlaUI.Core.Definitions.ControlType.Pane)
                            break;
                        parent = nextParent;
                    }
                    catch
                    {
                        break;
                    }
                }

                return ConvertToWindowInfo(parent);
            }
            catch
            {
                return window; // 如果失败，返回原窗口
            }
        }

        public Point ScreenToClient(WindowInfo window, Point screenPoint)
        {
            try
            {
                var element = _automation.FromHandle(window.NativeWindowHandle);
                if (element == null)
                    return screenPoint;

                var rect = element.BoundingRectangle;
                return new Point(
                    screenPoint.X - (int)rect.Left,
                    screenPoint.Y - (int)rect.Top
                );
            }
            catch
            {
                return screenPoint;
            }
        }

        public WindowInfo? GetWindowInfo(string handle)
        {
            try
            {
                if (IntPtr.TryParse(handle.Replace("0x", ""), 
                    System.Globalization.NumberStyles.HexNumber, 
                    null, out IntPtr hwnd))
                {
                    return GetWindowInfoFromNativeHandle(hwnd);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public WindowInfo? GetWindowInfoFromNativeHandle(IntPtr handle)
        {
            try
            {
                var element = _automation.FromHandle(handle);
                return element == null ? null : ConvertToWindowInfo(element);
            }
            catch
            {
                return null;
            }
        }

        public List<WindowInfo> GetChildWindows(WindowInfo window)
        {
            var result = new List<WindowInfo>();
            try
            {
                var element = _automation.FromHandle(window.NativeWindowHandle);
                if (element == null)
                    return result;

                var children = element.FindAllChildren();
                foreach (var child in children)
                {
                    try
                    {
                        var childInfo = ConvertToWindowInfo(child);
                        if (childInfo != null)
                            result.Add(childInfo);
                    }
                    catch
                    {
                        // 跳过无法转换的子元素
                    }
                }
            }
            catch
            {
                // 返回空列表
            }

            return result;
        }

        public WindowTreeNode? BuildWindowTree(WindowInfo rootWindow)
        {
            try
            {
                var element = _automation.FromHandle(rootWindow.NativeWindowHandle);
                if (element == null)
                    return null;

                return BuildTreeNode(element);
            }
            catch
            {
                return null;
            }
        }

        public bool IsSameWindow(WindowInfo window1, WindowInfo window2)
        {
            return window1.NativeWindowHandle == window2.NativeWindowHandle;
        }

        private WindowTreeNode BuildTreeNode(AutomationElement element)
        {
            var node = new WindowTreeNode
            {
                Handle = $"0x{element.Properties.NativeWindowHandle.ValueOrDefault.ToInt64():X}",
                NativeWindowHandle = element.Properties.NativeWindowHandle.ValueOrDefault,
                DisplayText = BuildDisplayText(element),
                ToolTipText = BuildToolTipText(element)
            };

            try
            {
                var children = element.FindAllChildren();
                foreach (var child in children)
                {
                    try
                    {
                        node.Children.Add(BuildTreeNode(child));
                    }
                    catch
                    {
                        // 跳过无法处理的子元素
                    }
                }
            }
            catch
            {
                // 无子元素
            }

            return node;
        }

        private string BuildDisplayText(AutomationElement element)
        {
            var handle = element.Properties.NativeWindowHandle.ValueOrDefault;
            var className = element.Properties.ClassName.ValueOrDefault ?? "Unknown";
            var name = element.Properties.Name.ValueOrDefault ?? "";
            var controlType = element.Properties.ControlType.ValueOrDefault.ToString().Replace("ControlType.", "");
            var isOffscreen = element.Properties.IsOffscreen.ValueOrDefault;

            var text = $"0x{handle.ToInt64():X} [{controlType}:{className}]";
            if (!string.IsNullOrEmpty(name))
            {
                text += $" \"{(name.Length > 30 ? name.Substring(0, 30) + "..." : name)}\"";
            }
            if (isOffscreen)
            {
                text += " [离屏]";
            }

            return text;
        }

        private string BuildToolTipText(AutomationElement element)
        {
            var handle = element.Properties.NativeWindowHandle.ValueOrDefault;
            var className = element.Properties.ClassName.ValueOrDefault ?? "Unknown";
            var name = element.Properties.Name.ValueOrDefault ?? "(无)";
            var controlType = element.Properties.ControlType.ValueOrDefault;
            var automationId = element.Properties.AutomationId.ValueOrDefault ?? "(无)";
            var frameworkId = element.Properties.FrameworkId.ValueOrDefault ?? "(无)";
            
            var tooltip = $"句柄: 0x{handle.ToInt64():X} ({handle.ToInt64()})\n" +
                         $"类名: {className}\n" +
                         $"名称: {name}\n" +
                         $"控件类型: {controlType}\n" +
                         $"AutomationId: {automationId}\n" +
                         $"框架: {frameworkId}";

            try
            {
                var rect = element.BoundingRectangle;
                tooltip += $"\n位置: ({rect.Left}, {rect.Top})\n" +
                          $"大小: {rect.Width} x {rect.Height}";
            }
            catch
            {
                // 无法获取位置信息
            }

            return tooltip;
        }

        private AutomationElement? GetDeepestElementAtPoint(AutomationElement startElement, Point screenPoint)
        {
            try
            {
                var current = startElement;
                var point = new System.Drawing.Point(screenPoint.X, screenPoint.Y);

                while (true)
                {
                    var children = current.FindAllChildren();
                    AutomationElement? deepest = null;

                    foreach (var child in children)
                    {
                        try
                        {
                            var rect = child.BoundingRectangle;
                            if (rect.Contains(point))
                            {
                                deepest = child;
                                break;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if (deepest == null)
                        break;

                    current = deepest;
                }

                return current;
            }
            catch
            {
                return startElement;
            }
        }

        private WindowInfo? ConvertToWindowInfo(AutomationElement element)
        {
            try
            {
                var handle = element.Properties.NativeWindowHandle.ValueOrDefault;
                var bounds = element.BoundingRectangle;
                var processId = element.Properties.ProcessId.ValueOrDefault;

                var info = new WindowInfo
                {
                    Handle = $"0x{handle.ToInt64():X}",
                    NativeWindowHandle = handle,
                    Title = element.Properties.Name.ValueOrDefault ?? "",
                    ClassName = element.Properties.ClassName.ValueOrDefault ?? "",
                    Bounds = new Rectangle(
                        (int)bounds.Left,
                        (int)bounds.Top,
                        (int)bounds.Right,
                        (int)bounds.Bottom
                    ),
                    ProcessId = processId,
                    IsVisible = !element.Properties.IsOffscreen.ValueOrDefault,
                    AutomationId = element.Properties.AutomationId.ValueOrDefault,
                    ControlType = element.Properties.ControlType.ValueOrDefault.ToString().Replace("ControlType.", ""),
                    FrameworkId = element.Properties.FrameworkId.ValueOrDefault,
                    HelpText = element.Properties.HelpText.ValueOrDefault
                };

                // 获取进程名称
                try
                {
                    var process = Process.GetProcessById(processId);
                    info.ProcessName = process.ProcessName;
                }
                catch
                {
                    info.ProcessName = "Unknown";
                }

                // 获取父窗口
                try
                {
                    var parent = element.Parent;
                    if (parent != null && parent.ControlType != FlaUI.Core.Definitions.ControlType.Pane)
                    {
                        info.ParentHandle = $"0x{parent.Properties.NativeWindowHandle.ValueOrDefault.ToInt64():X}";
                    }
                }
                catch
                {
                    // 无父窗口
                }

                // 获取支持的 Patterns
                info.SupportedPatterns = GetSupportedPatterns(element);

                // 构建样式描述
                info.StyleDescription = BuildStyleDescription(element);

                return info;
            }
            catch
            {
                return null;
            }
        }

        private List<string> GetSupportedPatterns(AutomationElement element)
        {
            var patterns = new List<string>();
            
            try
            {
                var supportedPatterns = element.GetSupportedPatterns();
                foreach (var pattern in supportedPatterns)
                {
                    patterns.Add(pattern.ToString().Replace("PatternId.", ""));
                }
            }
            catch
            {
                // 无法获取 patterns
            }

            return patterns;
        }

        private string BuildStyleDescription(AutomationElement element)
        {
            var styles = new List<string>();

            try
            {
                var controlType = element.Properties.ControlType.ValueOrDefault;
                styles.Add($"Type: {controlType.ToString().Replace("ControlType.", "")}");

                if (element.Properties.IsEnabled.ValueOrDefault)
                    styles.Add("Enabled");
                else
                    styles.Add("Disabled");

                if (element.Properties.IsKeyboardFocusable.ValueOrDefault)
                    styles.Add("Focusable");

                if (element.Properties.IsOffscreen.ValueOrDefault)
                    styles.Add("Offscreen");
            }
            catch
            {
                // 无法获取样式信息
            }

            return string.Join(", ", styles);
        }

        public void Dispose()
        {
            _automation?.Dispose();
        }
    }
}
