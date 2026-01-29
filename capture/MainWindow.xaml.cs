using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace capture
{
    /// <summary>
    /// 窗口树节点
    /// </summary>
    public class WindowTreeItem
    {
        public IntPtr Handle { get; set; }
        public string DisplayText { get; set; } = "";
        public string ToolTipText { get; set; } = "";
        public ObservableCollection<WindowTreeItem> Children { get; set; } = new();
    }

    /// <summary>
    /// 窗口信息获取工具
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer? _timer;
        private IntPtr _lastHandle = IntPtr.Zero;
        private IntPtr _lastControlHandle = IntPtr.Zero;
        private HighlightWindow? _highlightWindow;
        private IntPtr _myWindowHandle = IntPtr.Zero;
        private IntPtr _lastTreeRootHandle = IntPtr.Zero;

        #region Windows API 声明

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr ChildWindowFromPointEx(IntPtr hWndParent, POINT pt, uint uFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr RealChildWindowFromPoint(IntPtr hWndParent, POINT pt);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const uint GA_ROOT = 2;
        private const uint CWP_SKIPINVISIBLE = 0x0001;
        private const uint CWP_SKIPDISABLED = 0x0002;
        private const uint CWP_SKIPTRANSPARENT = 0x0004;
        private const int VK_CONTROL = 0x11;
        private const int VK_SHIFT = 0x10;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion

        #region 窗口样式常量

        // Window Styles
        private const int WS_OVERLAPPED = 0x00000000;
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WS_CHILD = 0x40000000;
        private const int WS_MINIMIZE = 0x20000000;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_DISABLED = 0x08000000;
        private const int WS_CLIPSIBLINGS = 0x04000000;
        private const int WS_CLIPCHILDREN = 0x02000000;
        private const int WS_MAXIMIZE = 0x01000000;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_BORDER = 0x00800000;
        private const int WS_DLGFRAME = 0x00400000;
        private const int WS_VSCROLL = 0x00200000;
        private const int WS_HSCROLL = 0x00100000;
        private const int WS_SYSMENU = 0x00080000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_MINIMIZEBOX = 0x00020000;
        private const int WS_MAXIMIZEBOX = 0x00010000;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            InitializeTimer();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 获取本窗口句柄，用于排除自身
            _myWindowHandle = new WindowInteropHelper(this).Handle;
            
            // 初始化高亮窗口
            _highlightWindow = new HighlightWindow();
            _highlightWindow.Owner = this;
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // 每50毫秒更新一次
            };
            _timer.Tick += Timer_Tick;
        }

        private void CaptureToggle_Click(object sender, RoutedEventArgs e)
        {
            if (CaptureToggle.IsChecked == true)
            {
                _timer?.Start();
            }
            else
            {
                _timer?.Stop();
                _highlightWindow?.Hide();
                ClearInfo();
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // 检查是否按住了Shift键（停止捕获并保留信息）
                bool shiftPressed = (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
                if (shiftPressed)
                {
                    StopCaptureAndKeepInfo();
                    return;
                }

                // 获取鼠标屏幕坐标
                if (!GetCursorPos(out POINT cursorPos))
                    return;

                ScreenPosText.Text = $"X: {cursorPos.X}, Y: {cursorPos.Y}";

                // 获取鼠标位置下的顶级窗口句柄
                IntPtr hWnd = WindowFromPoint(cursorPos);
                if (hWnd == IntPtr.Zero)
                    return;

                // 检查是否是本程序窗口或高亮窗口，如果是则跳过
                if (IsOwnWindow(hWnd))
                {
                    return;
                }

                // 获取顶级窗口（根窗口）
                IntPtr rootWnd = GetAncestor(hWnd, GA_ROOT);
                if (rootWnd == IntPtr.Zero)
                    rootWnd = hWnd;

                // 检查是否按住了Ctrl键（临时切换为只捕获顶级窗口）
                bool ctrlPressed = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
                bool captureControl = CaptureControlCheckBox.IsChecked == true && !ctrlPressed;

                // 目标句柄（可能是控件或窗口）
                IntPtr targetHandle = hWnd;
                IntPtr controlHandle = IntPtr.Zero;

                if (captureControl)
                {
                    // 尝试获取更深层的子控件
                    IntPtr deepestChild = GetDeepestChildWindow(hWnd, cursorPos);
                    
                    // 如果找到的最深子窗口不是顶级窗口，则认为它是控件
                    if (deepestChild != IntPtr.Zero && deepestChild != rootWnd)
                    {
                        controlHandle = deepestChild;
                        targetHandle = controlHandle;
                    }
                    // 如果 hWnd 本身就不是顶级窗口，也认为它是控件
                    else if (hWnd != rootWnd)
                    {
                        controlHandle = hWnd;
                        targetHandle = controlHandle;
                    }
                }

                // 计算窗口内坐标
                POINT clientPos = cursorPos;
                ScreenToClient(targetHandle, ref clientPos);
                WindowPosText.Text = $"X: {clientPos.X}, Y: {clientPos.Y}";

                // 更新高亮边框
                if (ShowHighlightCheckBox.IsChecked == true && _highlightWindow != null)
                {
                    UpdateHighlight(targetHandle);
                }
                else
                {
                    _highlightWindow?.Hide();
                }

                // 更新控件信息
                if (controlHandle != IntPtr.Zero)
                {
                    if (controlHandle != _lastControlHandle)
                    {
                        _lastControlHandle = controlHandle;
                        UpdateControlInfo(controlHandle);
                    }
                }
                else
                {
                    if (_lastControlHandle != IntPtr.Zero)
                    {
                        _lastControlHandle = IntPtr.Zero;
                        ClearControlInfo();
                    }
                }

                // 如果顶级窗口句柄没有变化，则不更新窗口信息
                if (rootWnd == _lastHandle)
                    return;

                _lastHandle = rootWnd;
                UpdateWindowInfo(rootWnd);

                // 如果自动刷新开启，更新窗口树
                if (AutoRefreshTreeCheckBox.IsChecked == true && rootWnd != _lastTreeRootHandle)
                {
                    _lastTreeRootHandle = rootWnd;
                    UpdateWindowTree(rootWnd);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止捕获并保留当前信息
        /// </summary>
        private void StopCaptureAndKeepInfo()
        {
            _timer?.Stop();
            CaptureToggle.IsChecked = false;
            
            // 隐藏高亮边框
            _highlightWindow?.Hide();
            
            // 不清除任何文字信息
            
            // 如果窗口树为空，刷新一次
            if (WindowTreeView.Items.Count == 0 && _lastHandle != IntPtr.Zero)
            {
                _lastTreeRootHandle = _lastHandle;
                UpdateWindowTree(_lastHandle);
            }
        }

        /// <summary>
        /// 刷新按钮点击
        /// </summary>
        private void RefreshTreeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastHandle != IntPtr.Zero)
            {
                _lastTreeRootHandle = _lastHandle;
                UpdateWindowTree(_lastHandle);
            }
        }

        /// <summary>
        /// 树形节点选中事件
        /// </summary>
        private void WindowTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is WindowTreeItem item && item.Handle != IntPtr.Zero)
            {
                // 高亮选中的窗口
                if (ShowHighlightCheckBox.IsChecked == true && _highlightWindow != null)
                {
                    UpdateHighlight(item.Handle);
                }
            }
        }

        /// <summary>
        /// 更新窗口树
        /// </summary>
        private void UpdateWindowTree(IntPtr rootHandle)
        {
            WindowTreeView.Items.Clear();

            if (rootHandle == IntPtr.Zero)
                return;

            var rootItem = CreateWindowTreeItem(rootHandle);
            BuildWindowTree(rootHandle, rootItem);
            
            WindowTreeView.Items.Add(rootItem);
            
            // 展开根节点
            if (WindowTreeView.ItemContainerGenerator.ContainerFromItem(rootItem) is TreeViewItem treeViewItem)
            {
                treeViewItem.IsExpanded = true;
            }
        }

        /// <summary>
        /// 创建窗口树节点
        /// </summary>
        private WindowTreeItem CreateWindowTreeItem(IntPtr hWnd)
        {
            StringBuilder titleBuilder = new StringBuilder(256);
            GetWindowText(hWnd, titleBuilder, 256);
            string title = titleBuilder.ToString();

            StringBuilder classBuilder = new StringBuilder(256);
            GetClassName(hWnd, classBuilder, 256);
            string className = classBuilder.ToString();

            bool isVisible = IsWindowVisible(hWnd);
            string visibleTag = isVisible ? "" : " [隐藏]";

            // 显示文本：句柄 + 类名 + 标题（如果有）
            string displayText = $"0x{hWnd.ToInt64():X} [{className}]";
            if (!string.IsNullOrEmpty(title))
            {
                displayText += $" \"{(title.Length > 30 ? title.Substring(0, 30) + "..." : title)}\"";
            }
            displayText += visibleTag;

            // 工具提示：完整信息
            string toolTip = $"句柄: 0x{hWnd.ToInt64():X8} ({hWnd.ToInt64()})\n" +
                            $"类名: {className}\n" +
                            $"标题: {(string.IsNullOrEmpty(title) ? "(无)" : title)}\n" +
                            $"可见: {(isVisible ? "是" : "否")}";

            if (GetWindowRect(hWnd, out RECT rect))
            {
                toolTip += $"\n位置: ({rect.Left}, {rect.Top})\n" +
                          $"大小: {rect.Right - rect.Left} x {rect.Bottom - rect.Top}";
            }

            return new WindowTreeItem
            {
                Handle = hWnd,
                DisplayText = displayText,
                ToolTipText = toolTip
            };
        }

        /// <summary>
        /// 递归构建窗口树
        /// </summary>
        private void BuildWindowTree(IntPtr parentHandle, WindowTreeItem parentItem)
        {
            List<IntPtr> childHandles = new List<IntPtr>();

            EnumChildWindows(parentHandle, (hWnd, lParam) =>
            {
                // 只获取直接子窗口（父窗口是 parentHandle 的）
                if (GetParent(hWnd) == parentHandle)
                {
                    childHandles.Add(hWnd);
                }
                return true;
            }, IntPtr.Zero);

            foreach (var childHandle in childHandles)
            {
                var childItem = CreateWindowTreeItem(childHandle);
                parentItem.Children.Add(childItem);
                
                // 递归构建子窗口的子窗口
                BuildWindowTree(childHandle, childItem);
            }
        }

        /// <summary>
        /// 检查是否是本程序的窗口
        /// </summary>
        private bool IsOwnWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            // 检查是否是本窗口
            if (hWnd == _myWindowHandle)
                return true;

            // 检查是否是高亮窗口
            if (_highlightWindow != null)
            {
                var highlightHandle = new WindowInteropHelper(_highlightWindow).Handle;
                if (hWnd == highlightHandle)
                    return true;
            }

            // 检查根窗口
            IntPtr root = GetAncestor(hWnd, GA_ROOT);
            if (root == _myWindowHandle)
                return true;

            if (_highlightWindow != null)
            {
                var highlightHandle = new WindowInteropHelper(_highlightWindow).Handle;
                if (root == highlightHandle)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 递归获取最深层的子窗口
        /// </summary>
        private IntPtr GetDeepestChildWindow(IntPtr hWnd, POINT screenPoint)
        {
            IntPtr result = hWnd;
            IntPtr child = hWnd;

            while (true)
            {
                // 将屏幕坐标转换为当前窗口的客户区坐标
                POINT clientPoint = screenPoint;
                ScreenToClient(child, ref clientPoint);

                // 使用 RealChildWindowFromPoint 获取子窗口
                IntPtr nextChild = RealChildWindowFromPoint(child, clientPoint);

                // 如果没有找到子窗口，或者子窗口就是自己，则停止
                if (nextChild == IntPtr.Zero || nextChild == child)
                    break;

                child = nextChild;
                result = child;
            }

            return result;
        }

        /// <summary>
        /// 更新高亮边框
        /// </summary>
        private void UpdateHighlight(IntPtr hWnd)
        {
            if (_highlightWindow == null || hWnd == IntPtr.Zero)
                return;

            if (GetWindowRect(hWnd, out RECT rect))
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                _highlightWindow.UpdateHighlight(rect.Left, rect.Top, width, height);
            }
        }

        /// <summary>
        /// 更新控件信息
        /// </summary>
        private void UpdateControlInfo(IntPtr hWnd)
        {
            // 控件句柄
            ControlHandleText.Text = $"0x{hWnd.ToInt64():X8}";

            // 控件类名
            StringBuilder classBuilder = new StringBuilder(256);
            GetClassName(hWnd, classBuilder, 256);
            ControlClassText.Text = classBuilder.ToString();

            // 控件文本
            StringBuilder textBuilder = new StringBuilder(256);
            GetWindowText(hWnd, textBuilder, 256);
            ControlTextContent.Text = textBuilder.ToString();

            // 控件位置
            if (GetWindowRect(hWnd, out RECT rect))
            {
                ControlRectText.Text = $"{rect.Right - rect.Left} x {rect.Bottom - rect.Top} @ ({rect.Left}, {rect.Top})";
            }
        }

        /// <summary>
        /// 清空控件信息
        /// </summary>
        private void ClearControlInfo()
        {
            ControlHandleText.Text = "";
            ControlClassText.Text = "";
            ControlTextContent.Text = "";
            ControlRectText.Text = "";
        }

        private void UpdateWindowInfo(IntPtr hWnd)
        {
            // 窗口句柄
            HandleText.Text = $"0x{hWnd.ToInt64():X8} ({hWnd.ToInt64()})";

            // 窗口标题
            StringBuilder titleBuilder = new StringBuilder(256);
            GetWindowText(hWnd, titleBuilder, 256);
            TitleText.Text = titleBuilder.ToString();

            // 窗口类名
            StringBuilder classBuilder = new StringBuilder(256);
            GetClassName(hWnd, classBuilder, 256);
            ClassNameText.Text = classBuilder.ToString();

            // 窗口位置和大小
            if (GetWindowRect(hWnd, out RECT rect))
            {
                WindowRectText.Text = $"Left: {rect.Left}, Top: {rect.Top}, Right: {rect.Right}, Bottom: {rect.Bottom}";
                WindowSizeText.Text = $"宽度: {rect.Right - rect.Left}, 高度: {rect.Bottom - rect.Top}";
            }

            // 进程信息
            GetWindowThreadProcessId(hWnd, out uint processId);
            ProcessIdText.Text = processId.ToString();

            try
            {
                Process process = Process.GetProcessById((int)processId);
                ProcessNameText.Text = process.ProcessName;
            }
            catch
            {
                ProcessNameText.Text = "无法获取";
            }

            // 父窗口句柄
            IntPtr parentHwnd = GetParent(hWnd);
            if (parentHwnd != IntPtr.Zero)
            {
                ParentHandleText.Text = $"0x{parentHwnd.ToInt64():X8} ({parentHwnd.ToInt64()})";
            }
            else
            {
                // 尝试获取根窗口
                IntPtr rootHwnd = GetAncestor(hWnd, GA_ROOT);
                if (rootHwnd != IntPtr.Zero && rootHwnd != hWnd)
                {
                    ParentHandleText.Text = $"根窗口: 0x{rootHwnd.ToInt64():X8}";
                }
                else
                {
                    ParentHandleText.Text = "无 (顶级窗口)";
                }
            }

            // 窗口样式
            int style = GetWindowLong(hWnd, GWL_STYLE);
            WindowStyleText.Text = GetStyleDescription(style);
        }

        private string GetStyleDescription(int style)
        {
            List<string> styles = new List<string>();

            if ((style & WS_POPUP) != 0) styles.Add("POPUP");
            if ((style & WS_CHILD) != 0) styles.Add("CHILD");
            if ((style & WS_MINIMIZE) != 0) styles.Add("MINIMIZE");
            if ((style & WS_VISIBLE) != 0) styles.Add("VISIBLE");
            if ((style & WS_DISABLED) != 0) styles.Add("DISABLED");
            if ((style & WS_CLIPSIBLINGS) != 0) styles.Add("CLIPSIBLINGS");
            if ((style & WS_CLIPCHILDREN) != 0) styles.Add("CLIPCHILDREN");
            if ((style & WS_MAXIMIZE) != 0) styles.Add("MAXIMIZE");
            if ((style & WS_CAPTION) == WS_CAPTION) styles.Add("CAPTION");
            if ((style & WS_BORDER) != 0) styles.Add("BORDER");
            if ((style & WS_VSCROLL) != 0) styles.Add("VSCROLL");
            if ((style & WS_HSCROLL) != 0) styles.Add("HSCROLL");
            if ((style & WS_SYSMENU) != 0) styles.Add("SYSMENU");
            if ((style & WS_THICKFRAME) != 0) styles.Add("THICKFRAME");
            if ((style & WS_MINIMIZEBOX) != 0) styles.Add("MINIMIZEBOX");
            if ((style & WS_MAXIMIZEBOX) != 0) styles.Add("MAXIMIZEBOX");

            return styles.Count > 0 
                ? $"0x{style:X8} ({string.Join(", ", styles)})" 
                : $"0x{style:X8}";
        }

        private void ClearInfo()
        {
            _lastHandle = IntPtr.Zero;
            _lastControlHandle = IntPtr.Zero;
            _lastTreeRootHandle = IntPtr.Zero;
            ScreenPosText.Text = "X: 0, Y: 0";
            WindowPosText.Text = "X: 0, Y: 0";
            HandleText.Text = "";
            TitleText.Text = "";
            ClassNameText.Text = "";
            WindowRectText.Text = "";
            WindowSizeText.Text = "";
            ProcessIdText.Text = "";
            ProcessNameText.Text = "";
            ParentHandleText.Text = "";
            WindowStyleText.Text = "";
            ClearControlInfo();
            WindowTreeView.Items.Clear();
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer?.Stop();
            _highlightWindow?.Close();
            base.OnClosed(e);
        }
    }
}