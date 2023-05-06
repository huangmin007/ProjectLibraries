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
    /// <summary>
    /// <see cref="FLASHINFO.dwFlags"/> 字段的值之一或值组合
    /// </summary>
    [Flags]
    public enum FlashFlags:uint
    {
        /// <summary>
        /// 停止闪烁。系统将窗口还原到其原始状态。
        /// </summary>
        FLASHW_STOP = 0x00000000,
        /// <summary>
        /// 刷新窗口标题。
        /// </summary>
        FLASHW_CAPTION = 0x00000001,
        /// <summary>
        /// 刷新任务栏按钮。
        /// </summary>
        FLASHW_TRAY = 0x00000002,
        /// <summary>
        /// 同时闪烁窗口标题和任务栏按钮。这等效于设置 <see cref="FLASHW_CAPTION"/> | <see cref="FLASHW_TRAY"/> 标志。
        /// </summary>
        FLASHW_ALL = 0x00000003,
        /// <summary>
        /// 连续闪烁，直到设置了 <see cref="FLASHW_STOP"/> 标志。
        /// </summary>
        FLASHW_TIMER = 0x00000004,
        /// <summary>
        /// 持续闪烁直到窗口到达前台。
        /// </summary>
        FLASHW_TIMERNOFG = 0x0000000C,
    }


    /// <summary>
    /// <see cref="User32.AnimateWindow"/> 函数参数 dwFlags 的值之一或值组合
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-animatewindow </para>
    /// </summary>
    [Flags]
    public enum AwFlags
    {
        /// <summary>
        /// 从左到右对窗口进行动画处理。此标志可与滚动或幻灯片动画一起使用。与 <see cref="AW_CENTER"/> 或 <see cref="AW_BLEND"/> 一起使用时将被忽略。
        /// </summary>
        AW_HOR_POSITIVE = 0x00000001,
        /// <summary>
        /// 从右到左对窗口进行动画处理。此标志可与滚动或幻灯片动画一起使用。与 <see cref="AW_CENTER"/> 或 <see cref="AW_BLEND"/> 一起使用时将被忽略。
        /// </summary>
        AW_HOR_NEGATIVE = 0x00000002,
        /// <summary>
        /// 从上到下对窗口进行动画处理。此标志可与滚动或幻灯片动画一起使用。与 <see cref="AW_CENTER"/> 或 <see cref="AW_BLEND"/> 一起使用时将被忽略。
        /// </summary>
        AW_VER_POSITIVE = 0x00000004,
        /// <summary>
        /// 从底部到顶部对窗口进行动画处理。此标志可与滚动或幻灯片动画一起使用。与 <see cref="AW_CENTER"/> 或 <see cref="AW_BLEND"/> 一起使用时将被忽略。
        /// </summary>
        AW_VER_NEGATIVE = 0x00000008,
        /// <summary>
        /// 如果使用 <see cref="AW_HIDE"/>，则使窗口看起来向内折叠；如果不使用 <see cref="AW_HIDE"/>，则使窗口向外折叠。各种方向标记无效。
        /// </summary>
        AW_CENTER = 0x00000010,
        /// <summary>
        /// 隐藏窗口。默认情况下，显示窗口。
        /// </summary>
        AW_HIDE = 0x00010000,
        /// <summary>
        /// 激活窗口。不要将此值与 <see cref="AW_HIDE"/> 一起使用。
        /// </summary>
        AW_ACTIVATE = 0x00020000,
        /// <summary>
        /// 使用幻灯片动画。默认情况下，使用滚动动画。与 <see cref="AW_CENTER"/> 一起使用时，将忽略此标志。
        /// </summary>
        AW_SLIDE = 0x00040000,
        /// <summary>
        /// 使用淡入淡出效果。仅当 hwnd 是顶级窗口时，才可以使用此标志。
        /// </summary>
        AW_BLEND = 0x00080000,
    }

    #endregion

    #region Structures
    /// <summary>
    /// FLASHWINFO 结构体。注意一定要设置 cbSize 大小，带参的构造函数 (FLASHINFO(IntPtr hwnd)) 中已经设置;(FLASHWINFO, *PFLASHWINFO)
    /// <para>包含窗口的闪烁状态以及系统应刷新窗口的次数。</para>
    /// <para>示例：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-flashwinfo </para>
    /// </summary>
    public struct FLASHINFO
    {
        /// <summary>
        /// 结构的大小，以字节为单位。
        /// <para>等于 (uint)Marshal.SizeOf(typeof(FLASHWINFO)); </para>
        /// </summary>
        public uint cbSize;
        /// <summary>
        /// 要刷新的窗口的句柄。该窗口可以打开或最小化。
        /// </summary>
        public IntPtr hWnd;
        /// <summary>
        /// 闪光灯状态
        /// </summary>
        public FlashFlags dwFlags;
        /// <summary>
        /// 刷新窗口的次数。
        /// </summary>
        public uint uCount;
        /// <summary>
        /// 刷新窗口的速率，以毫秒为单位。如果 dwTimeout 为零，则该函数使用默认的光标闪烁速率。
        /// </summary>
        public int dwTimeout;
        /// <summary>
        /// FLASHWINFO 结构体
        /// </summary>
        /// <param name="hwnd">要刷新的窗口的句柄。该窗口可以打开或最小化。</param>
        public FLASHINFO(IntPtr hwnd)
        {
            uCount = 3;
            hWnd = hwnd;
            dwTimeout = 500;
            dwFlags = FlashFlags.FLASHW_ALL;
            cbSize = (uint)Marshal.SizeOf(typeof(FLASHINFO));
        }

        /// <summary>
        /// <see cref="FLASHWINFO"/> 结构体大字，以字节为单位。
        /// </summary>
        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(FLASHINFO));

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{nameof(FLASHINFO)}] cbSize:{cbSize}, hWnd:{hWnd}, dwFlags:{dwFlags}, uCount:{uCount}, dwTimeout:{dwTimeout}";
        }
    }
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
        /// 闪烁指定的窗口一次。它不会更改窗口的活动状态。
        /// <para>若要将窗口刷新指定的次数，请使用 <see cref="FlashWindowEx"/> 函数。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-flashwindow </para>
        /// </summary>
        /// <param name="hWnd">要刷新的窗口的句柄。窗口可以打开或最小化。</param>
        /// <param name="bInvert">如果此参数为 TRUE，则窗口从一种状态闪烁到另一种状态。如果为 FALSE，则窗口将返回其原始状态（活动或不活动）。
        /// <para>当最小化应用程序且此参数为 TRUE 时，任务栏窗口按钮将闪烁活动/不活动。如果为 FALSE，则任务栏窗口按钮将不活动地闪烁，这意味着它不会更改颜色。它会闪烁，就像正在重绘一样，但不会向用户提供视觉上的反转提示。</para>
        /// </param>
        /// <returns>返回值指定在调用FlashWindow函数之前窗口的状态 。如果在调用之前将窗口标题绘制为活动窗口，则返回值为非零。否则，返回值为零。</returns>
        [DllImport("user32.dll")]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool FlashWindow(IntPtr hWnd, bool bInvert);

        /// <summary>
        /// 闪烁指定的窗口。它不会更改窗口的活动状态。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-flashwindowex </para>
        /// </summary>
        /// <param name="pfwi">[PFLASHWINFO] 指向 <see cref="FLASHINFO"/> 结构的指针</param>
        /// <returns>返回值指定在调用 <see cref="FlashWindowEx"/> 函数之前窗口的状态 。如果在调用之前将窗口标题绘制为活动窗口，则返回值为非零。否则，返回值为零。</returns>
        [DllImport("user32.dll")]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool FlashWindowEx(ref FLASHINFO pfwi);

        /// <summary>
        /// 在显示或隐藏窗口时使您产生特殊效果。动画有四种类型：滚动，滑动，折叠或展开以及 alpha 混合淡入。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-animatewindow </para>
        /// </summary>
        /// <param name="hWnd">窗口动画的句柄。调用线程必须拥有此窗口。</param>
        /// <param name="dwTime">播放动画所需的时间（以毫秒为单位）。通常，动画播放需要200毫秒。</param>
        /// <param name="dwFlags">动画的类型 <see cref="AwFlags"/></param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/> 函数。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool AnimateWindow(IntPtr hWnd, uint dwTime, AwFlags dwFlags);

    }

    /// <summary>
    /// WindowsAPI User32库，扩展常用/通用，功能/函数，扩展示例，以及使用方式ad
    /// </summary>
    public static partial class User32Extension
    {
    }

}
