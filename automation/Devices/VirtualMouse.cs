using automation.Core.Interfaces;
using automation.Core.Native;
using static automation.Core.Native.NativeMethods;

namespace automation.Devices;

/// <summary>
/// 虚拟鼠标实现
/// 使用 Windows SendInput API 模拟鼠标操作
/// </summary>
public class VirtualMouse : IVirtualMouse
{
    #region 移动操作

    /// <inheritdoc/>
    public void MoveTo(int x, int y)
    {
        var (absX, absY) = NativeMethods.ToAbsoluteCoordinates(x, y);
        var input = CreateMouseInput(absX, absY, 0,
            MouseEventFlags.MOUSEEVENTF_MOVE | MouseEventFlags.MOUSEEVENTF_ABSOLUTE);
        SendInput(1, [input], InputSize);
    }

    /// <inheritdoc/>
    public void MoveBy(int deltaX, int deltaY)
    {
        var input = CreateMouseInput(deltaX, deltaY, 0, MouseEventFlags.MOUSEEVENTF_MOVE);
        SendInput(1, [input], InputSize);
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
        var input = CreateMouseInput(0, 0, 0, MouseEventFlags.MOUSEEVENTF_LEFTDOWN);
        SendInput(1, [input], InputSize);
    }

    /// <inheritdoc/>
    public void LeftUp()
    {
        var input = CreateMouseInput(0, 0, 0, MouseEventFlags.MOUSEEVENTF_LEFTUP);
        SendInput(1, [input], InputSize);
    }

    /// <inheritdoc/>
    public void RightDown()
    {
        var input = CreateMouseInput(0, 0, 0, MouseEventFlags.MOUSEEVENTF_RIGHTDOWN);
        SendInput(1, [input], InputSize);
    }

    /// <inheritdoc/>
    public void RightUp()
    {
        var input = CreateMouseInput(0, 0, 0, MouseEventFlags.MOUSEEVENTF_RIGHTUP);
        SendInput(1, [input], InputSize);
    }

    /// <inheritdoc/>
    public void MiddleDown()
    {
        var input = CreateMouseInput(0, 0, 0, MouseEventFlags.MOUSEEVENTF_MIDDLEDOWN);
        SendInput(1, [input], InputSize);
    }

    /// <inheritdoc/>
    public void MiddleUp()
    {
        var input = CreateMouseInput(0, 0, 0, MouseEventFlags.MOUSEEVENTF_MIDDLEUP);
        SendInput(1, [input], InputSize);
    }

    #endregion

    #region 滚轮操作

    /// <inheritdoc/>
    public void ScrollVertical(int delta)
    {
        // WHEEL_DELTA = 120
        var input = CreateMouseInput(0, 0, (uint)(delta * 120), MouseEventFlags.MOUSEEVENTF_WHEEL);
        SendInput(1, [input], InputSize);
    }

    /// <inheritdoc/>
    public void ScrollHorizontal(int delta)
    {
        var input = CreateMouseInput(0, 0, (uint)(delta * 120), MouseEventFlags.MOUSEEVENTF_HWHEEL);
        SendInput(1, [input], InputSize);
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
        if (GetCursorPos(out POINT point))
        {
            return (point.X, point.Y);
        }
        return (0, 0);
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
