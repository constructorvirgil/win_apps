using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using capture.Core;

namespace capture
{
    /// <summary>
    /// 窗口树节点 - 用于 UI 绑定
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
        private string _lastHandle = "";
        private string _lastControlHandle = "";
        private HighlightWindow? _highlightWindow;
        private IntPtr _myWindowHandle = IntPtr.Zero;
        private string _lastTreeRootHandle = "";
        private readonly IWindowCapture _windowCapture;

        public MainWindow()
        {
            InitializeComponent();
            _windowCapture = new FlaUIWindowCapture();
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

            // 测试 FlaUI 是否正常工作
            try
            {
                var testPos = _windowCapture.GetCursorPosition();
                System.Diagnostics.Debug.WriteLine($"FlaUI Test: Cursor at ({testPos.X}, {testPos.Y})");
                
                var testWindow = _windowCapture.GetWindowFromPoint(testPos, false);
                if (testWindow != null)
                {
                    System.Diagnostics.Debug.WriteLine($"FlaUI Test: Successfully captured window '{testWindow.Title}'");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("FlaUI Test: Failed to capture window (returned null)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FlaUI Test Exception: {ex.Message}");
            }
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
                if (IsKeyPressed(System.Windows.Input.Key.LeftShift) || IsKeyPressed(System.Windows.Input.Key.RightShift))
                {
                    StopCaptureAndKeepInfo();
                    return;
                }

                // 获取鼠标屏幕坐标
                var cursorPos = _windowCapture.GetCursorPosition();
                ScreenPosText.Text = $"X: {cursorPos.X}, Y: {cursorPos.Y}";

                // 检查是否按住了Ctrl键（临时切换为只捕获顶级窗口）
                bool ctrlPressed = IsKeyPressed(System.Windows.Input.Key.LeftCtrl) || IsKeyPressed(System.Windows.Input.Key.RightCtrl);
                bool captureControl = CaptureControlCheckBox.IsChecked == true && !ctrlPressed;

                // 获取窗口信息
                var window = _windowCapture.GetWindowFromPoint(cursorPos, captureControl);
                if (window == null)
                {
                    StatusText.Text = "⚠ 无法获取窗口信息";
                    return;
                }

                // 优先通过进程ID判断（更可靠）
                if (IsOwnWindowByProcessId(window))
                {
                    StatusText.Text = "⚠ 鼠标在本程序窗口上，请移到其他窗口";
                    return;
                }

                // 备用：通过句柄判断（对于有原生句柄的窗口）
                if (window.NativeWindowHandle != IntPtr.Zero && IsOwnWindow(window.NativeWindowHandle))
                {
                    StatusText.Text = "⚠ 鼠标在本程序窗口上，请移到其他窗口";
                    return;
                }

                // 清空状态提示
                StatusText.Text = "";

                // 获取根窗口
                var rootWindow = _windowCapture.GetRootWindow(window);
                if (rootWindow == null)
                    rootWindow = window;

                // 确定目标窗口
                var targetWindow = window;
                var controlWindow = captureControl && !_windowCapture.IsSameWindow(window, rootWindow) ? window : null;

                // 计算窗口内坐标
                var clientPos = _windowCapture.ScreenToClient(targetWindow, cursorPos);
                WindowPosText.Text = $"X: {clientPos.X}, Y: {clientPos.Y}";

                // 更新高亮边框
                if (ShowHighlightCheckBox.IsChecked == true && _highlightWindow != null)
                {
                    UpdateHighlight(targetWindow);
                }
                else
                {
                    _highlightWindow?.Hide();
                }

                // 更新控件信息（实时更新）
                if (controlWindow != null)
                {
                    // 记录当前控件句柄
                    if (controlWindow.Handle != _lastControlHandle)
                    {
                        _lastControlHandle = controlWindow.Handle;
                    }
                    // 始终更新控件信息（控件的文本、位置等可能随时变化）
                    UpdateControlInfo(controlWindow);
                }
                else
                {
                    // 没有控件时清空信息
                    if (_lastControlHandle != "")
                    {
                        _lastControlHandle = "";
                        ClearControlInfo();
                    }
                }

                // 更新窗口信息（始终更新，确保实时性）
                // 窗口标题、位置、状态等可能随时变化，需要实时刷新
                if (rootWindow.Handle != _lastHandle)
                {
                    // 窗口句柄变化了，记录新句柄
                    _lastHandle = rootWindow.Handle;
                }
                UpdateWindowInfo(rootWindow);  // 始终更新

                // 如果自动刷新开启，更新窗口树
                if (AutoRefreshTreeCheckBox.IsChecked == true && rootWindow.Handle != _lastTreeRootHandle)
                {
                    _lastTreeRootHandle = rootWindow.Handle;
                    UpdateWindowTree(rootWindow);
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"❌ 错误: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error in Timer_Tick: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        private bool IsKeyPressed(System.Windows.Input.Key key)
        {
            return System.Windows.Input.Keyboard.IsKeyDown(key);
        }

        private void StopCaptureAndKeepInfo()
        {
            _timer?.Stop();
            CaptureToggle.IsChecked = false;
            
            // 隐藏高亮边框
            _highlightWindow?.Hide();
            
            // 如果窗口树为空，刷新一次
            if (WindowTreeView.Items.Count == 0 && _lastHandle != "")
            {
                var window = _windowCapture.GetWindowInfo(_lastHandle);
                if (window != null)
                {
                    _lastTreeRootHandle = _lastHandle;
                    UpdateWindowTree(window);
                }
            }
        }

        private void RefreshTreeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastHandle != "")
            {
                var window = _windowCapture.GetWindowInfo(_lastHandle);
                if (window != null)
                {
                    _lastTreeRootHandle = _lastHandle;
                    UpdateWindowTree(window);
                }
            }
        }

        private void WindowTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is WindowTreeItem item && item.Handle != IntPtr.Zero)
            {
                // 高亮选中的窗口
                if (ShowHighlightCheckBox.IsChecked == true && _highlightWindow != null)
                {
                    var window = _windowCapture.GetWindowInfoFromNativeHandle(item.Handle);
                    if (window != null)
                    {
                        UpdateHighlight(window);
                    }
                }
            }
        }

        private void UpdateWindowTree(Core.WindowInfo rootWindow)
        {
            WindowTreeView.Items.Clear();

            var tree = _windowCapture.BuildWindowTree(rootWindow);
            if (tree == null)
                return;

            var rootItem = ConvertToWindowTreeItem(tree);
            WindowTreeView.Items.Add(rootItem);
            
            // 展开根节点
            if (WindowTreeView.ItemContainerGenerator.ContainerFromItem(rootItem) is TreeViewItem treeViewItem)
            {
                treeViewItem.IsExpanded = true;
            }
        }

        private WindowTreeItem ConvertToWindowTreeItem(Core.WindowTreeNode node)
        {
            var item = new WindowTreeItem
            {
                Handle = node.NativeWindowHandle,
                DisplayText = node.DisplayText,
                ToolTipText = node.ToolTipText
            };

            foreach (var child in node.Children)
            {
                item.Children.Add(ConvertToWindowTreeItem(child));
            }

            return item;
        }

        private bool IsOwnWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                // 句柄为0很正常，不能仅凭这个判断
                // 很多现代应用（Chrome、WPF等）的窗口句柄可能为0
                return false;
            }

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

            return false;
        }

        private bool IsOwnWindowByProcessId(Core.WindowInfo window)
        {
            // 获取当前进程ID
            var currentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            
            // 如果是同一个进程，说明是本程序的窗口
            return window.ProcessId == currentProcessId;
        }

        private void UpdateHighlight(Core.WindowInfo window)
        {
            if (_highlightWindow == null)
                return;

            _highlightWindow.UpdateHighlight(
                window.Bounds.Left,
                window.Bounds.Top,
                window.Bounds.Width,
                window.Bounds.Height
            );
        }

        private void UpdateControlInfo(Core.WindowInfo window)
        {
            ControlHandleText.Text = window.Handle;
            ControlClassText.Text = window.ClassName;
            ControlTextContent.Text = window.Title;
            ControlRectText.Text = $"{window.Bounds.Width} x {window.Bounds.Height} @ ({window.Bounds.Left}, {window.Bounds.Top})";
        }

        private void ClearControlInfo()
        {
            ControlHandleText.Text = "";
            ControlClassText.Text = "";
            ControlTextContent.Text = "";
            ControlRectText.Text = "";
        }

        private void UpdateWindowInfo(Core.WindowInfo window)
        {
            HandleText.Text = $"{window.Handle} ({window.NativeWindowHandle.ToInt64()})";
            TitleText.Text = window.Title;
            ClassNameText.Text = window.ClassName;
            WindowRectText.Text = $"Left: {window.Bounds.Left}, Top: {window.Bounds.Top}, Right: {window.Bounds.Right}, Bottom: {window.Bounds.Bottom}";
            WindowSizeText.Text = $"宽度: {window.Bounds.Width}, 高度: {window.Bounds.Height}";
            ProcessIdText.Text = window.ProcessId.ToString();
            ProcessNameText.Text = window.ProcessName;

            if (window.ParentHandle != null)
            {
                ParentHandleText.Text = window.ParentHandle;
            }
            else
            {
                ParentHandleText.Text = "无 (顶级窗口)";
            }

            WindowStyleText.Text = window.StyleDescription;
            
            // FlaUI 特有信息
            AutomationIdText.Text = window.AutomationId ?? "(无)";
            ControlTypeText.Text = window.ControlType ?? "(无)";
            FrameworkIdText.Text = window.FrameworkId ?? "(无)";
        }

        private void ClearInfo()
        {
            _lastHandle = "";
            _lastControlHandle = "";
            _lastTreeRootHandle = "";
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
            AutomationIdText.Text = "";
            ControlTypeText.Text = "";
            FrameworkIdText.Text = "";
            ClearControlInfo();
            WindowTreeView.Items.Clear();
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer?.Stop();
            _highlightWindow?.Close();
            
            if (_windowCapture is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            base.OnClosed(e);
        }
    }
}
