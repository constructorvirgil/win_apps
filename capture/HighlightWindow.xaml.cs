using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace capture
{
    /// <summary>
    /// 高亮边框窗口 - 用于显示目标窗口/控件的边框
    /// </summary>
    public partial class HighlightWindow : Window
    {
        #region Windows API 声明

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;

        #endregion

        public HighlightWindow()
        {
            InitializeComponent();
            Loaded += HighlightWindow_Loaded;
        }

        private void HighlightWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 设置窗口为鼠标穿透
            var hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW);
        }

        /// <summary>
        /// 更新高亮边框的位置和大小
        /// </summary>
        public void UpdateHighlight(int left, int top, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                Hide();
                return;
            }

            // 考虑 DPI 缩放
            var source = PresentationSource.FromVisual(this);
            double dpiX = 1.0, dpiY = 1.0;
            if (source?.CompositionTarget != null)
            {
                dpiX = source.CompositionTarget.TransformFromDevice.M11;
                dpiY = source.CompositionTarget.TransformFromDevice.M22;
            }

            Left = left * dpiX;
            Top = top * dpiY;
            Width = width * dpiX;
            Height = height * dpiY;

            if (!IsVisible)
            {
                Show();
            }
        }

        /// <summary>
        /// 设置边框颜色
        /// </summary>
        public void SetBorderColor(Color color)
        {
            HighlightBorder.BorderBrush = new SolidColorBrush(color);
        }

        /// <summary>
        /// 设置边框粗细
        /// </summary>
        public void SetBorderThickness(double thickness)
        {
            HighlightBorder.BorderThickness = new Thickness(thickness);
        }
    }
}
