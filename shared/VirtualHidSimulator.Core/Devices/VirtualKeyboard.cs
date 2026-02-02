using VirtualHidSimulator.Core.Drivers;
using VirtualHidSimulator.Core.HidDefinitions;
using VirtualHidSimulator.Core.Interfaces;

namespace VirtualHidSimulator.Devices;

/// <summary>
/// 虚拟键盘实现
/// 通过驱动管理器使用底层驱动模拟键盘操作
/// </summary>
public class VirtualKeyboard : IVirtualKeyboard
{
    private IInputDriver Driver => DriverManager.Instance.CurrentDriver;

    #region 单键操作

    /// <inheritdoc/>
    public void PressKey(ushort keyCode)
    {
        KeyDown(keyCode);
        KeyUp(keyCode);
    }

    /// <inheritdoc/>
    public void KeyDown(ushort keyCode)
    {
        Driver.KeyDown(keyCode);
    }

    /// <inheritdoc/>
    public void KeyUp(ushort keyCode)
    {
        Driver.KeyUp(keyCode);
    }

    #endregion

    #region 文本输入

    /// <inheritdoc/>
    public void TypeChar(char c)
    {
        ushort keyCode = VirtualKeyCodes.FromChar(c);

        if (keyCode != 0)
        {
            bool needsShift = VirtualKeyCodes.NeedsShift(c);

            if (needsShift)
                KeyDown(VirtualKeyCodes.VK_SHIFT);

            PressKey(keyCode);

            if (needsShift)
                KeyUp(VirtualKeyCodes.VK_SHIFT);
        }
        else
        {
            // 使用 Unicode 输入
            TypeUnicodeChar(c);
        }
    }

    /// <inheritdoc/>
    public void TypeText(string text)
    {
        foreach (char c in text)
        {
            TypeChar(c);
        }
    }

    /// <inheritdoc/>
    public void TypeUnicode(string text)
    {
        foreach (char c in text)
        {
            TypeUnicodeChar(c);
        }
    }

    /// <inheritdoc/>
    public async Task TypeTextWithDelayAsync(string text, int delayMs = 50)
    {
        foreach (char c in text)
        {
            TypeChar(c);
            await Task.Delay(delayMs);
        }
    }

    #endregion

    #region 组合键操作

    /// <inheritdoc/>
    public void PressKeyCombination(params ushort[] keyCodes)
    {
        if (keyCodes.Length == 0) return;

        // 按顺序按下所有键
        foreach (var keyCode in keyCodes)
        {
            KeyDown(keyCode);
        }

        // 按相反顺序释放所有键
        for (int i = keyCodes.Length - 1; i >= 0; i--)
        {
            KeyUp(keyCodes[i]);
        }
    }

    /// <inheritdoc/>
    public void PressWithModifiers(ushort[] modifiers, ushort key)
    {
        // 按下所有修饰键
        foreach (var mod in modifiers)
        {
            KeyDown(mod);
        }

        // 按下并释放目标键
        PressKey(key);

        // 释放所有修饰键（逆序）
        for (int i = modifiers.Length - 1; i >= 0; i--)
        {
            KeyUp(modifiers[i]);
        }
    }

    #endregion

    #region 快捷方式

    /// <inheritdoc/>
    public void CtrlKey(ushort key)
    {
        PressWithModifiers([VirtualKeyCodes.VK_CONTROL], key);
    }

    /// <inheritdoc/>
    public void AltKey(ushort key)
    {
        PressWithModifiers([VirtualKeyCodes.VK_MENU], key);
    }

    /// <inheritdoc/>
    public void ShiftKey(ushort key)
    {
        PressWithModifiers([VirtualKeyCodes.VK_SHIFT], key);
    }

    /// <inheritdoc/>
    public void CtrlShiftKey(ushort key)
    {
        PressWithModifiers([VirtualKeyCodes.VK_CONTROL, VirtualKeyCodes.VK_SHIFT], key);
    }

    /// <inheritdoc/>
    public void CtrlAltKey(ushort key)
    {
        PressWithModifiers([VirtualKeyCodes.VK_CONTROL, VirtualKeyCodes.VK_MENU], key);
    }

    /// <inheritdoc/>
    public void WinKey(ushort key)
    {
        PressWithModifiers([VirtualKeyCodes.VK_LWIN], key);
    }

    #endregion

    #region 常用快捷键

    /// <inheritdoc/>
    public void Copy() => CtrlKey(VirtualKeyCodes.VK_C);

    /// <inheritdoc/>
    public void Paste() => CtrlKey(VirtualKeyCodes.VK_V);

    /// <inheritdoc/>
    public void Cut() => CtrlKey(VirtualKeyCodes.VK_X);

    /// <inheritdoc/>
    public void SelectAll() => CtrlKey(VirtualKeyCodes.VK_A);

    /// <inheritdoc/>
    public void Undo() => CtrlKey(VirtualKeyCodes.VK_Z);

    /// <inheritdoc/>
    public void Redo() => CtrlKey(VirtualKeyCodes.VK_Y);

    /// <inheritdoc/>
    public void Save() => CtrlKey(VirtualKeyCodes.VK_S);

    /// <inheritdoc/>
    public void New() => CtrlKey(VirtualKeyCodes.VK_N);

    /// <inheritdoc/>
    public void Open() => CtrlKey(VirtualKeyCodes.VK_O);

    /// <inheritdoc/>
    public void Find() => CtrlKey(VirtualKeyCodes.VK_F);

    /// <inheritdoc/>
    public void Replace() => CtrlKey(VirtualKeyCodes.VK_H);

    /// <inheritdoc/>
    public void Enter() => PressKey(VirtualKeyCodes.VK_RETURN);

    /// <inheritdoc/>
    public void Tab() => PressKey(VirtualKeyCodes.VK_TAB);

    /// <inheritdoc/>
    public void Escape() => PressKey(VirtualKeyCodes.VK_ESCAPE);

    /// <inheritdoc/>
    public void Backspace() => PressKey(VirtualKeyCodes.VK_BACK);

    /// <inheritdoc/>
    public void Delete() => PressKey(VirtualKeyCodes.VK_DELETE);

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 使用 Unicode 方式输入单个字符
    /// </summary>
    private void TypeUnicodeChar(char c)
    {
        Driver.SendUnicode(c);
    }

    #endregion
}
