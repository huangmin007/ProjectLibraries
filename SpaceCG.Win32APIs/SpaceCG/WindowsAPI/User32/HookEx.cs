using System;
using System.Runtime.InteropServices;

/***
 * 
 * 系统构子技术主题
 * 参考：https://docs.microsoft.com/zh-cn/windows/win32/winmsg/hooks
 * (未全部完成)
 * 
**/

namespace SpaceCG.WindowsAPI.User32
{

    #region Enumerations
    /// <summary>
    /// 挂钩过程的类型
    /// <para> <see cref="User32.SetWindowsHookEx"/> 函数数参考 idHook 的值之一 </para>
    /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowshookexa </para>
    /// <para>参考对应的结构数据：https://docs.microsoft.com/zh-cn/windows/win32/winmsg/hook-structures </para>
    /// </summary>
    public enum HookType:int
    {
        /// <summary>
        /// [线程或全局] 安装挂钩过程，以监视由于对话框，消息框，菜单或滚动条中的输入事件而生成的消息。有关更多信息，请参见 <see cref="MessageProc"/> 挂钩过程。
        /// </summary>
        WH_MSGFILTER = -1,
        /// <summary>
        /// [仅全局] 安装一个挂接过程，该过程记录发布到系统消息队列中的输入消息。该挂钩对于记录宏很有用。有关更多信息，请参见 <see cref="JournalRecordProc"/> 挂钩过程。
        /// </summary>
        WH_JOURNALRECORD = 0,
        /// <summary>
        /// [仅全局] 安装该消息发布之前由一个记录一个钩子程序 <see cref="WH_JOURNALRECORD"/> 钩子程序。欲了解更多信息，请参阅 <see cref="JournalPlaybackProc"/> 钩子程序。
        /// </summary>
        WH_JOURNALPLAYBACK = 1,
        /// <summary>
        /// [线程或全局] 安装挂钩过程，以监视击键消息。有关更多信息，请参见 <see cref="KeyboardProc"/> 挂接过程。
        /// </summary>
        WH_KEYBOARD = 2,
        /// <summary>
        /// [线程或全局] 安装挂钩过程，以监视发布到消息队列的消息。有关更多信息，请参见 <see cref="GetMsgProc"/> 挂钩过程。
        /// </summary>
        WH_GETMESSAGE = 3,
        /// <summary>
        /// [线程或全局] 安装挂钩程序，该程序在系统将消息发送到目标窗口过程之前监视消息。有关更多信息，请参见 <see cref="CallWndProc"/> 挂接过程。
        /// </summary>
        WH_CALLWNDPROC = 4,
        /// <summary>
        /// [线程或全局] 安装一个挂钩程序，该程序接收对 CBT 应用程序有用的通知。有关更多信息，请参见 <see cref="CBTProc"/> 挂钩过程。
        /// </summary>
        WH_CBT = 5,
        /// <summary>
        /// [仅全局] 安装挂钩过程，以监视由于对话框，消息框，菜单或滚动条中的输入事件而生成的消息。挂钩过程会在与调用线程相同的桌面中监视所有应用程序的这些消息。有关更多信息，请参见 <see cref="SysMsgProc"/> 挂接过程。
        /// </summary>
        WH_SYSMSGFILTER = 6,
        /// <summary>
        /// [线程或全局] 安装监视鼠标消息的挂钩过程。有关更多信息，请参见 <see cref="MouseProc"/> 挂钩过程。
        /// </summary>
        WH_MOUSE = 7,
        /// <summary>
        /// #if defined(_WIN32_WINDOWS) hardware
        /// </summary>
        WH_HARDWARE = 8,
        /// <summary>
        /// [线程或全局] 安装对调试其他挂钩过程有用的挂钩过程。有关更多信息，请参见 <see cref="DebugProc"/> 挂钩过程。
        /// </summary>
        WH_DEBUG = 9,
        /// <summary>
        /// [线程或全局] 安装一个挂钩程序，该程序接收对外壳程序有用的通知。有关更多信息，请参见 <see cref="ShellProc"/> 挂钩过程。
        /// </summary>
        WH_SHELL = 10,
        /// <summary>
        /// [线程或全局] 安装一个挂钩程序，当应用程序的前台线程即将变为空闲时将调用该挂钩程序。该挂钩对于在空闲时间执行低优先级任务很有用。有关更多信息，请参见 <see cref="ForegroundIdleProc"/> 挂钩过程。
        /// </summary>
        WH_FOREGROUNDIDLE = 11,
        /// <summary>
        /// [线程或全局] 安装挂钩过程，以监视目标窗口过程处理完的消息。有关更多信息，请参见 <see cref="CallWndRetProc"/> 挂接过程。
        /// </summary>
        WH_CALLWNDPROCRET = 12,
        /// <summary>
        /// [仅全局] 安装钩子程序，以监视低级键盘输入事件。有关更多信息，请参见 <see cref="LowLevelKeyboardProc"/> 挂钩过程。
        /// </summary>
        WH_KEYBOARD_LL = 13,
        /// <summary>
        /// [仅全局] 安装钩子过程，以监视低级鼠标输入事件。有关更多信息，请参见 <see cref="LowLevelMouseProc"/> 挂钩过程。
        /// </summary>
        WH_MOUSE_LL = 14,
    }
    #endregion


    #region Structures
    /// <summary>
    /// 包含有关低级键盘输入事件的信息。(KBDLLHOOKSTRUCT, *LPKBDLLHOOKSTRUCT, *PKBDLLHOOKSTRUCT)
    /// <para><see cref="HookType.WH_KEYBOARD_LL"/> 的数据结构体，<see cref="HookProc"/> 代理函数参数 lParam 数据结构体</para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/winmsg/hook-structures </para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/winmsg/hook-functions </para>
    /// </summary>
    public struct KBDLLHOOKSTRUCT
    {
        /// <summary>
        /// <see cref="VirtualKeyCode"/> 虚似键盘码
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public VirtualKeyCode vkCode;
        /// <summary>
        /// key 表示硬件扫描码 
        /// </summary>
        public uint scanCode;
        /// <summary>
        /// 扩展键标志，事件注入标志，上下文代码和过渡状态标志。该成员的指定如下。应用程序可以使用以下值来测试按键标志。测试 LLKHF_INJECTED（位4）将告诉您是否已注入事件。如果是这样，那么测试 LLKHF_LOWER_IL_INJECTED （位1）将告诉您是否从较低完整性级别运行的进程注入了事件。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-kbdllhookstruct?redirectedfrom=MSDN </para>
        /// </summary>
        public uint flags;
        /// <summary>
        /// 此消息的时间戳，等于此消息返回的 <see cref="User32.GetMessageTime"/>。
        /// </summary>
        public uint time;
        /// <summary>
        /// 与消息关联的其他信息。
        /// </summary>
        public UIntPtr dwExtraInfo;
        /// <summary>
        /// @ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[{nameof(KBDLLHOOKSTRUCT)}] vkCode:{vkCode}, scanCode:{scanCode}, flags:{flags}, time:{time}";
        }
    }

    /// <summary>
    /// 包含有关传递给 <see cref="MessageType.WH_MOUSE"/> 挂钩过程 <see cref="MouseProc"/> 的鼠标事件的信息。(MOUSEHOOKSTRUCT, * LPMOUSEHOOKSTRUCT, * PMOUSEHOOKSTRUCT)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-mousehookstruct?redirectedfrom=MSDN </para>
    /// </summary>
    public struct MOUSEHOOKSTRUCT
    {
        /// <summary>
        /// 光标的x和y坐标，以屏幕坐标表示。
        /// </summary>
        public POINT pt;
        /// <summary>
        /// 窗口的句柄，它将接收与 mouse 事件相对应的鼠标消息。
        /// </summary>
        public IntPtr hwnd;
        /// <summary>
        /// 命中测试值。有关命中测试值的列表，请参见 <see cref="MessageType.WM_NCHITTEST"/> 消息的描述。
        /// </summary>
        public uint wHitTestCode;
        /// <summary>
        /// 与消息关联的其他信息。
        /// </summary>
        public UIntPtr dwExtraInfo;
        /// <summary>
        /// @ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[{nameof(MOUSEHOOKSTRUCT)}] pt:{pt}, hwnd:{hwnd}, wHitTestCode:{wHitTestCode}, dwExtraInfo:{dwExtraInfo}";
        }
    }

    /// <summary>
    /// 包含有关低级鼠标输入事件的信息。(MSLLHOOKSTRUCT, *LPMSLLHOOKSTRUCT, *PMSLLHOOKSTRUCT)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-msllhookstruct?redirectedfrom=MSDN </para>
    /// </summary>
    public struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    /// <summary>
    /// 包含有关传递给 <see cref="HookType.WH_MOUSE"/> 挂钩过程 MouseProc 的鼠标事件的信息。 这是 <see cref="MOUSEHOOKSTRUCT"/> 结构的扩展，其中包括有关车轮移动或 X 按钮使用情况的信息。
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-mousehookstructex?redirectedfrom=MSDN </para>
    /// </summary>
    public struct MOUSEHOOKSTRUCTEX
    {
        public uint mouseData;
    }

    /// <summary>
    /// 包含有关发送到系统消息队列的硬件消息的信息。此结构用于存储 <see cref="JournalPlaybackProc"/> 回调函数的消息信息。(EVENTMSG, *PEVENTMSGMSG, *NPEVENTMSGMSG, *LPEVENTMSGMSG, *PEVENTMSG, *NPEVENTMSG, *LPEVENTMSG)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-eventmsg?redirectedfrom=MSDN </para>
    /// </summary>
    public struct EVENTMSG
    {
        public uint message;
        public uint paramL;
        public uint paramH;
        public uint time;
        public IntPtr hwnd;
    }

    /// <summary>
    /// 包含传递给 <see cref="HookType.WH_DEBUG"/> 挂钩过程 <see cref="DebugProc"/> 的调试信息。(DEBUGHOOKINFO, *PDEBUGHOOKINFO, *NPDEBUGHOOKINFO, *LPDEBUGHOOKINFO)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-debughookinfo?redirectedfrom=MSDN</para>
    /// </summary>
    public struct tagDEBUGHOOKINFO
    {
        public uint idThread;
        public uint idThreadInstaller;
        public IntPtr lParam;
        public IntPtr wParam;
        public int code;
    }

    /// <summary>
    /// 定义传递给 <see cref="HookType.WH_CALLWNDPROC"/> 挂钩过程 <see cref="CallWndProc"/> 的消息参数。(CWPSTRUCT, *PCWPSTRUCT, *NPCWPSTRUCT, *LPCWPSTRUCT)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-cwpstruct?redirectedfrom=MSDN</para>
    /// </summary>
    public struct CWPSTRUCT
    {
        public IntPtr lParam;
        public IntPtr wParam;
        public uint message;
        public IntPtr hwnd;
    }

    /// <summary>
    /// 定义传递给 <see cref="HookType.WH_CALLWNDPROCRET"/> 挂钩过程 <see cref="CallWndRetProc"/> 的消息参数。(CWPRETSTRUCT, *PCWPRETSTRUCT, *NPCWPRETSTRUCT, *LPCWPRETSTRUCT)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-cwpretstruct?redirectedfrom=MSDN </para>
    /// </summary>
    public struct CWPRETSTRUCT
    {
        public IntPtr lResult;
        public IntPtr lParam;
        public IntPtr wParam;
        public uint message;
        public IntPtr hwnd;
    }

    /// <summary>
    /// 包含在激活窗口之前传递给 <see cref="HookType.WH_CBT"/> 挂钩过程 <see cref="CBTProc"/> 的信息。(CBTACTIVATESTRUCT, *LPCBTACTIVATESTRUCT)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-cbtactivatestruct?redirectedfrom=MSDN </para>
    /// </summary>
    public struct CBTACTIVATESTRUCT
    {
        public bool fMouse;
        public IntPtr hWndActive;
    }

    /// <summary>
    /// 包含在创建窗口之前传递给 <see cref="HookType.WH_CBT"/> 挂钩过程 <see cref="CBTProc"/> 的信息。(CBT_CREATEWNDA, *LPCBT_CREATEWNDA)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-cbt_createwnda?redirectedfrom=MSDN </para>
    /// </summary>
    public struct CBT_CREATEWNDA
    {
        public IntPtr lpcs;   //struct CREATESTRUCTA *lpcs;
        public IntPtr hwndInsertAfter;
    }

    /// <summary>
    /// 定义传递给应用程序窗口过程的初始化参数。这些成员与 <see cref="User32.CreateWindowEx"/> 函数的参数相同。(CREATESTRUCTA, *LPCREATESTRUCTA)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-createstructa </para>
    /// </summary>
    public struct CREATESTRUCTA
    {
        public IntPtr lpCreateParams;
        public IntPtr hInstance;
        public IntPtr hMenu;
        public IntPtr hwndParent;
        public int cy;
        public int cx;
        public int y;
        public int x;
        public int style;
        public string lpszName;
        public string lpszClass;
        public uint dwExStyle;
    }
    #endregion


    #region Deletages
    /// <summary>
    /// 应用程序定义的功能，用于处理发送到窗口的消息。所述 WNDPROC 类型定义一个指向这个回调函数。WindowProc 是应用程序定义的函数名称的占位符。
    /// <para>参考 WPF <see cref="HwndSourceHook"/> </para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/previous-versions/windows/desktop/legacy/ms633573(v=vs.85) </para>
    /// </summary>
    /// <param name="hwnd">窗口的句柄。</param>
    /// <param name="uMsg">有关系统提供的消息的列表，请参阅系统定义的消息。</param>
    /// <param name="wParam">附加消息信息。此参数的内容取决于uMsg参数的值。</param>
    /// <param name="lParam">附加消息信息。此参数的内容取决于uMsg参数的值。</param>
    /// <returns>返回值是消息处理的结果，并取决于发送的消息。</returns>
    public delegate IntPtr WindowProc(IntPtr hwnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// HOOKPROC 回调函数
    /// <para>与 <see cref="User32.SetWindowsHookEx"/> 函数一起使用的应用程序定义或库定义的回调函数。调用 <see cref="User32.SendMessage"/> 函数后，系统将调用此函数。钩子程序可以检查消息；它不能修改它。</para>
    /// <para>所述 <see cref="HookProc"/> 类型定义一个指向这个回调函数。<see cref="User32.CallWndRetProc"/> 是应用程序定义或库定义的函数名称的占位符。</para>
    /// <para>应用程序通过在调用 <see cref="User32.SetWindowsHookEx"/> 函数时指定 <see cref="HookType.WH_CALLWNDPROCRET"/> 挂钩类型和指向该挂钩过程的指针来安装该挂钩过程。</para>
    /// <para>KeyboardProc 参考：https://docs.microsoft.com/zh-cn/windows/win32/winmsg/keyboardproc </para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nc-winuser-hookproc </para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/winmsg/hook-functions </para>
    /// </summary>
    /// <param name="nCode"></param>
    /// <param name="wParam">指定消息是否由当前进程发送。如果消息是由当前进程发送的，则该消息为非零；否则为0。否则为NULL。</param>
    /// <param name="lParam">指向 CWPRETSTRUCT 结构的指针，该结构包含有关消息的详细信息。</param>
    /// <returns>如果 nCode 小于零，则挂钩过程必须返回 <see cref="User32.CallNextHookEx"/> 返回的值。
    /// <para>如果 nCode 大于或等于零，则强烈建议您调用 <see cref="User32.CallNextHookEx"/> 并返回它返回的值。否则，其他安装了 WH_CALLWNDPROCRET 挂钩的应用程序将不会收到挂钩通知，因此可能会出现错误的行为。如果挂钩过程未调用 CallNextHookEx，则返回值应为零。</para>
    /// </returns>
    public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
    #endregion


    #region Notifications
    //WM_CANCELJOURNAL	
    //WM_QUEUESYNC
    #endregion
    

    /// <summary>
    /// 
    /// </summary>
    public static partial class User32
    {
        /// <summary>
        /// 将应用程序定义的挂钩过程安装到挂钩链中。您将安装一个挂钩过程来监视系统中的某些类型的事件。这些事件与特定线程或与调用线程在同一桌面上的所有线程相关联。
        /// <para>示例：当前APP:SetWindowsHookEx(idHook, HookProc, IntPtr.Zero, Kernel32.GetCurrentThreadId()); 全局:SetWindowsHookEx(idHook, HookProc, Process.GetCurrentProcess().MainModule.BaseAddress, 0);</para>
        /// <para>在终止之前，应用程序必须调用 <see cref="UnhookWindowsHookEx"/> 函数以释放与该挂钩关联的系统资源。</para>
        /// <para>调用 <see cref="CallNextHookEx"/> 函数链接到下一个挂钩过程是可选的，但强烈建议这样做；否则，其他已安装钩子的应用程序将不会收到钩子通知，因此可能会出现不正确的行为。除非绝对需要防止其他应用程序看到该通知，否则应调用 <see cref="CallNextHookEx"/>。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowshookexa </para>
        /// <para>示例：https://docs.microsoft.com/zh-cn/windows/win32/winmsg/using-hooks </para>
        /// <para>更多钩子函数：https://docs.microsoft.com/zh-cn/windows/win32/winmsg/hook-functions </para>
        /// </summary>
        /// <param name="idHook">要安装的挂钩过程的类型 <see cref="HookType"/></param>
        /// <param name="lpfn">指向钩子过程的指针。如果 dwThreadId 参数为零或指定由其他进程创建的线程的标识符，则 lpfn 参数必须指向 DLL 中的挂钩过程。否则，lpfn 可以指向与当前进程关联的代码中的挂钩过程。</param>
        /// <param name="hInstance">DLL 的句柄，其中包含由 lpfn 参数指向的挂钩过程。所述 HMOD 参数必须设置为 NULL，如果 dwThreadId 参数指定由当前进程，并且如果钩子程序是与当前过程相关联的所述代码中创建的线程。</param>
        /// <param name="threadId">挂钩过程将与之关联的线程的标识符。对于桌面应用程序，如果此参数为零，则挂钩过程与与调用线程在同一桌面上运行的所有现有线程相关联。</param>
        /// <returns>如果函数成功，则返回值是挂钩过程的句柄。如果函数失败，则返回值为NULL。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(HookType idHook, HookProc lpfn, IntPtr hInstance, uint threadId);

        /// <summary>
        /// 删除通过 <see cref="SetWindowsHookEx"/> 函数安装在挂钩链中的挂钩过程。
        /// <para>即使在 <see cref="UnhookWindowsHookEx"/> 返回之后，该挂钩过程也可以处于被另一个线程调用的状态。如果没有同时调用该挂钩过程，则在 <see cref="UnhookWindowsHookEx"/> 返回之前，立即删除该挂钩过程。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-unhookwindowshookex </para>
        /// <para>更多钩子函数：https://docs.microsoft.com/zh-cn/windows/win32/winmsg/hook-functions </para>
        /// </summary>
        /// <param name="hhk">钩子的手柄将被卸下。此参数是通过先前调用 <see cref="SetWindowsHookEx"/> 获得的挂钩句柄。</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /// <summary>
        /// 挂钩信息传递到当前挂钩链中的下一个挂钩过程。挂钩过程可以在处理挂钩信息之前或之后调用此函数。
        /// <para>对于特定的挂钩类型，挂钩程序是成链安装的。<see cref="CallNextHookEx"/> 调用链中的下一个钩子。</para>
        /// <para>调用 <see cref="CallNextHookEx"/> 是可选的，但强烈建议您使用；否则，其他已安装钩子的应用程序将不会收到钩子通知，因此可能会出现不正确的行为。除非绝对需要防止其他应用程序看到该通知，否则应调用 CallNextHookEx。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-callnexthookex </para>
        /// <para>更多钩子函数：https://docs.microsoft.com/zh-cn/windows/win32/winmsg/hook-functions </para>
        /// </summary>
        /// <param name="hhk">该参数被忽略。</param>
        /// <param name="nCode">挂钩代码传递给当前的挂钩过程。下一个挂钩过程将使用此代码来确定如何处理挂钩信息。</param>
        /// <param name="wParam">所述的 wParam 值传递到当前挂钩过程。此参数的含义取决于与当前挂钩链关联的挂钩的类型。</param>
        /// <param name="lParam">传递给当前挂钩过程的 lParam 值。此参数的含义取决于与当前挂钩链关联的挂钩的类型。</param>
        /// <returns>该值由链中的下一个挂钩过程返回。当前的挂钩过程还必须返回该值。返回值的含义取决于挂钩类型。有关更多信息，请参见各个挂钩过程的描述。</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// 将指定的消息和挂钩代码传递给与 <see cref="HookType.WH_SYSMSGFILTER"/> 和 <see cref="HookType.WH_MSGFILTER"/> 挂钩相关联的挂钩过程。 <see cref="HookType.WH_SYSMSGFILTER"/> 和 <see cref="HookType.WH_MSGFILTER"/> 挂钩过程是应用程序定义的回调函数，它检查并（可选）修改对话框，消息框，菜单或滚动条的消息。
        /// <para>系统调用 <see cref="CallMsgFilter"/> 来使应用程序能够在对话框，消息框，菜单和滚动条的内部处理过程中，或者当用户通过按 ALT+TAB 组合键激活其他窗口时，检查和控制消息流。</para>
        /// <para>通过使用 <see cref="SetWindowsHookEx"/> 函数安装此挂钩过程。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-callmsgfiltera?redirectedfrom=MSDN </para>
        /// </summary>
        /// <param name="lpMsg">[LPMSG] 指向 <see cref="MSG"/> 结构的指针，该结构包含要传递给挂钩过程的消息。</param>
        /// <param name="nCode">挂钩过程用来确定如何处理消息的应用程序定义的代码。该代码的值不得与与 <see cref="HookType.WH_SYSMSGFILTER"/> 和 <see cref="HookType.WH_MSGFILTER"/> 挂钩关联的系统定义的挂钩代码（MSGF_ 和 HC_）具有相同的值。</param>
        /// <returns>如果应用程序应进一步处理该消息，则返回值为零。 如果应用程序不应该进一步处理该消息，则返回值为非零。</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool CallMsgFilter(ref MSG lpMsg,  int nCode);
    }

    /// <summary>
    /// WindowsAPI User32库，扩展常用/通用，功能/函数，扩展示例，以及使用方式
    /// </summary>
    public static partial class User32Extension
    {
        /// <summary>
        /// <see cref="WindowProc"/> Arguments lParam
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static POINT LParamToPoint(int value)
        {
            return new POINT(value & 0xFFFF, value >> 16);
        }

        /// <summary>
        /// <see cref="WindowProc"/> Arguments lParam
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static POINT LParamToPoint(IntPtr value)
        {
            return LParamToPoint(value.ToInt32());
        }
    }

}
