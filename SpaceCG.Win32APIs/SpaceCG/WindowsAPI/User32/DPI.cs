using System;
using System.Runtime.InteropServices;

/***
 * 
 * 
 * 
 * 
 * 
**/

namespace SpaceCG.WindowsAPI.User32
{
    #region Enumerations
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
        /// 返回系统 DPI。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getdpiforsystem </para>
        /// </summary>
        /// <returns>返回 DPI 值</returns>
        [DllImport("user32.dll")]
        public static extern uint GetDpiForSystem();

        /// <summary>
        /// 返回关联窗口的每英寸点数（dpi）值。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getdpiforwindow </para>
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns>窗口的 DPI 取决于窗口的 DPI_AWARENESS。无效的 hwnd 值将导致返回值为0。</returns>
        [DllImport("user32.dll")]
        public static extern uint GetDpiForWindow(IntPtr hWnd);
    }

    /// <summary>
    /// WindowsAPI User32库，扩展常用/通用，功能/函数，扩展示例，以及使用方式ad
    /// </summary>
    public static partial class User32Extension
    {
    }

}
