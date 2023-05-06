using System;
using System.Text;
using System.Runtime.InteropServices;

namespace SpaceCG.WindowsAPI.User32
{

    #region Enumerations   
    /// <summary>
    /// <see cref="User32.PeekMessage"/> 函数参数 wRemoveMsg 的值之一或值组合
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-peekmessagea </para>
    /// </summary>
    [Flags]
    public enum PmFlags
    {
        /// <summary>
        /// 通过 <see cref="User32.PeekMessage"/> 处理后，消息不会从队列中删除。
        /// </summary>
        PM_NOREMOVE = 0x0000,
        /// <summary>
        /// 经过 <see cref="User32.PeekMessage"/> 处理后，消息将从队列中删除。
        /// </summary>
        PM_REMOVE = 0x0001,
        /// <summary>
        /// 防止系统释放任何等待调用方进入空闲状态的线程（请参见 WaitForInputIdle）。
        /// 将此值与 <see cref="PM_NOREMOVE"/> 或 <see cref="PM_REMOVE"/> 结合使用。
        /// </summary>
        PM_NOYIELD = 0x0002,
    }
    #endregion

    #region Structures    
    #endregion


    public static partial class User32
    {
        /// <summary>
        /// 将指定的消息发送到一个或多个窗口。该 SendMessage 函数的函数调用指定的窗口的窗口过程，并不会返回，直到窗口过程已经处理了该消息。
        /// <para>需要使用 HWND_BROADCAST 进行通信的应用程序应使用 <see cref="RegisterWindowMessage"/> 函数来获取用于应用程序间通信的唯一消息。</para>
        /// <para>要发送消息并立即返回，请使用 <see cref="SendMessageCallback"/> 或 <see cref="SendNotifyMessage"/> 函数。要将消息发布到线程的消息队列中并立即返回，请使用 <see cref="PostMessage"/> 或 <see cref="PostThreadMessage"/> 函数。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-sendmessage </para>
        /// </summary>
        /// <param name="hwnd">窗口的句柄，其窗口过程将接收到该消息。如果此参数为 HWND_BROADCAST((HWND)0xFFFF)，则消息将发送到系统中的所有顶级窗口，包括禁用或不可见的无主窗口，重叠的窗口和弹出窗口；但是消息不会发送到子窗口。
        /// <para>消息发送受 UIPI 约束。进程的线程只能将消息发送到完整性级别较低或相等的进程中的线程的消息队列。</para></param>
        /// <param name="msg">要发送的消息。</param>
        /// <param name="wParam">其他特定于消息的信息。</param>
        /// <param name="lParam">其他特定于消息的信息。</param>
        /// <returns>返回值指定消息处理的结果；这取决于发送的消息。使用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/> 检索错误。</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int SendMessage(IntPtr hwnd, MessageType msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// 将消息放置（张贴）在与创建指定窗口的线程相关联的消息队列中，并在不等待线程处理消息的情况下返回消息。
        /// <para>要将消息发布到与线程关联的消息队列中，请使用 <see cref="PostThreadMessage"/> 函数。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-postmessagea </para>
        /// </summary>
        /// <param name="hWnd">窗口的句柄，其窗口过程将接收消息。特殊值：
        /// <para>HWND_BROADCAST((HWND)0xFFFF) 该消息将发布到系统中的所有顶级窗口，包括禁用或不可见的无主窗口，重叠的窗口和弹出窗口。该消息未发布到子窗口。</para>
        /// <para>NULL 该函数的行为就像到呼叫 <see cref="PostThreadMessage"/> 与 dwThreadId 参数集到当前线程的标识符。</para>
        /// </param>
        /// <param name="Msg">要发布的消息类型</param>
        /// <param name="wParam">[WPARAM] 其他特定于消息的信息。</param>
        /// <param name="lParam">[LPARAM] 其他特定于消息的信息。</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/> </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, MessageType Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// 从调用线程的消息队列中检索消息。该函数分派传入的已发送消息，直到已发布的消息可供检索为止。与 <see cref="GetMessage"/> 不同 <see cref="PeekMessage"/> 函数在返回之前不等待消息发布。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getmessage </para>
        /// </summary>
        /// <param name="lpMsg">[LPMSG] 指向MSG结构的指针，该结构从线程的消息队列接收消息信息。</param>
        /// <param name="hWnd">要获取其消息的窗口的句柄。该窗口必须属于当前线程。
        /// <para>如果 hWnd 为 NULL，则 <see cref="GetMessage"/> 检索属于当前线程的任何窗口的消息，以及当前线程的消息队列中 hwnd 值为 NULL 的消息（请参阅MSG结构）。因此，如果 hWnd 为 NULL，则将同时处理窗口消息和线程消息。</para>
        /// <para>如果 hWnd 为 -1，则 <see cref="GetMessage"/> 仅检索当前线程的消息队列中其 hwnd 值为 NULL 的消息，即由 <see cref="PostMessage"/>（当 hWnd 参数为 NULL）或 <see cref="PostThreadMessage"/> 发布的线程消息 。</para>
        /// </param>
        /// <param name="wMsgFilterMin">要检索的最低消息值的整数值。使用 <see cref="MessageType.WM_KEYFIRST"/>(0x0100) 指定第一条键盘消息，或使用 <see cref="MessageType.WM_MOUSEFIRST"/>(0x0200) 指定第一条鼠标消息。</param>
        /// <param name="wMsgFilterMax">要检索的最高消息值的整数值。使用 <see cref="MessageType.WM_KEYLAST"/>  指定最后的键盘消息，或使用 <see cref="MessageType.WM_MOUSELAST"/>  指定最后的鼠标消息。</param>
        /// <returns>如果函数检索到 <see cref="MessageType.WM_QUIT"/> 以外的消息，则返回值为非零。如果该函数检索 <see cref="MessageType.WM_QUIT"/> 消息，则返回值为零。
        /// <para>如果有错误，则返回值为-1。例如，如果 hWnd 是无效的窗口句柄或 lpMsg 是无效的指针，该函数将失败。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</para>
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMessage(ref MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
        
        /// <summary>
        /// 调度传入的已发送消息，检查线程消息队列中是否有已发布消息，并检索消息（如果存在）。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-peekmessagea </para>
        /// </summary>
        /// <param name="lpMsg">[LPMSG] 指向接收消息信息的 <see cref="MSG"/> 结构的指针。</param>
        /// <param name="hWnd">要获取其消息的窗口的句柄。该窗口必须属于当前线程。
        /// <para>如果 hWnd 为 NULL，则 <see cref="PeekMessage"/> 检索属于当前线程的任何窗口的消息，以及当前线程的消息队列中 hwnd 值为 NULL 的消息（请参阅 <see cref="MSG"/> 结构）。因此，如果 hWnd 为 NULL，则将同时处理窗口消息和线程消息。</para>
        /// <para>如果 hWnd 为 -1，则 <see cref="PeekMessage"/> 仅检索当前线程的消息队列中其 hwnd 值为 NULL 的消息，即，由 <see cref="PostMessage"/>（当 hWnd 参数为 NULL）或 <see cref="PostThreadMessage"/> 发布的线程消息 。</para>
        /// </param>
        /// <param name="wMsgFilterMin">在要检查的消息范围内的第一条消息的值。使用 <see cref="MessageType.WM_KEYFIRST"/>(0x0100) 指定第一条键盘消息，或使用  <see cref="MessageType.WM_MOUSEFIRST"/>(0x0200) 指定第一条鼠标消息。
        ///     <para>如果 wMsgFilterMin 和 wMsgFilterMax 都为零，则 PeekMessage 返回所有可用消息（即，不执行范围过滤）。</para>
        /// </param>
        /// <param name="wMsgFilterMax">要检查的消息范围中的最后一条消息的值。使用 <see cref="MessageType.WM_KEYLAST"/> 指定最后的键盘消息，或使用 <see cref="MessageType.WM_MOUSELAST"/> 指定最后的鼠标消息。
        ///     <para>如果 wMsgFilterMin 和 wMsgFilterMax 都为零，则 <see cref="PeekMessage"/> 返回所有可用消息（即，不执行范围过滤）。</para>
        /// </param>
        /// <param name="wRemoveMsg">指定如何处理消息 <see cref="PmFlags"/>。</param>
        /// <returns>如果有消息，则返回值为非零。如果没有可用消息，则返回值为零。</returns>
        [DllImport("user32.dll")]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool PeekMessage(ref MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);
        
        /// <summary>
        /// 将消息信息传递到指定的窗口过程。
        /// <para>使用 <see cref="CallWindowProc"/> 函数进行窗口子类化。通常，具有相同类的所有窗口共享一个窗口过程。子类是具有相同类的一个窗口或一组窗口，其消息在传递给该类的窗口过程之前，已被另一个（或多个）过程拦截和处理。</para>
        /// <para>该 <see cref="SetWindowLong"/> 函数功能改变与特定窗口相关的窗口过程，导致系统调用新的窗口过程而不是以前一个创建子类。应用程序必须通过调用 <see cref="CallWindowProc"/> 将新窗口过程未处理的任何消息传递给前一个窗口过程。这允许应用程序创建一系列窗口过程。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-callwindowproca </para>
        /// </summary>
        /// <param name="lpPrevWndFunc">上一个窗口过程。如果通过在 nIndex 参数设置为 GWL_WNDPROC 或 DWL_DLGPROC 的情况下调用 <see cref="GetWindowLong"/> 函数获得此值，则它实际上是窗口或对话框过程的地址，或者是仅对 <see cref="CallWindowProc"/> 有意义的特殊内部值。</param>
        /// <param name="hWnd">接收消息的窗口过程的句柄。</param>
        /// <param name="Msg">[UINT] 消息类型</param>
        /// <param name="wParam">其他特定于消息的信息。此参数的内容取决于 Msg 参数的值。</param>
        /// <param name="lParam">其他特定于消息的信息。此参数的内容取决于 Msg 参数的值。</param>
        /// <returns>返回值指定消息处理的结果，并取决于发送的消息。</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr CallWindowProc(WindowProc lpPrevWndFunc, IntPtr hWnd, MessageType Msg, IntPtr wParam, IntPtr lParam);
        
        /// <summary>
        /// 调用默认窗口过程以为应用程序未处理的任何窗口消息提供默认处理。此功能确保处理所有消息。<see cref="DefWindowProc"/> 用窗口过程接收到的相同参数调用。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-defwindowproca </para>
        /// </summary>
        /// <param name="hWnd">接收到消息的窗口过程的句柄。</param>
        /// <param name="Msg">[UINT] 消息类型</param>
        /// <param name="wParam">附加消息信息。此参数的内容取决于 Msg 参数的值。</param>
        /// <param name="lParam">附加消息信息。此参数的内容取决于 Msg 参数的值。</param>
        /// <returns>返回值是消息处理的结果，并取决于消息。</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, MessageType Msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// 检索当前线程的额外消息（附加对象）信息。额外的消息信息是与当前线程的消息队列关联的应用程序或驱动程序定义的值。
        /// <para>若要设置线程的额外消息信息，请使用 <see cref="SetMessageExtraInfo"/> 函数。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getmessageextrainfo </para>
        /// </summary>
        /// <returns>[LPARAM] 返回值指定额外的信息。额外信息的含义是特定于设备的。</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetMessageExtraInfo();
        
        /// <summary>
        /// 设置当前线程的额外消息信息。额外的消息信息是与当前线程的消息队列关联的应用程序或驱动程序定义的值。应用程序可以使用 <see cref="GetMessageExtraInfo"/> 函数来检索线程的额外消息信息。
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setmessageextrainfo </para>
        /// </summary>
        /// <param name="lParam">与当前线程关联的值。</param>
        /// <returns>返回值是与当前线程关联的先前值。</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr SetMessageExtraInfo(IntPtr lParam);

    }
}
