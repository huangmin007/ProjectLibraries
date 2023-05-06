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
        /// 表示无法关闭系统，并设置启动系统关闭后要显示给用户的原因字符串。
        /// <para>只能从创建由 hWnd 参数指定的窗口的线程中调用此函数。否则，函数将失败，最后的错误代码是 ERROR_ACCESS_DENIED。</para>
        /// <para>应用程序在开始无法中断的操作（例如刻录 CD 或 DVD）时应调用此函数。操作完成后，请调用 <see cref="ShutdownBlockReasonDestroy"/> 函数以指示可以关闭系统。</para>
        /// <para>由于用户通常在系统关闭时很着急，因此他们可能只花几秒钟的时间查看系统显示的关闭原因。因此，重要的是您的原因字符串必须简短明了。例如，“正在进行CD刻录。” 优于“此应用程序阻止系统关闭，因为正在进行CD刻录。请勿关闭。”</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-shutdownblockreasoncreate </para>
        /// </summary>
        /// <param name="hWnd">应用程序主窗口的句柄。</param>
        /// <param name="pwszReason">[LPCWSTR] 应用程序必须阻止系统关闭的原因。该字符串将在显示 MAX_STR_BLOCKREASON 个字符后被截断。</param>
        /// <returns>如果调用成功，则返回值为非零。如果调用失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShutdownBlockReasonCreate(IntPtr hWnd, string pwszReason);

        /// <summary>
        /// 表示可以关闭系统并释放原因字符串。
        /// <para>只能从创建由hWnd参数指定的窗口的线程中调用此函数。否则，函数将失败，最后的错误代码是 ERROR_ACCESS_DENIED。</para>
        /// <para>如果先前已通过 <see cref="ShutdownBlockReasonCreate"/> 函数阻止了系统关闭，则此函数将释放原因字符串。否则，此功能为无操作。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-shutdownblockreasondestroy </para>
        /// </summary>
        /// <param name="hWnd">应用程序主窗口的句柄。</param>
        /// <returns>如果调用成功，则返回值为非零。如果调用失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShutdownBlockReasonDestroy(IntPtr hWnd);

    }

    /// <summary>
    /// WindowsAPI User32库，扩展常用/通用，功能/函数，扩展示例，以及使用方式ad
    /// </summary>
    public static partial class User32Extension
    {
    }

}
