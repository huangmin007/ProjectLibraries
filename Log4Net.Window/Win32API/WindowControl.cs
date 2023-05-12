using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

/***
 * 
 * 窗体控件相关函数
 * 
**/

namespace Win32API.User32
{

    #region Enumerations
    /// <summary>
    /// <see cref="User32.SetWindowPos(IntPtr, IntPtr, int, int, int, int, SwpFlags)"/> 函数参数 hWndInsertAfter 的值之一
    /// </summary>
    public enum SwpInsertAfter:int
    {
        /// <summary>
        /// 将窗口放置在所有非最上面的窗口上方（即，所有最上面的窗口的后面）。如果窗口已经是非最上面的窗口，则此标志无效。
        /// </summary>
        HWND_NOTOPMOST = -2,
        /// <summary>
        /// 将窗口置于所有非最上面的窗口上方；即使禁用窗口，窗口也将保持其最高位置。
        /// </summary>
        HWND_TOPMOST = -1,
        /// <summary>
        /// 将窗口置于Z顺序的顶部。
        /// </summary>
        HWND_TOP = 0,
        /// <summary>
        /// 将窗口置于Z顺序的底部。
        /// <para>如果hWnd参数标识了最顶部的窗口，则该窗口将失去其最顶部的状态，并放置在所有其他窗口的底部。</para>
        /// </summary>
        HWND_BOTTOM = 1,
    }

    /// <summary>
    /// <see cref="User32.SetWindowPos(IntPtr, IntPtr, int, int, int, int, SwpFlags)"/> 函数参数 wFlags 的值之一或组合值
    /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos </para>
    /// </summary>
    [Flags]
    public enum SwpFlags : uint
    {
        /// <summary>
        /// 保留当前大小（忽略 cx 和 cy 参数）
        /// </summary>
        NOSIZE = 0x0001,
        /// <summary>
        /// 保留当前位置（忽略 X 和 Y 参数）。
        /// </summary>
        NOMOVE = 0x0002,
        /// <summary>
        /// 保留当前的Z顺序（忽略 hWndInsertAfter 参数）
        /// </summary>
        NOZORDER = 0x0004,
        /// <summary>
        /// 不重绘更改。
        /// <para>如果设置了此标志，则不会发生任何重绘。这适用于工作区，非工作区（包括标题栏和滚动条）以及由于移动窗口而导致未显示的父窗口的任何部分。</para>
        /// <para>设置此标志后，应用程序必须显式使窗口和父窗口中需要重绘的任何部分无效或重绘。</para>
        /// </summary>
        NOREDRAW = 0x0008,
        /// <summary>
        /// 不激活窗口。
        /// <para>如果未设置此标志，则激活窗口并将其移至最顶层或非顶层组的顶部（取决于 hWndInsertAfter 参数的设置）。</para>
        /// </summary>
        NOACTIVATE = 0x0010,
        /// <summary>
        /// 在窗口周围绘制框架（在窗口的类描述中定义）
        /// </summary>
        DRAWFRAME = 0x0020,
        /// <summary>
        /// 应用使用 SetWindowLong 函数设置的新框架样式；将 WM_NCCALCSIZE 消息发送到窗口，即使未更改窗口的大小。
        /// <para>如果未指定此标志，则仅在更改窗口大小时才发送 WM_NCCALCSIZE </para>
        /// </summary>
        FRAMECHANGED = 0x0020,
        /// <summary>
        /// 显示窗口
        /// </summary>
        SHOWWINDOW = 0x0040,
        /// <summary>
        /// 隐藏窗口
        /// </summary>
        HIDEWINDOW = 0x0080,
        /// <summary>
        /// 丢弃客户区的全部内容。
        /// <para>如果未指定此标志，则在调整窗口大小或位置后，将保存客户区的有效内容并将其复制回客户区。</para>
        /// </summary>
        NOCOPYBITS = 0x0100,
        /// <summary>
        /// 不更改所有者窗口在 Z 顺序中的位置。
        /// </summary>
        NOOWNERZORDER = 0x0200,
        /// <summary>
        /// 与 SWP_NOOWNERZORDER 标志相同。
        /// </summary>
        NOREPOSITION = 0x0200,
        /// <summary>
        /// 阻止窗口接收 WM_WINDOWPOSCHANGING 消息
        /// </summary>
        NOSENDCHANGING = 0x0400,
        /// <summary>
        /// 防止生成 WM_SYNCPAINT 消息
        /// </summary>
        DEFERERASE = 0x2000,
        /// <summary>
        /// 如果调用线程和拥有窗口的线程连接到不同的输入队列，则系统会将请求发布到拥有窗口的线程
        /// <para>这样可以防止在其他线程处理请求时调用线程阻塞其执行</para>
        /// </summary>
        ASYNCWINDOWPOS = 0x4000,
    }

    /// <summary>
    /// <see cref="User32.GetWindow"/> 函数参考 uCmd 的值之一
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getwindow </para>
    /// </summary>
    public enum GwCmd:uint
    {
        /// <summary>
        /// 检索到的句柄标识 Z 顺序中最高的同一类型的窗口。
        /// <para>如果指定的窗口是最上面的窗口，则该句柄标识最上面的窗口。如果指定的窗口是顶级窗口，则该句柄标识顶级窗口。如果指定的窗口是子窗口，则该句柄标识同级窗口。</para>
        /// </summary>
        GW_HWNDFIRST = 0,
        /// <summary>
        /// 检索到的句柄标识 Z 顺序中最低的同一类型的窗口。
        /// <para>如果指定的窗口是最上面的窗口，则该句柄标识最上面的窗口。如果指定的窗口是顶级窗口，则该句柄标识顶级窗口。如果指定的窗口是子窗口，则该句柄标识同级窗口。</para>
        /// </summary>
        GW_HWNDLAST = 1,
        /// <summary>
        /// 检索到的句柄以 Z 顺序标识指定窗口下方的窗口。
        /// <para>如果指定的窗口是最上面的窗口，则该句柄标识最上面的窗口。如果指定的窗口是顶级窗口，则该句柄标识顶级窗口。如果指定的窗口是子窗口，则该句柄标识同级窗口。</para>
        /// </summary>
        GW_HWNDNEXT = 2,
        /// <summary>
        /// 检索到的句柄以 Z 顺序标识指定窗口上方的窗口。
        /// <para>如果指定的窗口是最上面的窗口，则该句柄标识最上面的窗口。如果指定的窗口是顶级窗口，则该句柄标识顶级窗口。如果指定的窗口是子窗口，则该句柄标识同级窗口。</para>
        /// </summary>
        GW_HWNDPREV = 3,
        /// <summary>
        /// 检索到的句柄标识指定窗口的所有者窗口（如果有）。
        /// </summary>
        GW_OWNER = 4,
        /// <summary>
        /// 如果指定的窗口是父窗口，则检索到的句柄在Z顺序的顶部标识子窗口。否则，检索到的句柄为 NULL。该功能仅检查指定窗口的子窗口。它不检查后代窗口。
        /// </summary>
        GW_CHILD = 5,
        /// <summary>
        /// 检索到的句柄标识指定窗口拥有的启用的弹出窗口（搜索使用通过 <see cref="GwCmd.GW_HWNDNEXT"/> 找到的第一个此类窗口）；否则，如果没有启用的弹出窗口，则检索到的句柄是指定窗口的句柄。
        /// </summary>
        GW_ENABLEDPOPUP = 6,
    }

    /// <summary>
    /// 系统窗体样式(System Window Style)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/winmsg/window-styles </para>
    /// </summary>
    [Flags]
    public enum WindowStyle : uint
    {
        /// <summary>
        /// 该窗口是一个重叠的窗口。重叠的窗口具有标题栏和边框。与 <see cref="WS_TILED"/> 样式相同。
        /// </summary>
        WS_OVERLAPPED = 0x00000000,
        /// <summary>
        /// 窗口是一个弹出窗口。此样式不能与 <see cref="WS_CHILD"/> 样式一起使用。
        /// </summary>
        WS_POPUP = 0x80000000,
        /// <summary>
        /// 该窗口是子窗口。具有这种样式的窗口不能具有菜单栏。此样式不能与 <see cref="WS_POPUP"/> 样式一起使用。
        /// </summary>
        WS_CHILD = 0x40000000,
        /// <summary>
        /// 最初将窗口最小化。与 <see cref="WS_ICONIC"/> 样式相同。
        /// </summary>
        WS_MINIMIZE = 0x20000000,
        /// <summary>
        /// 该窗口最初是可见的。可以使用 <see cref="User32.ShowWindow"/> 或 <see cref="User32.SetWindowPos(IntPtr, IntPtr, int, int, int, int, SwpFlags)"/> 函数打开和关闭此样式。
        /// </summary>
        WS_VISIBLE = 0x10000000,
        /// <summary>
        /// 该窗口最初被禁用。禁用的窗口无法接收来自用户的输入。要在创建窗口后更改此设置，请使用 <see cref="User32.EnableWindow"/> 函数。
        /// </summary>
        WS_DISABLED = 0x08000000,
        /// <summary>
        /// 相对于彼此剪辑子窗口；也就是说当特定的子窗口接收到 <see cref="MessageType.WM_PAINT"/> 消息时，<see cref="WS_CLIPSIBLINGS"/> 样式会将所有其他重叠的子窗口剪切到要更新的子窗口区域之外。如果未指定 <see cref="WS_CLIPSIBLINGS"/> 并且子窗口重叠，则在子窗口的客户区域内进行绘制时，可以在相邻子窗口的客户区域内进行绘制。
        /// </summary>
        WS_CLIPSIBLINGS = 0x04000000,
        /// <summary>
        /// 在父窗口内进行绘制时，不包括子窗口所占的区域。创建父窗口时使用此样式。
        /// </summary>
        WS_CLIPCHILDREN = 0x02000000,
        /// <summary>
        /// 该窗口最初被最大化。
        /// </summary>
        WS_MAXIMIZE = 0x01000000,
        /// <summary>
        /// 窗口具有标题栏（包括 <see cref="WS_BORDER"/> 样式）。<see cref="WS_BORDER"/> | <see cref="WS_DLGFRAME"/>  
        /// </summary>
        WS_CAPTION = 0x00C00000,
        /// <summary>
        /// 窗口具有细线边框。
        /// </summary>
        WS_BORDER = 0x00800000,
        /// <summary>
        /// 窗口具有通常用于对话框的样式的边框。具有这种样式的窗口不能具有标题栏。
        /// </summary>
        WS_DLGFRAME = 0x00400000,
        /// <summary>
        /// 该窗口具有垂直滚动条。
        /// </summary>
        WS_VSCROLL = 0x00200000,
        /// <summary>
        /// 该窗口具有水平滚动条。
        /// </summary>
        WS_HSCROLL = 0x00100000,
        /// <summary>
        /// 该窗口的标题栏上有一个窗口菜单。该 <see cref="WS_CAPTION"/> 风格也必须指定。
        /// </summary>
        WS_SYSMENU = 0x00080000,
        /// <summary>
        /// 窗口具有大小调整边框。与 <see cref="WS_SIZEBOX"/> 样式相同。
        /// </summary>
        WS_THICKFRAME = 0x00040000,
        /// <summary>
        /// 该窗口是一组控件中的第一个控件。该组由该第一个控件和在其后定义的所有控件组成，直到下一个具有 <see cref="WS_GROUP"/> 样式的下一个控件。每个组中的第一个控件通常具有 <see cref="WS_TABSTOP"/> 样式，以便用户可以在组之间移动。用户随后可以使用方向键将键盘焦点从组中的一个控件更改为组中的下一个控件。
        /// <para>您可以打开和关闭此样式以更改对话框导航。若要在创建窗口后更改此样式，请使用 <see cref="User32.SetWindowLong"/> 函数。</para>
        /// </summary>
        WS_GROUP = 0x00020000,
        /// <summary>
        /// 该窗口是一个控件，当用户按下 TAB 键时可以接收键盘焦点。按下 TAB 键可将键盘焦点更改为 <see cref="WS_TABSTOP"/> 样式的下一个控件。
        /// <para>您可以打开和关闭此样式以更改对话框导航。若要在创建窗口后更改此样式，请使用 <see cref="User32.SetWindowLong"/> 函数。为了使用户创建的窗口和无模式对话框可与制表符一起使用，请更改消息循环以调用 <see cref="User32.IsDialogMessage"/> 函数。</para>
        /// </summary>
        WS_TABSTOP = 0x00010000,
        /// <summary>
        /// 该窗口有一个最小化按钮。不能与 <see cref="WindowStyleEx.WS_EX_CONTEXTHELP"/> 样式结合使用。该 <see cref="WS_SYSMENU"/> 风格也必须指定。
        /// </summary>
        WS_MINIMIZEBOX = 0x00020000,
        /// <summary>
        /// 该窗口具有最大化按钮。不能与 <see cref="WindowStyleEx.WS_EX_CONTEXTHELP"/> 样式结合使用。该 <see cref="WS_SYSMENU"/> 风格也必须指定。
        /// </summary>
        WS_MAXIMIZEBOX = 0x00010000,
        /// <summary>
        /// 该窗口是一个重叠的窗口。重叠的窗口具有标题栏和边框。与 <see cref="WS_OVERLAPPED"/> 样式相同。
        /// </summary>
        WS_TILED = WS_OVERLAPPED,
        /// <summary>
        /// 最初将窗口最小化。与 <see cref="WS_MINIMIZE"/> 样式相同。
        /// </summary>
        WS_ICONIC = WS_MINIMIZE,
        /// <summary>
        /// 窗口具有大小调整边框。与 <see cref="WS_THICKFRAME"/> 样式相同。
        /// </summary>
        WS_SIZEBOX = WS_THICKFRAME,
        /// <summary>
        /// 该窗口是一个重叠的窗口。与 <see cref="WS_OVERLAPPEDWINDOW"/> 样式相同。
        /// </summary>
        WS_TILEDWINDOW = WS_OVERLAPPEDWINDOW,
        /// <summary>
        /// 该窗口是一个重叠的窗口。与 <see cref="WS_TILEDWINDOW"/> 样式相同。
        /// </summary>
        WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
        /// <summary>
        /// 该窗口是一个弹出窗口。该 <see cref="WS_CAPTION"/> 和 <see cref="WS_POPUPWINDOW"/> 风格一定要结合使窗口菜单可见。
        /// </summary>
        WS_POPUPWINDOW = WS_POPUP | WS_BORDER | WS_SYSMENU,
        /// <summary>
        /// 与 <see cref="WS_CHILD"/> 样式相同。
        /// </summary>
        WS_CHILDWINDOW = WS_CHILD,
    }

    /// <summary>
    /// 扩展窗体样式(Extended Window Styles)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/winmsg/extended-window-styles </para>
    /// </summary>
    [Flags]
    public enum WindowStyleEx : uint
    {
        /// <summary>
        /// 窗口有一个双边框。该窗口可以任选地用一个标题栏，通过指定所创建的 <see cref="WindowStyle.WS_CAPTION"/> 在样式 dwStyle 参数。
        /// </summary>
        WS_EX_DLGMODALFRAME = 0x00000001,
        /// <summary>
        /// 使用此样式创建的子窗口在创建或销毁时不会将 <see cref="MessageType.WM_PARENTNOTIFY"/> 消息发送到其父窗口。
        /// </summary>
        WS_EX_NOPARENTNOTIFY = 0x00000004,
        /// <summary>
        /// 该窗口应放置在所有非最上面的窗口上方，并且即使在停用该窗口的情况下也应保持在它们之上。若要添加或删除此样式，请使用 <see cref="User32.SetWindowPos(IntPtr, IntPtr, int, int, int, int, SwpFlags)"/> 函数。
        /// </summary>
        WS_EX_TOPMOST = 0x00000008,
        /// <summary>
        /// 该窗口接受拖放文件。
        /// </summary>
        WS_EX_ACCEPTFILES = 0x00000010,
        /// <summary>
        /// 在绘制窗口下方的兄弟姐妹（由同一线程创建）之前，不应绘制窗口。该窗口显示为透明，因为基础同级窗口的位已被绘制。
        /// <para>要获得透明性而没有这些限制，请使用 <see cref="User32.SetWindowRgn"/> 函数。</para>
        /// </summary>
        WS_EX_TRANSPARENT = 0x00000020,
        /// <summary>
        /// 该窗口是 MDI 子窗口。
        /// </summary>
        WS_EX_MDICHILD = 0x00000040,
        /// <summary>
        /// 该窗口旨在用作浮动工具栏。工具窗口的标题栏比普通标题栏短，并且窗口标题使用较小的字体绘制。
        /// <para>当用户按下 ALT+TAB 时，工具窗口不会出现在任务栏或对话框中。如果工具窗口具有系统菜单，则其图标不会显示在标题栏上。但是您可以通过右键单击或键入 ALT+SPACE 来显示系统菜单。</para>
        /// </summary>
        WS_EX_TOOLWINDOW = 0x00000080,
        /// <summary>
        /// 窗口的边框带有凸起的边缘。
        /// </summary>
        WS_EX_WINDOWEDGE = 0x00000100,
        /// <summary>
        /// 窗口的边框带有凹陷的边缘。
        /// </summary>
        WS_EX_CLIENTEDGE = 0x00000200,
        /// <summary>
        /// 窗口的标题栏包含一个问号。当用户单击问号时，光标将变为带有指针的问号。如果用户然后单击子窗口，则该子窗口会收到 <see cref="MessageType.WM_HELP"/> 消息。
        /// <para>子窗口应将消息传递给父窗口过程，该过程应使用 HELP_WM_HELP 命令调用 WinHelp 函数。帮助应用程序显示一个弹出窗口，通常包含子窗口的帮助。<see cref="WS_EX_CONTEXTHELP"/> 不能与 <see cref="WindowStyle.WS_MAXIMIZEBOX"/> 或 <see cref="WindowStyle.WS_MINIMIZEBOX"/> 样式一起使用。</para>
        /// </summary>
        WS_EX_CONTEXTHELP = 0x00000400,
        /// <summary>
        /// 该窗口具有通用的“右对齐”属性。这取决于窗口类。仅当外壳语言是希伯来语，阿拉伯语或其他支持阅读顺序对齐的语言时，此样式才有效。否则，样式将被忽略。
        /// <para>将 <see cref="WS_EX_RIGHT"/> 样式用于静态或编辑控件分别具有与使用 SS_RIGHT 或 ES_RIGHT 样式相同的效果。通过按钮控件使用此样式与使用 BS_RIGHT 和 BS_RIGHTBUTTON 样式具有相同的效果。</para>
        /// </summary>
        WS_EX_RIGHT = 0x00001000,
        /// <summary>
        /// 该窗口具有通用的左对齐属性。这是默认值。
        /// </summary>
        WS_EX_LEFT = 0x00000000,
        /// <summary>
        /// 如果外壳语言是希伯来语，阿拉伯语或其他支持阅读顺序对齐的语言，则使用从右到左的阅读顺序属性显示窗口文本。对于其他语言，样式将被忽略。
        /// </summary>
        WS_EX_RTLREADING = 0x00002000,
        /// <summary>
        /// 使用从左到右的阅读顺序属性显示窗口文本。这是默认值。
        /// </summary>
        WS_EX_LTRREADING = 0x00000000,
        /// <summary>
        /// 如果外壳语言是希伯来语，阿拉伯语或其他支持阅读顺序对齐的语言，则垂直滚动条（如果有）位于客户区域的左侧。对于其他语言，样式将被忽略。
        /// </summary>
        WS_EX_LEFTSCROLLBAR = 0x00004000,
        /// <summary>
        /// 垂直滚动条（如果有）在客户区的右侧。这是默认值。
        /// </summary>
        WS_EX_RIGHTSCROLLBAR = 0x00000000,
        /// <summary>
        /// 窗口本身包含子窗口，应参与对话框导航。如果指定了此样式，则对话框管理器在执行导航操作（例如处理 TAB 键，箭头键或键盘助记符）时会循环到此窗口的子级中。
        /// </summary>
        WS_EX_CONTROLPARENT = 0x00010000,
        /// <summary>
        /// 该窗口具有三维边框样式，旨在用于不接受用户输入的项目。
        /// </summary>
        WS_EX_STATICEDGE = 0x00020000,
        /// <summary>
        /// 可见时将顶级窗口强制到任务栏上。
        /// </summary>
        WS_EX_APPWINDOW = 0x00040000,
        /// <summary>
        /// 该窗口是一个重叠的窗口。
        /// </summary>
        WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE,
        /// <summary>
        /// 该窗口是调色板窗口，这是一个无模式对话框，显示了一系列命令。
        /// </summary>
        WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
        /// <summary>
        /// 窗户是一个分层的窗户。如果窗口中有一个不能用这种风格类样式之一 CS_OWNDC 或 CS_CLASSDC。
        /// <para>Windows 8： <see cref="WS_EX_LAYERED"/> 样式支持顶级窗口和子窗口。以前的 Windows 版本仅对顶级窗口支持 <see cref="WS_EX_LAYERED"/>。</para>
        /// </summary>
        WS_EX_LAYERED = 0x00080000,
        /// <summary>
        /// 该窗口不会将其窗口布局传递给其子窗口。
        /// </summary>
        WS_EX_NOINHERITLAYOUT = 0x00100000,
        /// <summary>
        /// 窗口不渲染到重定向表面。这适用于不具有可见内容或使用除表面以外的机制来提供其视觉效果的窗口。
        /// </summary>
        WS_EX_NOREDIRECTIONBITMAP = 0x00200000,
        /// <summary>
        /// 如果外壳语言是希伯来语，阿拉伯语或其他支持阅读顺序对齐的语言，则窗口的水平原点在右边缘。水平值增加到左侧。
        /// </summary>
        WS_EX_LAYOUTRTL = 0x00400000,
        /// <summary>
        /// 使用双缓冲以从下到上的绘制顺序绘制窗口的所有后代。从下到上的绘画顺序允许后代窗口具有半透明（alpha）和透明（color-key）效果，但前提是后代窗口也设置了 <see cref="WS_EX_TRANSPARENT"/> 位。双缓冲允许绘制窗口及其后代，而不会闪烁。如果窗口有此不能使用类样式之一 CS_OWNDC 或 CS_CLASSDC。Windows 2000：不支持此样式。
        /// </summary>
        WS_EX_COMPOSITED = 0x02000000,
        /// <summary>
        /// 当用户单击它时，以这种样式创建的顶级窗口不会成为前台窗口。当用户最小化或关闭前景窗口时，系统不会将此窗口置于前景。不应通过程序访问或使用讲述人等可访问技术通过键盘导航来激活该窗口。
        /// <para>要激活该窗口，请使用 <see cref="User32.SetActiveWindow"/> 或 <see cref="User32.SetForegroundWindow"/> 函数。默认情况下，该窗口不显示在任务栏上。要强制窗口显示在任务栏上，请使用 <see cref="WS_EX_APPWINDOW"/> 样式。</para>
        /// </summary>
        WS_EX_NOACTIVATE = 0x08000000,
    }

    /// <summary>
    /// <see cref="User32.ShowWindow"/> 函数参考 nCmdShow 的值之一
    /// </summary>
    public enum SwCmd:int
    {
        /// <summary>
        /// 隐藏该窗口并激活另一个窗口。
        /// </summary>
        HIDE = 0,
        /// <summary>
        /// 激活并显示一个窗口。如果窗口最小化或最大化，则系统会将其还原到其原始大小和位置。首次显示窗口时，应用程序应指定此标志。
        /// </summary>
        SHOWNORMAL = 1,
        /// <summary>
        /// 激活窗口并将其显示为最小化窗口。
        /// </summary>
        SHOWMINIMIZED = 2,
        /// <summary>
        /// 激活窗口并将其显示为最大化窗口。
        /// </summary>
        SHOWMAXIMIZED = 3,
        /// <summary>
        /// 最大化指定的窗口。
        /// </summary>
        MAXIMIZE = 3,
        /// <summary>
        /// 以最新大小和位置显示窗口。该值类似于SW_SHOWNORMAL，除了未激活窗口。
        /// </summary>
        SHOWNOACTIVATE = 4,
        /// <summary>
        /// 激活窗口并以其当前大小和位置显示它。
        /// </summary>
        SHOW = 5,
        /// <summary>
        /// 最小化指定的窗口并以Z顺序激活下一个顶级窗口。
        /// </summary>
        MINIMIZE = 6,
        /// <summary>
        /// 将窗口显示为最小化窗口。该值类似于 SW_SHOWMINIMIZED，除了未激活窗口。
        /// </summary>
        SHOWMINNOACTIVE = 7,
        /// <summary>
        /// 以当前大小和位置显示窗口。该值与SW_SHOW相似，除了不激活窗口。
        /// </summary>
        SHOWNA = 8,
        /// <summary>
        /// 激活并显示窗口。如果窗口最小化或最大化，则系统会将其还原到其原始大小和位置。恢复最小化窗口时，应用程序应指定此标志。
        /// </summary>
        RESTORE = 9,
        /// <summary>
        /// 根据启动应用程序的程序传递给 <see cref="User32.CreateProcess"/> 函数的 <see cref="STARTUPINFO"/> 结构中指定的SW_值设置显示状态。
        /// </summary>
        SHOWDEFAULT = 10,
        /// <summary>
        /// 最小化一个窗口，即使拥有该窗口的线程没有响应。仅当最小化来自其他线程的窗口时，才应使用此标志。
        /// </summary>
        FORCEMINIMIZE = 11,

        //MAX = 11,
    }

    /// <summary>
    /// <see cref="GetWindowRgn"/> 函数返回值之一 （排列值存在问题）
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getwindowrgn </para>
    /// </summary>
    public enum GwrResult
    {
        /// <summary>
        /// 空区域，该区域为空。
        /// </summary>
        NULLREGION,
        /// <summary>
        /// 简单区域，该区域是单个矩形。
        /// </summary>
        SIMPLEREGION,
        /// <summary>
        /// 复杂区域，该区域不止一个矩形。
        /// </summary>
        COMPLEXREGION,
        /// <summary>
        /// 错误，指定的窗口没有区域，或者尝试返回该区域时发生错误。
        /// </summary>
        ERROR,
    }
    #endregion


    #region Structures
    /// <summary>
    /// POINT 结构定义点的 x 和 y 坐标。(POINT, POINTL, *PPOINT, *NPPOINT, *LPPOINT)
    /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/windef/ns-windef-point </para>
    /// </summary>
    public struct POINT
    {
        /// <summary>
        /// 指定点的x坐标
        /// </summary>
        public int x;
        /// <summary>
        /// 指定点的y坐标
        /// </summary>
        public int y;
        /// <summary>
        /// POINT 结构体
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public POINT(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        /// <summary>
        /// @ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[{nameof(POINT)}] x:{x}, y:{y}";
        }
    }

    /// <summary>
    /// RECT 结构通过其左上角和右下角的坐标定义一个矩形。(RECT, RECTL, *PRECT, NEAR *NPRECT, FAR *LPRECT)
    /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/windef/ns-windef-rect </para>
    /// </summary>
    public struct RECT
    {
        /// <summary>
        /// 指定矩形左上角的x坐标
        /// </summary>
        public int left;
        /// <summary>
        /// 指定矩形左上角的y坐标
        /// </summary>
        public int top;
        /// <summary>
        /// 指定矩形右下角的x坐标。
        /// </summary>
        public int right;
        /// <summary>
        /// 指定矩形右下角的y坐标。
        /// </summary>
        public int buttom;
        /// <summary>
        /// RECT 结构体
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="buttom"></param>
        public RECT(int left, int top, int right, int buttom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.buttom = buttom;
        }

        /// <summary>
        /// @ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[RECT] left:{left}, top:{top}, right:{right}, buttom:{buttom}";
        }

        #region RECT Functions
        /// <summary>
        /// 复制一个矩形的坐标到另一个。
        /// <para>因为应用程序可以将矩形用于不同的目的，所以矩形函数不使用显式的度量单位。相反，所有矩形坐标和尺寸均以带符号的逻辑值给出。映射模式和使用矩形的功能确定度量单位。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-copyrect </para>
        /// </summary>
        /// <param name="lprc">指向 <see cref="RECT"/> 结构的指针，该结构接收源矩形的逻辑坐标。</param>
        /// <param name="lprcSrc">指向要以逻辑单位复制其坐标的 <see cref="RECT"/> 结构的指针。</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CopyRect(out RECT lprc, ref RECT lprcSrc);

        /// <summary>
        /// 设置指定矩形的坐标。这等效于将 left，top，right 和 bottom 参数分配给RECT结构的适当成员。
        /// <para>因为应用程序可以将矩形用于不同的目的，所以矩形函数不使用显式的度量单位。相反，所有矩形坐标和尺寸均以带符号的逻辑值给出。映射模式和使用矩形的功能确定度量单位。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setrect </para>
        /// </summary>
        /// <param name="lprc">指向包含要设置的矩形的 <see cref="RECT"/> 结构的指针。</param>
        /// <param name="xLeft">指定矩形左上角的 x 坐标。</param>
        /// <param name="yTop">指定矩形左上角的 y 坐标。</param>
        /// <param name="xRight">指定矩形右下角的 x 坐标。</param>
        /// <param name="yBottom">指定矩形右下角的 y 坐标。</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetRect(out RECT lprc, int xLeft, int yTop, int xRight, int yBottom);

        /// <summary>
        /// 创建一个空的矩形，其中所有的坐标都设置为零。
        /// <para>因为应用程序可以将矩形用于不同的目的，所以矩形函数不使用显式的度量单位。相反，所有矩形坐标和尺寸均以带符号的逻辑值给出。映射模式和使用矩形的功能确定度量单位。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setrectempty </para>
        /// </summary>
        /// <param name="lprc">指向包含矩形坐标的 <see cref="RECT"/> 结构的指针。</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetRectEmpty(out RECT lprc);

        /// <summary>
        /// 确定指定的矩形是否为空。空矩形是没有面积的矩形。也就是说，右侧的坐标小于或等于左侧的坐标，或者底侧的坐标小于或等于顶侧的坐标。
        /// <para>因为应用程序可以将矩形用于不同的目的，所以矩形函数不使用显式的度量单位。相反，所有矩形坐标和尺寸均以带符号的逻辑值给出。映射模式和使用矩形的功能确定度量单位。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-isrectempty </para>
        /// </summary>
        /// <param name="lprc">指向包含矩形逻辑坐标的 <see cref="RECT"/> 结构的指针。</param>
        /// <returns>如果矩形为空，则返回值为非零。如果矩形不为空，则返回值为零。</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsRectEmpty(ref RECT lprc);

        /// <summary>
        /// 确定两个指定的矩形是否通过比较它们的左上角和右下角的坐标相等。
        /// <para>该函数没有把空矩形作为平等的，如果它们的坐标是不同的。</para>
        /// <para>因为应用程序可以将矩形用于不同的目的，所以矩形函数不使用显式的度量单位。相反所有矩形坐标和尺寸均以带符号的逻辑值给出。映射模式和使用矩形的功能确定度量单位。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-equalrect </para>
        /// </summary>
        /// <param name="lprc1">指向 RECT 结构的指针，该结构包含第一个矩形的逻辑坐标。</param>
        /// <param name="lprc2">指向 RECT 结构的指针，该结构包含第二个矩形的逻辑坐标。</param>
        /// <returns>如果两个矩形相同，则返回值为非零。如果两个矩形不相同，则返回值为零。</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EqualRect(ref RECT lprc1, ref RECT lprc2);

        /// <summary>
        /// 确定是否指定的矩形内的指定点所在。如果一个点位于矩形的左侧或顶部，或者位于所有四个侧面，则该点位于矩形内。右侧或底部的一个点被认为是在矩形的外部。
        /// <para>必须在调用 <see cref="PtInRect"/> 之前将矩形标准化。也就是说，lprc.right 必须大于 lprc.left，而 lprc.bottom 必须大于 lprc.top。如果矩形未标准化，则永远不会在矩形内部考虑点。</para>
        /// <para>因为应用程序可以将矩形用于不同的目的，所以矩形函数不使用显式的度量单位。相反，所有矩形坐标和尺寸均以带符号的逻辑值给出。映射模式和使用矩形的功能确定度量单位。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-ptinrect </para>
        /// </summary>
        /// <param name="lprc">指向包含指定矩形的 <see cref="RECT"/> 结构的指针。</param>
        /// <param name="pt">一个 <see cref="POINT"/> 结构，包含指定点。</param>
        /// <returns>如果指定点位于矩形内，则返回值为非零。如果指定的点不在矩形内，则返回值为零。</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PtInRect(ref RECT lprc, POINT pt);

        /// <summary>
        /// 增加或减少指定的矩形的宽度和高度。所述 <see cref="InflateRect"/> 函数添加 dx 单位矩形的和左和右端 dy 单元的顶部和底部。在 dx 和 dy 参数符号值; 正值增加宽度和高度，负值减小宽度和高度。
        /// <para>因为应用程序可以将矩形用于不同的目的，所以矩形函数不使用显式的度量单位。相反所有矩形坐标和尺寸均以带符号的逻辑值给出。映射模式和使用矩形的功能确定度量单位。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-inflaterect </para>
        /// </summary>
        /// <param name="lprc">[LPRECT] 指向大小增加或减小的 <see cref="RECT"/> 结构的指针。</param>
        /// <param name="dx">增大或减小矩形宽度的量。此参数必须为负数以减小宽度。</param>
        /// <param name="dy">增加或减少矩形高度的数量。此参数必须为负数以减小高度。</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool InflateRect(ref RECT lprc, int dx, int dy);

        /// <summary>
        /// 由指定的偏移量移动指定的矩形。
        /// <para>因为应用程序可以将矩形用于不同的目的，所以矩形函数不使用显式的度量单位。相反，所有矩形坐标和尺寸均以带符号的逻辑值给出。映射模式和使用矩形的功能确定度量单位。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-offsetrect </para>
        /// </summary>
        /// <param name="lprc">指向 <see cref="RECT"/> 结构的指针，该结构包含要移动的矩形的逻辑坐标。</param>
        /// <param name="dx">指定向左或向右移动矩形的量。此参数必须为负值，以将矩形向左移动。</param>
        /// <param name="dy">指定向上或向下移动矩形的量。此参数必须为负值才能向上移动矩形。</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OffsetRect(ref RECT lprc, int dx, int dy);

        /// <summary>
        /// 计算两个源矩形的交集与交点矩形的坐标放入目标矩形。如果源矩形不相交，则将一个空矩形（所有坐标均设置为零）放置到目标矩形中。
        /// <para>因为应用程序可以将矩形用于不同的目的，所以矩形函数不使用显式的度量单位。相反，所有矩形坐标和尺寸均以带符号的逻辑值给出。映射模式和使用矩形的功能确定度量单位。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-intersectrect </para>
        /// </summary>
        /// <param name="lprcDst">指向 <see cref="RECT"/> 结构的指针，该结构将接收 lprcSrc1 和 lprcSrc2 参数指向的矩形的交集。此参数不能为 NULL。</param>
        /// <param name="lprc1">指向包含第一个源矩形的 <see cref="RECT"/> 结构的指针。</param>
        /// <param name="lprc2">指向包含第二个源矩形的 <see cref="RECT"/> 结构的指针。</param>
        /// <returns>如果矩形相交，则返回值为非零。如果矩形不相交，则返回值为零。</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IntersectRect(out RECT lprcDst, ref RECT lprc1, ref RECT lprc2);

        /// <summary>
        /// 确定一个矩形的由从另一个中减去一个矩形形成的坐标。
        /// <para>该函数仅减去由指定的矩形 lprcSrc2 从由指定的矩形 lprcSrc1 当矩形无论是在X或Y方向上完全相交。
        /// 例如，如果 lprcSrc1 具有坐标（10,10,100,100），而 lprcSrc2 具有坐标（50,50,150,150），则该函数会将 lprcDst 指向的矩形的坐标设置为（10,10,100,100）。
        /// 如果 lprcSrc1 具有坐标（10,10,100,100）并且 lprcSrc2 具有坐标（50,10,150,150），但是，该函数设置 lprcDst 指向的矩形的坐标到（10,10,50,100）。换句话说，所得的矩形是几何差异的边界框。</para>
        /// <para>因为应用程序可以将矩形用于不同的目的，所以矩形函数不使用显式的度量单位。相反，所有矩形坐标和尺寸均以带符号的逻辑值给出。映射模式和使用矩形的功能确定度量单位。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-subtractrect </para>
        /// </summary>
        /// <param name="lprcDst">指向一个 <see cref="RECT"/> 接收矩形的坐标结构减去由指向的矩形确定 lprcSrc2 从矩形指向 lprcSrc1。</param>
        /// <param name="lprc1">指向 <see cref="RECT"/> 结构的指针，该函数从中减去 lprcSrc2 指向的矩形。</param>
        /// <param name="lprc2">该函数从 lprcSrc1 指向的矩形中减去的 <see cref="RECT"/> 结构的指针。</param>
        /// <returns>如果结果矩形为空，则返回值为零。如果结果矩形不为空，则返回值为非零。</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SubtractRect(out RECT lprcDst, ref RECT lprc1, ref RECT lprc2);

        /// <summary>
        /// 创建两个矩形的联合。联合是包含两个源矩形的最小矩形。
        /// <para>系统会忽略空矩形的尺寸，即所有坐标均设置为零的矩形，因此它没有高度或宽度。</para>
        /// <para>因为应用程序可以将矩形用于不同的目的，所以矩形函数不使用显式的度量单位。相反，所有矩形坐标和尺寸均以带符号的逻辑值给出。映射模式和使用矩形的功能确定度量单位。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-unionrect </para>
        /// </summary>
        /// <param name="lprcDst">指向 <see cref="RECT"/> 结构的指针，该结构将接收一个包含 lprcSrc1 和 lprcSrc2 参数指向的矩形的矩形。</param>
        /// <param name="lprc1">指向包含第一个源矩形的 <see cref="RECT"/> 结构的指针。</param>
        /// <param name="lprc2">指向包含第二个源矩形的 <see cref="RECT"/> 结构的指针。</param>
        /// <returns>如果指定的结构包含非空矩形，则返回值为非零。如果指定的结构不包含非空矩形，则返回值为零。</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnionRect(out RECT lprcDst, ref RECT lprc1, ref RECT lprc2);
        #endregion
    }


    /// <summary>
    /// 包含窗口信息。(WINDOWINFO, * PWINDOWINFO, * LPWINDOWINFO)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-windowinfo </para>
    /// </summary>
    public struct WINDOWINFO
    {
        /// <summary>
        /// 结构的大小，以字节为单位。呼叫者必须将此成员设置为 sizeof(<see cref="WINDOWINFO"/>)。
        /// </summary>
        public uint cbSize;
        /// <summary>
        /// 窗口的坐标。
        /// </summary>
        public RECT rcWindow;
        /// <summary>
        /// 客户区域的坐标。
        /// </summary>
        public RECT rcClient;
        /// <summary>
        /// 窗口样式。有关窗口样式的表，请参见 <see cref="WindowStyle"/>。
        /// </summary>
        public WindowStyle dwStyle;
        /// <summary>
        /// 扩展的窗口样式。有关扩展窗口样式的表，请参见 。
        /// </summary>
        public WindowStyleEx dwExStyle;
        /// <summary>
        /// 窗口状态。如果此成员是 WS_ACTIVECAPTION（0x0001），则该窗口处于活动状态。否则，该成员为零。
        /// </summary>
        public uint dwWindowStatus;
        /// <summary>
        /// 窗口边框的宽度，以像素为单位。
        /// </summary>
        public uint cxWindowBorders;
        /// <summary>
        /// 窗口边框的高度，以像素为单位。
        /// </summary>
        public uint cyWindowBorders;
        /// <summary>
        /// 窗口类原子（请参见 RegisterClass）。
        /// </summary>
        public ushort atomWindowType;
        /// <summary>
        /// 创建窗口的应用程序的 Windows 版本。
        /// </summary>
        public ushort wCreatorVersion;

        /// <summary>
        /// <see cref="WINDOWINFO"/> 结构体字节大小
        /// </summary>
        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(WINDOWINFO));

        /// <summary>
        /// 创建一个已经设置 cbSize 大小的 <see cref="WINDOWINFO"/> 对象。
        /// </summary>
        /// <returns></returns>
        public static WINDOWINFO Create()
        {
            return new WINDOWINFO() { cbSize = Size };
        }
        /// <summary>
        /// @ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[WINDOWINFO] rcWindow:{rcWindow}, rcClient:{rcClient}, dwStyle:{dwStyle}, dwExStyle:{dwExStyle}, dwWindowStatus:{dwWindowStatus}, cxWindowBorders:{cxWindowBorders}, cyWindowBorders:{cyWindowBorders}, atomWindowType:{atomWindowType}, wCreatorVersion:{wCreatorVersion}";
        }
    }

    /// <summary>
    /// 包含有关窗口的大小和位置的信息。(WINDOWPOS, * LPWINDOWPOS, * PWINDOWPOS)
    /// <para><see cref="BeginDeferWindowPos"/>, <see cref="DeferWindowPos"/>, <see cref="EndDeferWindowPos"/></para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-windowpos?redirectedfrom=MSDN </para>
    /// </summary>
    public struct WINDOWPOS
    {
        /// <summary>
        /// 窗口在Z顺序中的位置（前后位置）。该成员可以是放置该窗口的窗口的句柄，也可以是 <see cref="User32.SetWindowPos(IntPtr, int, int, int, int, int, SwpFlags)"/> 函数列出的特殊值之一。
        /// </summary>
        public IntPtr hwndInsertAfter;
        /// <summary>
        /// 窗口的句柄。
        /// </summary>
        public IntPtr hwnd;
        /// <summary>
        /// 窗口左边缘的位置。
        /// </summary>
        public int x;
        /// <summary>
        /// 窗口顶部边缘的位置。
        /// </summary>
        public int y;
        /// <summary>
        /// 窗口宽度，以像素为单位。
        /// </summary>
        public int cx;
        /// <summary>
        /// 窗口高度，以像素为单位。
        /// </summary>
        public int cy;
        /// <summary>
        /// 窗口位置。该成员可以是 <see cref="SwpFlags"/> 一个或多个值。
        /// </summary>
        public SwpFlags flags;
    }
    #endregion


    #region Deletages
    /// <summary>
    /// 与 <see cref="User32.EnumWindows"/> 或 <see cref="User32.EnumDesktopWindows"/> 函数一起使用的应用程序定义的回调函数。它接收顶级窗口句柄。lpEnumFunc 类型定义一个指向这个回调函数。<see cref="User32.EnumWindowsProc"/> 是应用程序定义的函数名称的占位符。
    /// <para>参考：https://docs.microsoft.com/zh-cn/previous-versions/windows/desktop/legacy/ms633498(v=vs.85) </para>
    /// </summary>
    /// <param name="hwnd">顶级窗口的句柄。</param>
    /// <param name="lParam">在 <see cref="User32.EnumWindows"/> 或 <see cref="User32.EnumDesktopWindows"/> 中给出的应用程序定义的值。</param>
    /// <returns>要继续枚举，回调函数必须返回 TRUE；要停止枚举，它必须返回 FALSE。</returns>
    public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    /// <summary>
    /// 与 <see cref="User32.EnumChildWindows"/> 函数一起使用的应用程序定义的回调函数。它接收子窗口句柄。lpEnumFunc 类型定义一个指向这个回调函数。<see cref="EnumChildProc"/> 是应用程序定义的函数名称的占位符。
    /// <para>参考：https://docs.microsoft.com/zh-cn/previous-versions/windows/desktop/legacy/ms633493(v=vs.85) </para>
    /// </summary>
    /// <param name="hwnd">在 <see cref="User32.EnumChildWindows"/> 中指定的父窗口的子窗口的句柄。</param>
    /// <param name="lParam"> 在 <see cref="User32.EnumChildWindows"/> 中给定的应用程序定义的值。</param>
    /// <returns>要继续枚举，回调函数必须返回 TRUE；要停止枚举，它必须返回 FALSE。</returns>
    public delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);
    #endregion


    internal static partial class User32
    {

        #region Window Position
        /// <summary>
        /// 更改子窗口，弹出窗口或顶级窗口的大小，位置和Z顺序；这些窗口是根据其在屏幕上的外观排序的；最顶部的窗口获得最高排名，并且是Z顺序中的第一个窗口。
        /// <para>如果使用 <see cref="SetWindowLong"/> 更改了某些窗口数据，则必须调用 <see cref="SetWindowPos(IntPtr,IntPtr,int,int, int,int, SwpFlags)"/> 才能使更改生效。对 uFlags 使用以下组合：SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED。</para>
        /// <para>示例：SetWindowPos(new WindowInteropHelper(this).Handle., (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SwpFlags.SWP_NOMOVE | SwpFlags.SWP_NOSIZE); //将窗口 Z 序设置为最顶</para>
        /// <para>示例：SetWindowPos(hWnd, (IntPtr)HWND_TOPMOST, 10, 10, 800, 600, SwpFlags.SWP_NOZORDER ); //设置窗口大小及位置，忽略 Z 序</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos </para>
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="hWndInsertAfter">在Z顺序中位于定位的窗口之前的窗口位置。见 <see cref="SwpInsertAfter"/></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <param name="wFlags">窗口大小和位置标志 <see cref="SwpFlags"/></param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/> 。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SwpFlags wFlags);
        /// <summary>
        /// 更改子窗口，弹出窗口或顶级窗口的大小，位置和Z顺序；这些窗口是根据其在屏幕上的外观排序的；最顶部的窗口获得最高排名，并且是Z顺序中的第一个窗口。
        /// <para>如果使用 <see cref="SetWindowLong"/> 更改了某些窗口数据，则必须调用 <see cref="SetWindowPos(IntPtr,IntPtr,int,int, int,int, SwpFlags)"/> 才能使更改生效。对 uFlags 使用以下组合：SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED。</para>
        /// <para>示例：SetWindowPos(new WindowInteropHelper(this).Handle., (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SwpFlags.SWP_NOMOVE | SwpFlags.SWP_NOSIZE); //将窗口 Z 序设置为最顶</para>
        /// <para>示例：SetWindowPos(hWnd, (IntPtr)HWND_TOPMOST, 10, 10, 800, 600, SwpFlags.SWP_NOZORDER ); //设置窗口大小及位置，忽略 Z 序</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos </para>
        /// </summary>
        /// <param name="hWnd">A handle to the window.</param>
        /// <param name="hWndInsertAfter">在Z顺序中位于定位的窗口之前的窗口位置。见 <see cref="SwpInsertAfter"/></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        /// <param name="wFlags">窗口大小和位置标志 <see cref="SwpFlags"/></param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/> 。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, [MarshalAs(UnmanagedType.U4)]SwpInsertAfter hWndInsertAfter, int x, int y, int cx, int cy, SwpFlags wFlags);
        
        /// <summary>
        /// 更改指定窗口的位置和尺寸。对于顶级窗口，位置和尺寸是相对于屏幕的左上角的。对于子窗口，它们相对于父窗口客户区的左上角。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-movewindow </para>
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="nWidth"></param>
        /// <param name="nHeight"></param>
        /// <param name="bRepaint">指示是否要重新绘制窗口。
        /// <para>如果此参数为 TRUE，则窗口会收到一条消息。如果参数为 FALSE，则不会进行任何重绘。这适用于客户区域，非客户区域（包括标题栏和滚动栏）以及由于移动子窗口而暴露的父窗口的任何部分。</para></param>
        /// <returns>如果函数成功，则返回值为非零。</returns>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);
        #endregion



        #region Window Enum & Info
        /// <summary>
        /// 检索与指定窗口具有指定关系（Z 顺序或所有者）的窗口的句柄。
        /// <para>#define GetNextWindow(hWnd, wCmd) GetWindow(hWnd, wCmd);//GW_HWNDNEXT,GW_HWNDPREV</para>
        /// <para>与循环调用 <see cref="GetWindow"/> 相比，<see cref="EnumChildWindows"/> 函数更可靠。调用 <see cref="GetWindow"/> 来执行此任务的应用程序可能会陷入无限循环或引用已被破坏的窗口的句柄。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getwindow </para>
        /// </summary>
        /// <param name="hWnd">窗口的句柄。基于 uCmd 参数的值，检索到的窗口句柄是与此窗口相对的。</param>
        /// <param name="uCmd">指定窗口和要获取其句柄的窗口之间的关系</param>
        /// <returns>如果函数成功，则返回值为窗口句柄。如果不存在与指定窗口具有指定关系的窗口，则返回值为 NULL。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindow(IntPtr hWnd, GwCmd uCmd);

        /// <summary>
        /// 检索有关指定窗口的信息。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getwindowinfo?redirectedfrom=MSDN </para>
        /// </summary>
        /// <param name="hwnd">要获取其信息的窗口的句柄。</param>
        /// <param name="pwi">指向 <see cref="WINDOWINFO"/> 结构的指针以接收信息。请注意，在调用此函数之前，必须将 cbSize 成员设置为 sizeof(<see cref="WINDOWINFO"/>)。</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。
        ///     <para>要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</para>
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

        /// <summary>
        /// 通过将句柄传递给每个窗口，依次传递到应用程序定义的回调函数，可以枚举屏幕上所有的顶级窗口。<see cref="EnumWindows"/> 继续，直到枚举最后一个顶级窗口或回调函数返回 FALSE 为止。
        /// <para>该 <see cref="EnumWindows"/> 的功能不枚举子窗口，与由拥有该系统拥有一些顶层窗口除外 <see cref="WindowStyle.WS_CHILD"/> 风格。</para>
        /// <para>该函数比循环调用 <see cref="GetWindow"/> 函数更可靠。调用 <see cref="GetWindow"/> 来执行此任务的应用程序可能会陷入无限循环或引用已被破坏的窗口的句柄。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-enumwindows </para>
        /// </summary>
        /// <param name="lpEnumFunc">指向应用程序定义的回调函数的指针。有关更多信息，请参见 <see cref="EnumWindowsProc"/>。</param>
        /// <param name="lParam">应用程序定义的值，将传递给回调函数。</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。
        /// <para>如果 EnumWindowsProc 返回零，则返回值也为零。在这种情况下，回调函数应调用 SetLastError 以获得有意义的错误代码，以将其返回给 <see cref="EnumWindows"/> 的调用者。</para></returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        /// <summary>
        /// 枚举与指定桌面关联的所有顶级窗口。它将句柄传递给每个窗口，依次传递给应用程序定义的回调函数。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-enumdesktopwindows?redirectedfrom=MSDN </para>
        /// </summary>
        /// <param name="hDesktop">[HDESK]要枚举其顶级窗口的桌面的句柄。该句柄由 <see cref="CreateDesktop"/>，<see cref="GetThreadDesktop"/>，<see cref="OpenDesktop"/> 或 <see cref="OpenInputDesktop"/> 函数返回，并且必须具有 DESKTOP_READOBJECTS 访问权限。
        ///     <para>如果此参数为NULL，则使用当前桌面。</para>
        /// </param>
        /// <param name="lpfn">指向应用程序定义的 <see cref="EnumWindowsProc"/> 回调函数的指针 。</param>
        /// <param name="lParam">应用程序定义的值，将传递给回调函数。</param>
        /// <returns>如果函数失败或无法执行枚举，则返回值为零。
        ///     <para>要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。如果失败，则必须确保回调函数设置 SetLastError。</para>
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)] 
        public static extern bool EnumDesktopWindows(IntPtr hDesktop,  EnumWindowsProc lpEnumFunc,  IntPtr lParam);

        /// <summary>
        /// 通过将句柄传递给每个子窗口并依次传递给应用程序定义的回调函数，可以枚举属于指定父窗口的子窗口。<see cref="EnumChildWindows"/> 继续，直到枚举最后一个子窗口或回调函数返回 FALSE 为止。
        /// <para>如果子窗口创建了自己的子窗口，则 <see cref="EnumChildWindows"/> 也会枚举这些窗口。</para>
        /// <para>将正确枚举在枚举过程中以Z顺序移动或重新定位的子窗口。该函数不会枚举在枚举之前销毁的子窗口或在枚举过程中创建的子窗口。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-enumchildwindows </para>
        /// </summary>
        /// <param name="hWndParent">父窗口的句柄，其子窗口将被枚举。如果此参数为 NULL，则此函数等效于 <see cref="EnumWindows"/>。</param>
        /// <param name="lpEnumFunc">指向应用程序定义的回调函数的指针。有关更多信息，请参见 <see cref="EnumChildProc"/>。</param>
        /// <param name="lParam">应用程序定义的值，将传递给回调函数。</param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)] 
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);
        #endregion



        #region Window Rect
        /// <summary>
        /// 设置窗口的窗口区域。窗口区域确定系统允许绘图的窗口区域。系统不会显示位于窗口区域之外的窗口的任何部分。
        /// <para>若要获取窗口的窗口区域，请调用 <see cref="GetWindowRgn"/> 函数。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-setwindowrgn </para>
        /// </summary>
        /// <param name="hWnd">要设置其窗口区域的窗口的句柄。</param>
        /// <param name="hRgn">[HRGN] 区域的句柄。该功能将窗口的窗口区域设置为此区域。如果 hRgn 为 NULL，则该函数将窗口区域设置为 NULL。</param>
        /// <param name="bRedraw">指定在设置窗口区域后系统是否重画窗口。如果 bRedraw 为 TRUE，则系统将这样做；否则，事实并非如此。通常，如果窗口可见，则将 bRedraw 设置为TRUE。</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。</returns>
        [DllImport("user32.dll")]
        public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        /// <summary>
        /// 获得一个窗口的窗口区域的副本。通过调用 <see cref="SetWindowRgn"/> 函数来设置窗口的窗口区域。窗口区域确定系统允许绘图的窗口区域。系统不会显示位于窗口区域之外的窗口的任何部分。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getwindowrgn </para>
        /// </summary>
        /// <param name="hWnd">要获取其窗口区域的窗口句柄</param>
        /// <param name="hRgn">[HRGN] 处理将被修改为代表窗口区域的区域</param>
        /// <returns>返回 <see cref="GwrResult"/> 之一的结果。 </returns>
        [DllImport("user32.dll")]
        public static extern int GetWindowRgn(IntPtr hWnd, IntPtr hRgn);

        /// <summary>
        /// 检索指定窗口的边界矩形的尺寸。尺寸以相对于屏幕左上角的屏幕坐标给出。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getwindowrect </para>
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="lpRect">指向一个 <see cref="RECT"/> 结构的指针，该结构接收窗口的左上角和右下角的屏幕坐标</param>
        /// <returns>如果函数成功，返回值为非零：如果函数失败，返回值为零</returns>
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        /// <summary>
        /// 检索窗口的工作区的坐标。客户坐标指定客户区域的左上角和右下角。因为客户坐标是相对于窗口客户区的左上角的，所以左上角的坐标是（0,0）。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getclientrect?redirectedfrom=MSDN </para>
        /// </summary>
        /// <param name="hWnd">要获取其客户坐标的窗口的句柄。</param>
        /// <param name="lpRect">指向接收客户坐标的 RECT 结构的指针。在左和顶级成员是零。的右和底部构件包含该窗口的宽度和高度。</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。要获取扩展的错误信息，请调用GetLastError。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetClientRect(IntPtr hWnd, ref RECT lpRect);
        #endregion



        #region Window Find/Ex
        /// <summary>
        /// 检索顶级窗口的句柄，该窗口的类名和窗口名与指定的字符串匹配。此功能不搜索子窗口。此功能不执行区分大小写的搜索。
        /// <para>要从指定的子窗口开始搜索子窗口，请使用 <see cref="FindWindowEx"/> 函数。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-findwindowa </para>
        /// </summary>
        /// <param name="lpClassName">[LPCSTR]如果 lpClassName 指向一个字符串，则它指定窗口类名称。类名可以是在 <see cref="RegisterClass"/> 或 <see cref="RegisterClassEx"/> 中注册的任何名称，也可以是任何预定义的控件类名称。
        ///     <para>如果 lpClassName 为 NULL，它将找到标题与 lpWindowName 参数匹配的任何窗口</para>
        /// </param>
        /// <param name="lpWindowName">[LPCSTR]窗口名称（窗口标题）。如果此参数为 NULL，则所有窗口名称均匹配。</param>
        /// <returns>如果函数成功，返回值为具有指定类名和窗口名的窗口句柄；如果函数失败，返回值为 NULL。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static IntPtr FindWindow([MarshalAs(UnmanagedType.LPStr)] string lpClassName, [MarshalAs(UnmanagedType.LPStr)] string lpWindowName);
        /// <summary>
        /// 检索顶级窗口的句柄，该窗口的类名和窗口名与指定的字符串匹配。此功能不搜索子窗口。此功能不执行区分大小写的搜索。
        /// <para>要从指定的子窗口开始搜索子窗口，请使用 <see cref="FindWindowEx"/> 函数。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-findwindowa </para>
        /// </summary>
        /// <param name="lpClassName">[LPCSTR]如果 lpClassName 指向一个字符串，则它指定窗口类名称。类名可以是在 <see cref="RegisterClass"/> 或 <see cref="RegisterClassEx"/> 中注册的任何名称，也可以是任何预定义的控件类名称。
        ///     <para>如果 lpClassName 为 NULL，它将找到标题与 lpWindowName 参数匹配的任何窗口</para>
        /// </param>
        /// <param name="lpWindowName">[LPCSTR]窗口名称（窗口标题）。如果此参数为 NULL，则所有窗口名称均匹配。</param>
        /// <returns>如果函数成功，返回值为具有指定类名和窗口名的窗口句柄；如果函数失败，返回值为 NULL。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static IntPtr FindWindow(byte[] lpClassName, [MarshalAs(UnmanagedType.LPStr)]string lpWindowName);

        /// <summary>
        /// 检索其类名和窗口名与指定的字符串匹配的窗口的句柄。该功能搜索子窗口，从指定子窗口之后的子窗口开始。此功能不执行区分大小写的搜索。
        /// <para>如果 lpszWindow 参数不为 NULL，则 <see cref="FindWindowEx"/> 调用 <see cref="GetWindowText"/> 函数以检索窗口名称以进行比较。有关可能出现的潜在问题的描述，请参见 <see cref="GetWindowText"/> 。</para>
        /// <para>应用程序可以通过以下方式调用此函数： FindWindowEx(NULL, NULL, MAKEINTATOM(0x8000), NULL); 请注意，0x8000 是菜单类的原子。当应用程序调用此函数时，该函数检查是否正在显示该应用程序创建的上下文菜单。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-findwindowexa </para>
        /// </summary>
        /// <param name="hWndParent">父窗口要搜索其子窗口的句柄。
        /// <para>如果 hwndParent 为 NULL，则该函数使用桌面窗口作为父窗口。该功能在作为桌面子窗口的窗口之间搜索。如果 hwndParent 为 HWND_MESSAGE，则该函数搜索所有仅消息窗口。</para></param>
        /// <param name="hWndChildAfter">子窗口的句柄。搜索从Z顺序的下一个子窗口开始。子窗口必须是 hwndParent 的直接子窗口，而不仅仅是后代窗口。
        /// <para>如果 hwndChildAfter 为 NULL，则搜索从 hwndParent 的第一个子窗口开始。请注意，如果 hwndParent 和 hwndChildAfter 均为 NULL，则该函数将搜索所有顶级窗口和仅消息窗口。</para></param>
        /// <param name="lpszClass">由先前调用 <see cref="RegisterClass"/> 或 <see cref="RegisterClassEx"/> 函数创建的类名称或类原子。原子必须放在 lpszClass 的低位字中；高阶字必须为零。
        ///     <para>如果 lpszClass 是一个字符串，则它指定窗口类名称。类名可以是在 <see cref="RegisterClass"/> 或 <see cref="RegisterClassEx"/> 中注册的任何名称，也可以是任何预定义的控件类名称，也可以是 MAKEINTATOM(0x8000)。在后一种情况下，0x8000 是菜单类的原子。</para></param>
        /// <param name="lpszWindow">窗口名称（窗口标题）。如果此参数为 NULL，则所有窗口名称均匹配。</param>
        /// <returns>如果函数成功，则返回值是具有指定类和窗口名称的窗口的句柄。如果函数失败，则返回值为 NULL。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, [MarshalAs(UnmanagedType.LPStr)]string lpszClass, [MarshalAs(UnmanagedType.LPStr)]string lpszWindow);
        /// <summary>
        /// 检索其类名和窗口名与指定的字符串匹配的窗口的句柄。该功能搜索子窗口，从指定子窗口之后的子窗口开始。此功能不执行区分大小写的搜索。
        /// <para>如果 lpszWindow 参数不为 NULL，则 <see cref="FindWindowEx"/> 调用 <see cref="GetWindowText"/> 函数以检索窗口名称以进行比较。有关可能出现的潜在问题的描述，请参见 <see cref="GetWindowText"/> 。</para>
        /// <para>应用程序可以通过以下方式调用此函数： FindWindowEx(NULL, NULL, MAKEINTATOM(0x8000), NULL); 请注意，0x8000 是菜单类的原子。当应用程序调用此函数时，该函数检查是否正在显示该应用程序创建的上下文菜单。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-findwindowexa </para>
        /// </summary>
        /// <param name="hWndParent">父窗口要搜索其子窗口的句柄。
        /// <para>如果 hwndParent 为 NULL，则该函数使用桌面窗口作为父窗口。该功能在作为桌面子窗口的窗口之间搜索。如果 hwndParent 为 HWND_MESSAGE，则该函数搜索所有仅消息窗口。</para></param>
        /// <param name="hWndChildAfter">子窗口的句柄。搜索从Z顺序的下一个子窗口开始。子窗口必须是 hwndParent 的直接子窗口，而不仅仅是后代窗口。
        /// <para>如果 hwndChildAfter 为 NULL，则搜索从 hwndParent 的第一个子窗口开始。请注意，如果 hwndParent 和 hwndChildAfter 均为 NULL，则该函数将搜索所有顶级窗口和仅消息窗口。</para></param>
        /// <param name="lpszClass">由先前调用 <see cref="RegisterClass"/> 或 <see cref="RegisterClassEx"/> 函数创建的类名称或类原子。原子必须放在 lpszClass 的低位字中；高阶字必须为零。
        ///     <para>如果 lpszClass 是一个字符串，则它指定窗口类名称。类名可以是在 <see cref="RegisterClass"/> 或 <see cref="RegisterClassEx"/> 中注册的任何名称，也可以是任何预定义的控件类名称，也可以是 MAKEINTATOM(0x8000)。在后一种情况下，0x8000 是菜单类的原子。</para></param>
        /// <param name="lpszWindow">窗口名称（窗口标题）。如果此参数为 NULL，则所有窗口名称均匹配。</param>
        /// <returns>如果函数成功，则返回值是具有指定类和窗口名称的窗口的句柄。如果函数失败，则返回值为 NULL。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, byte[] lpszClass, [MarshalAs(UnmanagedType.LPStr)]string lpszWindow);
        #endregion



        #region Window Activity
        /// <summary>
        /// 将创建指定窗口的线程带入前台并激活该窗口。
        /// <para>键盘输入直接指向窗口，并且为用户更改了各种视觉提示。系统向创建前景窗口的线程分配的优先级比向其他线程分配的优先级高。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-setforegroundwindow </para>
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns>如果将窗口带到前台，则返回值为非零。如果未将窗口带到前台，则返回值为零</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// 检索前景窗口（用户当前正在使用的窗口）的句柄。系统向创建前景窗口的线程分配的优先级比向其他线程分配的优先级高。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getforegroundwindow</para>
        /// </summary>
        /// <returns>返回值是前景窗口的句柄。在某些情况下，例如某个窗口失去激活状态，前景窗口可以为 NULL。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// 检索桌面窗口的句柄。桌面窗口覆盖整个屏幕。桌面窗口是在其上绘制其他窗口的区域。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getdesktopwindow </para>
        /// </summary>
        /// <returns></returns>
        [DllImport("user32.dll")]
        public extern static IntPtr GetDesktopWindow();

        /// <summary>
        /// 将窗口句柄检索到附加到调用线程的消息队列的活动窗口。
        /// <para>要获取前景窗口的句柄，可以使用 <see cref="GetForegroundWindow"/>。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getactivewindow </para>
        /// </summary>
        /// <returns>返回值是附加到调用线程的消息队列的活动窗口的句柄。否则，返回值为 NULL。</returns>
        [DllImport("user32.dll")]
        public extern static IntPtr GetActiveWindow();

        /// <summary>
        /// 激活一个窗口。该窗口必须附加到调用线程的消息队列。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-setactivewindow </para>
        /// </summary>
        /// <param name="hWnd">要激活的顶层窗口的句柄。</param>
        /// <returns>如果函数成功，则返回值是先前处于活动状态的窗口的句柄。如果函数失败，则返回值为 NULL。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public extern static IntPtr SetActiveWindow(IntPtr hWnd);

        /// <summary>
        /// 如果窗口附加到调用线程的消息队列，则检索具有键盘焦点的窗口的句柄。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getfocus </para>
        /// </summary>
        /// <returns>返回值是具有键盘焦点的窗口的句柄。如果调用线程的消息队列没有与键盘焦点相关联的窗口，则返回值为 NULL。</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();

        /// <summary>
        /// 检索已捕获鼠标的窗口的句柄（如果有）。一次只能捕获一个窗口。无论光标是否在其边界内，此窗口都会接收鼠标输入。
        /// <para>一个 NULL 的返回值意味着当前线程未捕获鼠标。但是，很可能另一个线程或进程捕获了鼠标。要获取另一个线程上的捕获窗口的句柄，请使用 <see cref="GetGUIThreadInfo"/> 函数。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getcapture </para>
        /// </summary>
        /// <returns>返回值是与当前线程关联的捕获窗口的句柄。如果线程中没有窗口捕获到鼠标，则返回值为 NULL。</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetCapture();

        /// <summary>
        /// 将鼠标捕获设置为属于当前线程的指定窗口。当鼠标悬停在捕获窗口上方时，或者当鼠标悬停在捕获窗口上方且按钮仍处于按下状态时，按下鼠标按钮时，<see cref="SetCapture"/> 捕获鼠标输入。一次只能捕获一个窗口。
        /// <para>如果鼠标光标位于另一个线程创建的窗口上，则仅当按下鼠标按钮时，系统才会将鼠标输入定向到指定的窗口。</para>
        /// <para>只有前景窗口可以捕获鼠标。当后台窗口尝试这样做时，该窗口仅接收有关光标热点位于窗口可见部分之内时发生的鼠标事件的消息。同样，即使前景窗口捕获了鼠标，用户仍然可以单击另一个窗口，将其置于前景。</para>
        /// <para>当窗口不再需要所有鼠标输入时，创建窗口的线程应调用 <see cref="ReleaseCapture"/> 函数来释放鼠标。此功能不能用于捕获用于其他进程的鼠标输入。捕获鼠标后，菜单热键和其他键盘加速器不起作用。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-setcapture?redirectedfrom=MSDN </para>
        /// </summary>
        /// <param name="hWnd">当前线程中要捕获鼠标的窗口的句柄。</param>
        /// <returns>返回值是先前捕获鼠标的窗口的句柄。如果没有这样的窗口，则返回值为 NULL。</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr SetCapture(IntPtr hWnd);

        /// <summary>
        /// 从当前线程的窗口中释放鼠标捕获，并恢复正常的鼠标输入处理。捕获光标的窗口将接收所有鼠标输入，而与光标的位置无关，除非在光标热点位于另一个线程的窗口中时单击鼠标按钮。
        /// <para>应用程序在调用 <see cref="SetCapture"/> 函数之后调用此函数。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-releasecapture?redirectedfrom=MSDN </para>
        /// </summary>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReleaseCapture();
        #endregion



        #region Window Name Or Class Name        
        /// <summary>
        /// 检索指定窗口所属的类的名称。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getclassname </para>
        /// </summary>
        /// <param name="hWnd">窗口的句柄及间接给出的窗口所属的类</param>
        /// <param name="lpClassName">[LPSTR]类名字符串。
        ///     <para>注意：这里 lpClassName 需要设置容量大小，否则会出现意外的错误；例如：StringBuilder sb = new StringBuilder(255); </para></param>
        /// <param name="nMaxCount">lpClassName 缓冲区的长度，以字符为单位。缓冲区必须足够大以包含终止的空字符。否则，类名字符串将被截断为 nMaxCount-1 字符。</param>
        /// <returns>如果函数成功，则返回值是复制到缓冲区的字符数，不包括终止的空字符。如果函数失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/> 函数。</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        /// <summary>
        /// 检索指定窗口的标题栏文本的长度（以字符为单位）（如果窗口具有标题栏）。如果指定的窗口是控件，则该函数将检索控件内文本的长度。但是 <see cref="GetWindowTextLength"/> 无法在另一个应用程序中检索编辑控件的文本长度。
        /// <para>如果目标窗口由当前进程拥有，则 <see cref="GetWindowTextLength"/> 导致将 <see cref="MessageType.WM_GETTEXTLENGTH"/> 消息发送到指定的窗口或控件。</para>
        /// <para>要获取文本的确切长度，请使用 <see cref="MessageType.WM_GETTEXT"/>，LB_GETTEXT 或 CB_GETLBTEXT 消息或 <see cref="GetWindowText"/> 函数。</para>
        /// <para>在某些情况下，<see cref="GetWindowTextLength"/> 函数可能返回的值大于文本的实际长度。这是由于 ANSI 和 Unicode 的某些混合而发生的，并且是由于系统允许文本中可能存在双字节字符集（DBCS）字符。
        /// 但是返回值将始终至少与文本的实际长度一样大。因此，您始终可以使用它来指导缓冲区分配。当应用程序同时使用 ANSI 函数和使用 Unicode 的通用对话框时，就可能出现此现象。当应用程序的窗口过程为 Unicode 的窗口使用 ANSI 版本的 <see cref="GetWindowTextLength"/> 或 <see cref="GetWindowTextLength"/> 的 Unicode 版本时，也会发生这种情况。窗口过程为ANSI的窗口。有关 ANSI 和 ANSI 函数的更多信息，请参见函数原型约定。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getwindowtextlengtha </para>
        /// </summary>
        /// <param name="hWnd">窗口或控件的句柄。</param>
        /// <returns>如果函数成功，则返回值是文本的长度（以字符为单位）。在某些情况下，该值实际上可能大于文本的长度。
        /// <para>如果窗口没有文本，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</para>
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        /// <summary>
        /// 将指定窗口标题栏的文本（如果有的话）复制到缓冲区中。如果指定的窗口是控件，则复制控件的文本。但是 <see cref="GetWindowText"/> 无法在另一个应用程序中检索控件的文本。
        /// <para>GetWindowTextA(LPSTR), GetWindowTextW(LPWSTR) </para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getwindowtexta </para>
        /// </summary>
        /// <param name="hWnd">包含文本的窗口或控件的句柄</param>
        /// <param name="lpString">[LPSTR]将接收文本的缓冲区。如果字符串与缓冲区一样长或更长，则字符串将被截断并以空字符终止。
        /// <para>注意：这里 lpString 最好是设置容量大小，例如：StringBuilder sb = new StringBuilder(255); </para></param>
        /// <param name="nMaxCount">要复制到缓冲区的最大字符数，包括空字符。如果文本超过此限制，则会被截断。</param>
        /// <returns>如果函数成功，则返回值是所复制字符串的长度（以字符为单位），不包括终止的空字符。
        ///     <para>如果窗口没有标题栏或文本，如果标题栏为空，或者窗口或控件句柄无效，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</para>
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// 更改指定窗口标题栏的文本（如果有的话）。如果指定的窗口是控件，则更改控件的文本。但是，<see cref="SetWindowText"/> 无法在另一个应用程序中更改控件的文本。
        /// <para>要在另一个进程中设置控件的文本，请直接发送 <see cref="MessageType.WM_SETTEXT"/> 消息，而不是调用 <see cref="SetWindowText"/>。 </para>
        /// <para>该函数 <see cref="SetWindowText"/> 函数不展开制表符（ASCII代码0×09）。制表符显示为竖线（|）字符。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-setwindowtexta </para>
        /// </summary>
        /// <param name="hWnd">要更改其文本的窗口或控件的句柄。</param>
        /// <param name="lpString">[LPCSTR] 新标题或控件文本</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetWindowText(IntPtr hWnd, String lpString);
        /// <summary>
        /// 更改指定窗口标题栏的文本（如果有的话）。如果指定的窗口是控件，则更改控件的文本。但是，<see cref="SetWindowText"/> 无法在另一个应用程序中更改控件的文本。
        /// <para>要在另一个进程中设置控件的文本，请直接发送 <see cref="MessageType.WM_SETTEXT"/> 消息，而不是调用 <see cref="SetWindowText"/>。 </para>
        /// <para>该函数 <see cref="SetWindowText"/> 函数不展开制表符（ASCII代码0×09）。制表符显示为竖线（|）字符。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-setwindowtexta </para>
        /// </summary>
        /// <param name="hWnd">要更改其文本的窗口或控件的句柄。</param>
        /// <param name="lpString">[LPCSTR] 新标题或控件文本</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetWindowText(IntPtr hWnd, StringBuilder lpString);
        #endregion



        #region Window Operation Return Boolean Value
        /// <summary>
        /// 启用或禁用向指定窗口或控件的鼠标和键盘输入。禁用输入后，该窗口不会接收到诸如鼠标单击和按键之类的输入。启用输入后，窗口将接收所有输入。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-enablewindow </para>
        /// </summary>
        /// <param name="hWnd">要启用或禁用的窗口句柄。</param>
        /// <param name="bEnable">指示是启用还是禁用窗口。如果此参数为 TRUE，则启用窗口。如果参数为 FALSE，则禁用窗口。</param>
        /// <returns>如果以前禁用了窗口，则返回值为非零。如果该窗口先前未禁用，则返回值为零。</returns>
        [DllImport("user32.dll")]
        public static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        /// <summary>
        /// 确定是否为鼠标和键盘输入启用了指定的窗口。
        /// <para>子窗口仅在启用且可见的情况下才接收输入。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-iswindowenabled </para>
        /// </summary>
        /// <param name="hWnd">要测试的窗口的句柄。</param>
        /// <returns>如果启用了窗口，则返回值为非零。如果未启用该窗口，则返回值为零。</returns>
        [DllImport("user32.dll")]
        public static extern bool IsWindowEnabled(IntPtr hWnd);

        /// <summary>
        /// 确定指定窗口的可见性状态。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-iswindowvisible </para>
        /// </summary>
        /// <param name="hWnd">要测试的窗口的句柄。</param>
        /// <returns>如果指定的窗口，其父窗口，其父级的父窗口等具有 <see cref="WindowStyle.WS_VISIBLE"/> 样式，则返回值为非零。否则，返回值为零。
        /// <para>因为返回值指定窗口是否具有 <see cref="WindowStyle.WS_VISIBLE"/> 样式，所以即使该窗口被其他窗口完全遮盖了，返回值也可能为非零。</para>
        /// </returns>
        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// 确定指定的窗口句柄是否标识现有窗口。
        /// <para>线程不应对未创建的窗口使用 IsWindow，因为调用此函数后该窗口可能会被破坏。此外，由于窗口句柄被回收，因此该句柄甚至可以指向其他窗口。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-iswindow </para>
        /// </summary>
        /// <param name="hWnd">要测试的窗口的句柄。</param>
        /// <returns>如果窗口句柄标识现有窗口，则返回值为非零。如果窗口句柄无法识别现有窗口，则返回值为零。</returns>
        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        /// <summary>
        /// 确定指定的窗口是否为本地 Unicode 窗口。
        /// <para>如果窗口类是使用 ANSI 版本的 <see cref="RegisterClass"/>（RegisterClassA）注册的，则窗口的字符集是 ANSI。如果窗口类是使用 Unicode 版本的 <see cref="RegisterClass"/>（RegisterClassW）注册的，则窗口的字符集为 Unicode。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-iswindowunicode </para>
        /// </summary>
        /// <param name="hWnd">要测试的窗口的句柄。</param>
        /// <returns>如果该窗口是本机 Unicode 窗口，则返回值为非零。如果该窗口不是本机 Unicode 窗口，则返回值为零。该窗口是本机 ANSI 窗口。</returns>
        [DllImport("user32.dll")]
        public static extern bool IsWindowUnicode(IntPtr hWnd);

        /// <summary>
        /// 确定窗口是指定父窗口的子窗口还是子窗口。子窗口是指定父窗口的直接后代（如果该父窗口在父窗口的链中）；父窗口链从原始重叠窗口或弹出窗口通向子窗口。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-ischild </para>
        /// </summary>
        /// <param name="hWndParent">父窗口的句柄。</param>
        /// <param name="hWnd">要测试的窗口的句柄。</param>
        /// <returns>如果该窗口是指定父窗口的子窗口或子窗口，则返回值为非零。如果该窗口不是指定父窗口的子窗口或子窗口，则返回值为零。</returns>
        [DllImport("user32.dll")]
        public static extern bool IsChild(IntPtr hWndParent, IntPtr hWnd);

        /// <summary>
        /// 确定窗口是否最大化。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-iszoomed </para>
        /// </summary>
        /// <param name="hWnd">要测试的窗口的句柄。</param>
        /// <returns>如果缩放窗口，则返回值为非零。如果窗口未缩放，则返回值为零。</returns>
        [DllImport("user32.dll")]
        public static extern bool IsZoomed(IntPtr hWnd);

        /// <summary>
        /// 确定指定的窗口是否最小化（图标）。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-isiconic </para>
        /// </summary>
        /// <param name="hWnd">要测试的窗口的句柄。</param>
        /// <returns>如果窗口是标志性的，则返回值为非零。如果窗口不是标志性的，则返回值为零。</returns>
        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        /// <summary>
        /// 设置窗口的显示状态。
        /// <para>要在显示或隐藏窗口时执行某些特殊效果，请使用 <see cref="AnimateWindow"/>。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-showwindow </para>
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="nCmdShow">[int] 控制窗口的显示方式 <see cref="SwCmd"/></param>
        /// <returns>如果该窗口以前是可见的，则返回值为非零。如果该窗口以前是隐藏的，则返回值为零。</returns>
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, SwCmd nCmdShow);

        /// <summary>
        /// 设置指定窗口的显示状态，而无需等待操作完成。
        /// <para>要在显示或隐藏窗口时执行某些特殊效果，请使用 <see cref="AnimateWindow"/>。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-showwindowasync </para>
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="nCmdShow">[int] 控制窗口的显示方式 <see cref="SwCmd"/></param>
        /// <returns>如果该窗口以前是可见的，则返回值为非零。如果该窗口以前是隐藏的，则返回值为零。</returns>
        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(IntPtr hWnd, SwCmd nCmdShow);

        /// <summary>
        /// 创建一个重叠窗口，弹出窗口或子窗口。它指定窗口类，窗口标题，窗口样式，以及（可选）窗口的初始位置和大小。该函数还指定窗口的父级或所有者（如果有）以及窗口的菜单。
        /// <para>除了 <see cref="CreateWindow"/> 支持的样式之外，要使用扩展的窗口样式，请使用 <see cref="CreateWindowEx"/> 函数。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-createwindowexa </para>
        /// </summary>
        /// <param name="lpClassName"></param>
        /// <param name="lpWindowName"></param>
        /// <param name="dwStyle"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="hwndParent"></param>
        /// <param name="hMenu"></param>
        /// <param name="hInstance"></param>
        /// <param name="lpParam">[LPVOID]</param>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern void CreateWindow(string lpClassName, string lpWindowName, WindowStyle dwStyle, int x, int y, int width, int height,
                                              IntPtr hwndParent, IntPtr hMenu, IntPtr hInstance, [MarshalAs(UnmanagedType.AsAny)] object lpParam);

        /// <summary>
        /// 创建具有扩展窗口样式的重叠窗口，弹出窗口或子窗口；否则，此函数与 <see cref="CreateWindow"/> 函数相同。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-createwindowexa </para>
        /// </summary>
        /// <param name="dwExStyle">正在创建的窗口的扩展窗口样式。有关可能值的列表，请参见 <see cref="WindowStyleEx"/>。</param>
        /// <param name="lpClassName"></param>
        /// <param name="lpWindowName"></param>
        /// <param name="dwStyle"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="hwndParent"></param>
        /// <param name="hMenu"></param>
        /// <param name="hInstance"></param>
        /// <param name="lpParam"></param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateWindowEx(WindowStyleEx dwExStyle, string lpClassName, string lpWindowName, WindowStyle dwStyle, int x, int y, int width, int height,
                                              IntPtr hwndParent, IntPtr hMenu, IntPtr hInstance, [MarshalAs(UnmanagedType.AsAny)] object lpParam);
        /// <summary>
        /// 最小化（但不破坏）指定的窗口。
        /// <para>要销毁窗口，应用程序必须使用 <see cref="DestroyWindow"/> 函数。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-closewindow </para>
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool CloseWindow(IntPtr hWnd);

        /// <summary>
        /// 销毁指定的窗口。
        /// <para>如果指定的窗口是父窗口或所有者窗口，则 <see cref="DestroyWindow"/> 在销毁父窗口或所有者窗口时会自动销毁关联的子窗口或所有者窗口。该函数首先销毁子窗口或所有者窗口，然后销毁父窗口或所有者窗口。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-destroywindow </para>
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyWindow(IntPtr hWnd);

        /// <summary>
        /// 将焦点切换到指定的窗口，并将其置于前景。
        /// <para>通常调用此函数来维护窗口 Z 顺序。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-switchtothiswindow </para>
        /// </summary>
        /// <param name="hWnd">窗口的句柄。</param>
        /// <param name="fUnknown">TRUE 此参数指示窗口正在被切换到使用 Alt/CTL+Tab 键序列。否则，此参数应为 FALSE。</param>
        [DllImport("user32.dll")]
        public static extern void SwitchToThisWindow(IntPtr hWnd, bool fUnknown);
        #endregion


        //[DllImport("user32.dll")]
        //public static extern uint GetWindowThreadProcessId(IntPtr hWnd, ref uint lpdwProcessId);
    }

    /// <summary>
    /// WindowsAPI User32库，扩展常用/通用，功能/函数，扩展示例，以及使用方式
    /// </summary>
    public static partial class User32Extension
    {
        /// <summary>
        /// StringBuffer 实例所分配的内存中的最大字符数，Default 0xFF.
        /// </summary>
        private const int CAPACITY_DEFAULT_SIZE = 0xFF;

        /// <summary>
        /// 设置窗口在 Z 顺序中位于定位的窗口之前的窗口的值
        /// <para>调用 <see cref="User32.SetWindowPos(IntPtr, SwpInsertAfter, int, int, int, int, SwpFlags)"/></para>
        /// <para>WPF Window Handle use <see cref="WindowInteropHelper.Handle"/></para>
        /// </summary>
        /// <param name="window">WPF 窗体对象</param>
        /// <param name="after">see <see cref="SwpInsertAfter"/></param>
        /// <exception cref="ArgumentException"></exception>
        public static void InsertAfter(this Window window, SwpInsertAfter after)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
                throw new ArgumentException("窗口实例化未完成/未呈现，无法获取窗口句柄。");

            User32.SetWindowPos(hwnd, after, 0, 0, 0, 0, SwpFlags.NOMOVE | SwpFlags.NOSIZE);
        }

        /// <summary>
        /// 遍历屏幕上所有的顶层窗口，然后给回调函数传入每个遍历窗口的句柄。
        /// 不过并不是所有遍历的窗口都是顶层窗口，有一些非顶级系统窗口也会遍历到，详见：<see cref="User32.EnumWindows"/> 
        /// </summary>
        /// <returns>返回顶层窗口句柄集合</returns>
        public static IReadOnlyList<IntPtr> EnumWindows()
        {
            List<IntPtr> windows = new List<IntPtr>();
            User32.EnumWindows((hwnd, lParam) =>
            {
                //lParam: process id
                windows.Add(hwnd);
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        /// <summary>
        /// 获取 窗口句柄/窗口标题 字典
        /// </summary>
        /// <returns>返回窗口句柄及对应的标题</returns>
        public static IReadOnlyDictionary<IntPtr, string> FindWindowByTitleName()
        {
            StringBuilder lpString = new StringBuilder(CAPACITY_DEFAULT_SIZE);
            Dictionary<IntPtr, string> windows = new Dictionary<IntPtr, string>();

            User32.EnumWindows((hwnd, IParam) =>
            {
                User32.GetWindowText(hwnd, lpString, lpString.Capacity);
                windows.Add(hwnd, lpString.ToString());

                return true;
            }, IntPtr.Zero);

            return windows;
        }

        /// <summary>
        /// 获取 窗口句柄/窗口标题 字典
        /// </summary>
        /// <param name="titleName">关键名称搜索</param>
        /// <returns>返回窗口句柄及对应的标题</returns>
        public static IReadOnlyDictionary<IntPtr, string> FindWindowByTitleName(string titleName)
        {
            if (string.IsNullOrWhiteSpace(titleName)) return FindWindowByTitleName();

            StringBuilder lpString = new StringBuilder(CAPACITY_DEFAULT_SIZE);
            Dictionary<IntPtr, string> windows = new Dictionary<IntPtr, string>(16);

            User32.EnumWindows((hwnd, IParam) =>
            {
                User32.GetWindowText(hwnd, lpString, lpString.Capacity);

                if (lpString.ToString().IndexOf(titleName, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    windows.Add(hwnd, lpString.ToString());
                }
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        /// <summary>
        /// 获取 窗口句柄/窗口类名 字典
        /// </summary>
        /// <returns>返回窗口句柄及对应的类名</returns>
        public static IReadOnlyDictionary<IntPtr, string> FindWindowByClassName()
        {
            StringBuilder lpString = new StringBuilder(CAPACITY_DEFAULT_SIZE);
            Dictionary<IntPtr, string> windows = new Dictionary<IntPtr, string>();

            User32.EnumWindows((hwnd, IParam) =>
            {
                User32.GetClassName(hwnd, lpString, lpString.Capacity);
                windows.Add(hwnd, lpString.ToString());

                return true;
            }, IntPtr.Zero);

            return windows;
        }

        /// <summary>
        /// 获取 窗口句柄/窗口类名 字典
        /// </summary>
        /// <param name="className"></param>
        /// <returns>返回窗口句柄及对应的类名</returns>
        public static IReadOnlyDictionary<IntPtr, string> FindWindowByClassName(string className)
        {
            StringBuilder lpString = new StringBuilder(CAPACITY_DEFAULT_SIZE);
            Dictionary<IntPtr, string> windows = new Dictionary<IntPtr, string>(16);

            User32.EnumWindows((hwnd, IParam) =>
            {
                User32.GetClassName(hwnd, lpString, lpString.Capacity);
                if (lpString.ToString().IndexOf(className, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    windows.Add(hwnd, lpString.ToString());
                }

                return true;
            }, IntPtr.Zero);

            return windows;
        }
    }
}
