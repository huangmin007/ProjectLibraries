using System;
using System.Runtime.InteropServices;

/***
 * 
 * 
 * 
 * 
 * 
**/

namespace Win32API.User32
{

    #region Enumerations
    /// <summary>
    /// <see cref="User32.RegisterHotKey"/> 函数参数 fsModifiers 的值之一或值组合
    /// <para>OR <see cref="MessageType.WM_HOTKEY"/> lParam </para>
    /// <para>参考 https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-registerhotkey </para>
    /// </summary>
    [Flags]
    public enum RhkModifier:uint
    {
        /// <summary>
        /// 必须按住 ALT 键。
        /// </summary>
        ALT = 0x0001,
        /// <summary>
        /// 必须按住 CTRL 键。
        /// </summary>
        CONTROL = 0x0002,
        /// <summary>
        /// 必须按住 SHIFT 键。
        /// </summary>
        SHIFT = 0x0004,
        /// <summary>
        /// 按住 WINDOWS 键。这些键带有 Windows 徽标。保留与 WINDOWS 键相关的键盘快捷键，供操作系统使用。
        /// </summary>
        WIN = 0x0008,
        /// <summary>
        /// 更改热键行为，以使键盘自动重复操作不会产生多个热键通知。
        /// </summary>
        NOREPEAT = 0x4000,
    }
    #endregion


    #region Structures
    #endregion


    #region Deletages
    #endregion


    #region Notifications
    #endregion


    /// <summary>
    /// 
    /// </summary>
    public static partial class User32
    {
        /// <summary>
        /// 定义系统范围的热键。
        /// <para>此功能无法将热键与另一个线程创建的窗口关联。如果为热键指定的击键已经被另一个热键注册，则 <see cref="RegisterHotKey"/> 失败。</para>
        /// <para>如果已经存在具有相同 hWnd 和 id 参数的热键，则将其与新的热键一起维护。应用程序必须显式调用 <see cref="UnregisterHotKey"/> 来注销旧的热键。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-registerhotkey </para>
        /// </summary>
        /// <param name="hWnd">窗口的句柄，它将接收由热键生成的 <see cref="MessageType.WM_HOTKEY"/> 消息。如果此参数为 NULL，则将 <see cref="MessageType.WM_HOTKEY"/> 消息发布到调用线程的消息队列中，并且必须在消息循环中进行处理。</param>
        /// <param name="id">热键的标识符。如果 hWnd 参数为 NULL，则热键与当前线程关联，而不与特定窗口关联。如果已经存在具有相同 hWnd 和 id 参数的热键。</param>
        /// <param name="fsModifiers">必须将这些键与 uVirtKey 参数指定的键组合在一起 才能生成 <see cref="MessageType.WM_HOTKEY"/> 消息</param>
        /// <param name="vk">热键的虚拟键代码 <see cref="VirtualKeyCode"/></param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, RhkModifier fsModifiers, VirtualKeyCode vk);

        /// <summary>
        /// 释放先前由调用线程注册的热键。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-unregisterhotkey </para>
        /// </summary>
        /// <param name="hWnd">与要释放的热键关联的窗口的句柄。如果热键未与窗口关联，则此参数应为 NULL。</param>
        /// <param name="id">要释放的热键的标识符。</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }

    /// <summary>
    /// WindowsAPI User32库，扩展常用/通用，功能/函数，扩展示例，以及使用方式ad
    /// </summary>
    public static partial class User32Extension
    {
    }

}
