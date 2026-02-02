namespace VirtualHidSimulator.Core.Interfaces;

/// <summary>
/// 虚拟键盘接口
/// 定义键盘模拟的基本操作
/// </summary>
public interface IVirtualKeyboard
{
    #region 单键操作

    /// <summary>
    /// 按下并释放单个键
    /// </summary>
    /// <param name="keyCode">虚拟键码</param>
    void PressKey(ushort keyCode);

    /// <summary>
    /// 按下键（不释放）
    /// </summary>
    /// <param name="keyCode">虚拟键码</param>
    void KeyDown(ushort keyCode);

    /// <summary>
    /// 释放键
    /// </summary>
    /// <param name="keyCode">虚拟键码</param>
    void KeyUp(ushort keyCode);

    #endregion

    #region 文本输入

    /// <summary>
    /// 输入单个字符
    /// </summary>
    /// <param name="c">要输入的字符</param>
    void TypeChar(char c);

    /// <summary>
    /// 输入文本字符串
    /// </summary>
    /// <param name="text">要输入的文本</param>
    void TypeText(string text);

    /// <summary>
    /// 使用 Unicode 方式输入文本（支持中文等特殊字符）
    /// </summary>
    /// <param name="text">要输入的文本</param>
    void TypeUnicode(string text);

    /// <summary>
    /// 带延迟的输入文本（模拟真实打字）
    /// </summary>
    /// <param name="text">要输入的文本</param>
    /// <param name="delayMs">每个字符之间的延迟（毫秒）</param>
    Task TypeTextWithDelayAsync(string text, int delayMs = 50);

    #endregion

    #region 组合键操作

    /// <summary>
    /// 按下组合键
    /// 例如: Ctrl+C, Ctrl+Shift+S
    /// </summary>
    /// <param name="keyCodes">要同时按下的键码数组</param>
    void PressKeyCombination(params ushort[] keyCodes);

    /// <summary>
    /// 按下修饰键 + 普通键的组合
    /// </summary>
    /// <param name="modifiers">修饰键数组 (Ctrl, Shift, Alt)</param>
    /// <param name="key">普通键</param>
    void PressWithModifiers(ushort[] modifiers, ushort key);

    #endregion

    #region 快捷方式

    /// <summary>
    /// Ctrl + 键
    /// </summary>
    void CtrlKey(ushort key);

    /// <summary>
    /// Alt + 键
    /// </summary>
    void AltKey(ushort key);

    /// <summary>
    /// Shift + 键
    /// </summary>
    void ShiftKey(ushort key);

    /// <summary>
    /// Ctrl + Shift + 键
    /// </summary>
    void CtrlShiftKey(ushort key);

    /// <summary>
    /// Ctrl + Alt + 键
    /// </summary>
    void CtrlAltKey(ushort key);

    /// <summary>
    /// Win + 键
    /// </summary>
    void WinKey(ushort key);

    #endregion

    #region 常用快捷键

    /// <summary>
    /// 复制 (Ctrl+C)
    /// </summary>
    void Copy();

    /// <summary>
    /// 粘贴 (Ctrl+V)
    /// </summary>
    void Paste();

    /// <summary>
    /// 剪切 (Ctrl+X)
    /// </summary>
    void Cut();

    /// <summary>
    /// 全选 (Ctrl+A)
    /// </summary>
    void SelectAll();

    /// <summary>
    /// 撤销 (Ctrl+Z)
    /// </summary>
    void Undo();

    /// <summary>
    /// 重做 (Ctrl+Y)
    /// </summary>
    void Redo();

    /// <summary>
    /// 保存 (Ctrl+S)
    /// </summary>
    void Save();

    /// <summary>
    /// 新建 (Ctrl+N)
    /// </summary>
    void New();

    /// <summary>
    /// 打开 (Ctrl+O)
    /// </summary>
    void Open();

    /// <summary>
    /// 查找 (Ctrl+F)
    /// </summary>
    void Find();

    /// <summary>
    /// 替换 (Ctrl+H)
    /// </summary>
    void Replace();

    /// <summary>
    /// 回车键
    /// </summary>
    void Enter();

    /// <summary>
    /// Tab键
    /// </summary>
    void Tab();

    /// <summary>
    /// Escape键
    /// </summary>
    void Escape();

    /// <summary>
    /// 退格键
    /// </summary>
    void Backspace();

    /// <summary>
    /// 删除键
    /// </summary>
    void Delete();

    #endregion
}
