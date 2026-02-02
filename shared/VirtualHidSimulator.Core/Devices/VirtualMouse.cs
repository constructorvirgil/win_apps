using VirtualHidSimulator.Core.Drivers;
using VirtualHidSimulator.Core.Interfaces;

namespace VirtualHidSimulator.Devices;

/// <summary>
/// 虚拟鼠标实现
/// 通过驱动管理器使用底层驱动模拟鼠标操作
/// </summary>
public class VirtualMouse : IVirtualMouse
{
    private IInputDriver Driver => DriverManager.Instance.CurrentDriver;

    #region 移动操作

    /// <inheritdoc/>
    public void MoveTo(int x, int y)
    {
        Driver.MouseMoveTo(x, y);
    }

    /// <inheritdoc/>
    public void MoveBy(int deltaX, int deltaY)
    {
        Driver.MouseMoveBy(deltaX, deltaY);
    }

    /// <inheritdoc/>
    public async Task MoveToSmoothAsync(int x, int y, int durationMs = 500, int steps = 50)
    {
        var (startX, startY) = GetPosition();
        int stepDelay = durationMs / steps;

        for (int i = 1; i <= steps; i++)
        {
            float progress = (float)i / steps;
            // 使用缓动函数使移动更自然
            float easedProgress = EaseInOutQuad(progress);

            int currentX = (int)(startX + (x - startX) * easedProgress);
            int currentY = (int)(startY + (y - startY) * easedProgress);

            MoveTo(currentX, currentY);
            await Task.Delay(stepDelay);
        }
    }

    #endregion

    #region 点击操作

    /// <inheritdoc/>
    public void LeftClick()
    {
        LeftDown();
        LeftUp();
    }

    /// <inheritdoc/>
    public void LeftDoubleClick()
    {
        LeftClick();
        LeftClick();
    }

    /// <inheritdoc/>
    public void RightClick()
    {
        RightDown();
        RightUp();
    }

    /// <inheritdoc/>
    public void MiddleClick()
    {
        MiddleDown();
        MiddleUp();
    }

    /// <inheritdoc/>
    public void LeftClickAt(int x, int y)
    {
        MoveTo(x, y);
        LeftClick();
    }

    /// <inheritdoc/>
    public void RightClickAt(int x, int y)
    {
        MoveTo(x, y);
        RightClick();
    }

    /// <inheritdoc/>
    public void DoubleClickAt(int x, int y)
    {
        MoveTo(x, y);
        LeftDoubleClick();
    }

    #endregion

    #region 按下/释放操作

    /// <inheritdoc/>
    public void LeftDown()
    {
        Driver.MouseButtonDown(MouseButton.Left);
    }

    /// <inheritdoc/>
    public void LeftUp()
    {
        Driver.MouseButtonUp(MouseButton.Left);
    }

    /// <inheritdoc/>
    public void RightDown()
    {
        Driver.MouseButtonDown(MouseButton.Right);
    }

    /// <inheritdoc/>
    public void RightUp()
    {
        Driver.MouseButtonUp(MouseButton.Right);
    }

    /// <inheritdoc/>
    public void MiddleDown()
    {
        Driver.MouseButtonDown(MouseButton.Middle);
    }

    /// <inheritdoc/>
    public void MiddleUp()
    {
        Driver.MouseButtonUp(MouseButton.Middle);
    }

    #endregion

    #region 滚轮操作

    /// <inheritdoc/>
    public void ScrollVertical(int delta)
    {
        Driver.MouseWheel(delta, horizontal: false);
    }

    /// <inheritdoc/>
    public void ScrollHorizontal(int delta)
    {
        Driver.MouseWheel(delta, horizontal: true);
    }

    #endregion

    #region 拖拽操作

    /// <inheritdoc/>
    public async Task DragToAsync(int toX, int toY)
    {
        LeftDown();
        await MoveToSmoothAsync(toX, toY, 300, 30);
        LeftUp();
    }

    /// <inheritdoc/>
    public async Task DragAsync(int fromX, int fromY, int toX, int toY)
    {
        MoveTo(fromX, fromY);
        await Task.Delay(50);
        await DragToAsync(toX, toY);
    }

    #endregion

    #region 状态查询

    /// <inheritdoc/>
    public (int x, int y) GetPosition()
    {
        return Driver.GetMousePosition();
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 缓动函数：二次缓入缓出
    /// </summary>
    private static float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - (-2f * t + 2f) * (-2f * t + 2f) / 2f;
    }

    #endregion
}
