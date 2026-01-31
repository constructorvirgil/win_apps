namespace capture.Core
{
    /// <summary>
    /// 窗口捕获接口 - 定义统一的窗口操作抽象
    /// </summary>
    public interface IWindowCapture
    {
        /// <summary>
        /// 获取鼠标当前屏幕坐标
        /// </summary>
        Point GetCursorPosition();

        /// <summary>
        /// 从屏幕坐标获取窗口元素
        /// </summary>
        /// <param name="screenPoint">屏幕坐标</param>
        /// <param name="deepSearch">是否深度搜索子元素</param>
        /// <returns>窗口信息，如果未找到返回 null</returns>
        WindowInfo? GetWindowFromPoint(Point screenPoint, bool deepSearch = false);

        /// <summary>
        /// 获取窗口的根窗口
        /// </summary>
        WindowInfo? GetRootWindow(WindowInfo window);

        /// <summary>
        /// 获取屏幕坐标在窗口中的相对坐标
        /// </summary>
        Point ScreenToClient(WindowInfo window, Point screenPoint);

        /// <summary>
        /// 获取窗口的详细信息
        /// </summary>
        WindowInfo? GetWindowInfo(string handle);

        /// <summary>
        /// 获取窗口的子窗口列表
        /// </summary>
        List<WindowInfo> GetChildWindows(WindowInfo window);

        /// <summary>
        /// 构建窗口树
        /// </summary>
        WindowTreeNode? BuildWindowTree(WindowInfo rootWindow);

        /// <summary>
        /// 检查句柄是否是指定窗口
        /// </summary>
        bool IsSameWindow(WindowInfo window1, WindowInfo window2);

        /// <summary>
        /// 从原生句柄获取窗口信息
        /// </summary>
        WindowInfo? GetWindowInfoFromNativeHandle(IntPtr handle);
    }
}
