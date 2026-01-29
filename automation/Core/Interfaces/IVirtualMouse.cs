namespace automation.Core.Interfaces;

/// <summary>
/// 虚拟鼠标接口
/// 定义鼠标模拟的基本操作
/// </summary>
public interface IVirtualMouse
{
    #region 移动操作

    /// <summary>
    /// 移动鼠标到指定的屏幕坐标
    /// </summary>
    /// <param name="x">屏幕X坐标</param>
    /// <param name="y">屏幕Y坐标</param>
    void MoveTo(int x, int y);

    /// <summary>
    /// 相对当前位置移动鼠标
    /// </summary>
    /// <param name="deltaX">X方向偏移量</param>
    /// <param name="deltaY">Y方向偏移量</param>
    void MoveBy(int deltaX, int deltaY);

    /// <summary>
    /// 平滑移动鼠标到指定坐标（带动画效果）
    /// </summary>
    /// <param name="x">目标X坐标</param>
    /// <param name="y">目标Y坐标</param>
    /// <param name="durationMs">移动持续时间（毫秒）</param>
    /// <param name="steps">移动步数</param>
    Task MoveToSmoothAsync(int x, int y, int durationMs = 500, int steps = 50);

    #endregion

    #region 点击操作

    /// <summary>
    /// 左键单击
    /// </summary>
    void LeftClick();

    /// <summary>
    /// 左键双击
    /// </summary>
    void LeftDoubleClick();

    /// <summary>
    /// 右键单击
    /// </summary>
    void RightClick();

    /// <summary>
    /// 中键单击
    /// </summary>
    void MiddleClick();

    /// <summary>
    /// 在指定位置左键单击
    /// </summary>
    void LeftClickAt(int x, int y);

    /// <summary>
    /// 在指定位置右键单击
    /// </summary>
    void RightClickAt(int x, int y);

    /// <summary>
    /// 在指定位置双击
    /// </summary>
    void DoubleClickAt(int x, int y);

    #endregion

    #region 按下/释放操作

    /// <summary>
    /// 按下左键
    /// </summary>
    void LeftDown();

    /// <summary>
    /// 释放左键
    /// </summary>
    void LeftUp();

    /// <summary>
    /// 按下右键
    /// </summary>
    void RightDown();

    /// <summary>
    /// 释放右键
    /// </summary>
    void RightUp();

    /// <summary>
    /// 按下中键
    /// </summary>
    void MiddleDown();

    /// <summary>
    /// 释放中键
    /// </summary>
    void MiddleUp();

    #endregion

    #region 滚轮操作

    /// <summary>
    /// 垂直滚动（正值向上，负值向下）
    /// </summary>
    /// <param name="delta">滚动量（正值向上，负值向下）</param>
    void ScrollVertical(int delta);

    /// <summary>
    /// 水平滚动（正值向右，负值向左）
    /// </summary>
    /// <param name="delta">滚动量（正值向右，负值向左）</param>
    void ScrollHorizontal(int delta);

    #endregion

    #region 拖拽操作

    /// <summary>
    /// 拖拽操作：从当前位置拖拽到目标位置
    /// </summary>
    /// <param name="toX">目标X坐标</param>
    /// <param name="toY">目标Y坐标</param>
    Task DragToAsync(int toX, int toY);

    /// <summary>
    /// 拖拽操作：从起点拖拽到终点
    /// </summary>
    Task DragAsync(int fromX, int fromY, int toX, int toY);

    #endregion

    #region 状态查询

    /// <summary>
    /// 获取当前鼠标位置
    /// </summary>
    (int x, int y) GetPosition();

    #endregion
}
