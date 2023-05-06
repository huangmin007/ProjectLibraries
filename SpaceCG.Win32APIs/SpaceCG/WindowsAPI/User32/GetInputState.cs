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
    /// <see cref="MapVirtualKey"/> 函数参数 uMapType 的值之一
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-mapvirtualkeya </para>
    /// </summary>
    public enum MapVKType
    {
        /// <summary>
        /// uCode 是虚拟密钥代码，并转换为扫描代码。如果它是不能区分左手键和右手键的虚拟键代码，则返回左手扫描代码。如果没有转换，则该函数返回0。
        /// </summary>
        VK_TO_VSC = 0,
        /// <summary>
        /// uCode 是一种扫描代码，并转换为虚拟键代码，该虚拟键代码无法区分左手键和右手键。如果没有转换，则该函数返回0。
        /// </summary>
        VSC_TO_VK = 1,
        /// <summary>
        /// uCode 是虚拟键码，并在返回值的低位字中转换为未移位的字符值。死键（变音符号）通过设置返回值的最高位来指示。如果没有转换，则该函数返回0。
        /// </summary>
        VK_TO_CHAR = 2,
        /// <summary>
        /// uCode 是一种扫描代码，并被翻译成可区分左手键和右手键的虚拟键码。如果没有转换，则该函数返回0。
        /// </summary>
        VSC_TO_VK_EX = 3,
    }

    /// <summary>
    /// Key State Masks for Mouse Messages (wParam)
    /// <para>MessageType WM_MouseXXX wParam value type</para>
    /// </summary>
    [Flags]
    public enum MouseKey
    {
        /// <summary>
        /// </summary>
        MK_LBUTTON = 0x0001,
        /// <summary>
        /// </summary>
        MK_RBUTTON = 0x0002,
        /// <summary>
        /// </summary>
        MK_SHIFT = 0x0004,
        /// <summary>
        /// </summary>
        MK_CONTROL = 0x0008,
        /// <summary>
        /// </summary>
        MK_MBUTTON = 0x0010,
        /// <summary>
        /// </summary>
        MK_XBUTTON1 = 0x0020,
        /// <summary>
        /// </summary>
        MK_XBUTTON2 = 0x0040,
    }

    #endregion

    #region Structures
    /// <summary>
    /// 包含全局光标信息。注意 cbSize 大小需要设置。(CURSORINFO, *PCURSORINFO, *LPCURSORINFO)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-cursorinfo </para>
    /// </summary>
    public struct CURSORINFO
    {
        /// <summary>
        /// 结构的大小，以字节为单位。
        /// <para>等于 Marshal.SizeOf(typeof(CURSORINFO)); </para>
        /// </summary>
        public uint cbSize;
        /// <summary>
        /// 光标状态。
        /// <para>0  光标被隐藏。</para>
        /// <para>CURSOR_SHOWING    0x00000001  光标正在显示。</para>
        /// <para>CURSOR_SUPPRESSED 0x00000002  该标志指示系统未在绘制光标，因为用户是通过触摸或笔而不是鼠标来提供输入的。</para>
        /// </summary>
        public uint flags;
        /// <summary>
        /// 光标的句柄。
        /// </summary>
        public IntPtr hCursor;
        /// <summary>
        /// 接收光标的屏幕坐标的结构。
        /// </summary>
        public POINT ptScreenPos;
        /// <summary>
        /// CURSORINFO 结构体
        /// </summary>
        /// <param name="cursor"></param>
        public CURSORINFO(IntPtr cursor)
        {
            flags = 0x00000001;
            hCursor = cursor;
            ptScreenPos = new POINT();
            cbSize = (uint)Marshal.SizeOf(typeof(CURSORINFO));
        }

        /// <summary>
        /// <see cref="CURSORINFO"/> 结构体大小，以字节为单位
        /// </summary>
        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(CURSORINFO));

        /// <summary>
        /// @ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[{nameof(CURSORINFO)}] cbSize:{cbSize}, flags:{flags}, hCursor:{hCursor}, ptScreenPos:{ptScreenPos}";
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
        #region Window Mouse/Cursor State
        /// <summary>
        /// 检索当前光标的句柄。
        /// <para>要获取有关全局游标的信息，即使它不是当前线程所有，也可以使用 <see cref="GetCursorInfo"/> </para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getcursor </para>
        /// </summary>
        /// <returns>返回值 (HCURSOR) 是当前游标的句柄。如果没有游标，则返回值为 NULL。</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetCursor();

        /// <summary>
        /// 检索有关全局游标的信息。
        /// <para>CURSORINFO pci = new CURSORINFO(){ cbSize = Marshal.SizeOf(typeof(CURSORINFO)) };</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getcursorinfo </para>
        /// </summary>
        /// <param name="pci">[PCURSORINFO] 指向接收信息的 <see cref="CURSORINFO"/> 结构的指针。请注意，在调用此函数之前，必须将 cbSize 成员设置为 sizeof(CURSORINFO)。</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorInfo(ref CURSORINFO pci);

        /// <summary>
        /// 检索鼠标光标在屏幕坐标中的位置。
        /// <para>光标位置始终在屏幕坐标中指定，并且不受包含光标的窗口的映射模式的影响。调用过程必须对窗口站具有 WINSTA_READATTRIBUTES 访问权限。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getcursorpos </para>
        /// </summary>
        /// <param name="lpPoint">[LPPOINT]指向 <see cref="POINT"/> 结构的指针，该结构接收光标的屏幕坐标。</param>
        /// <returns>如果成功返回非零，否则返回零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(ref POINT lpPoint);
        #endregion


        #region Window Keyboard State
        /// <summary>
        /// 将虚拟键的状态复制到指定的缓冲区。
        /// <para>要检索单个键的状态信息，请使用 <see cref="GetKeyState(int)"/> 函数。若要检索单个键的当前状态，而不管是否已从消息队列中检索到相应的键盘消息，请使用 <see cref="GetAsyncKeyState(int)"/> 函数。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getkeyboardstate </para>
        /// </summary>
        /// <param name="lpKeyState">[PBYTE]接收每个虚拟密钥的状态数据的256字节数组。
        /// <para>函数返回时，lpKeyState 参数指向的数组的每个成员都 包含虚拟键的状态数据。</para>
        /// <para>如果高位为1，则按键按下；否则为0。否则，它会上升。如果键是切换键（例如CAPS LOCK），则切换键时低位为1；如果取消切换，则低位为0。对于非拨动键，低位无意义。</para>
        /// <para>切换键在打开时被称为切换键。切换键时，键盘上的切换键指示灯（如果有）将亮起；如果不切换键，则指示灯将熄灭。</para>
        /// </param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        /// <summary>
        /// 检索指定虚拟键的状态。状态指定按键是向上，向下还是切换（打开，关闭-每次按下按键时交替显示）。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getkeystate </para>
        /// </summary>
        /// <param name="nVirtKey">虚拟键 <see cref="VirtualKeyCode"/>。如果所需的虚拟键是字母或数字（A到Z，a到z或0到9）， 则必须将 nVirtKey 设置为该字符的 ASCII 值。对于其他密钥，它必须是虚拟密钥代码。</param>
        /// <returns>返回值指定指定虚拟键的状态，如下所示：
        /// <para>如果高位为1，则按键按下；否则为0。否则，它会上升。</para>
        /// <para>如果低位为1，则切换键。如果打开了一个键（例如 CAPS LOCK 键），则会对其进行切换。如果低位为0，则此键处于关闭状态且不切换。切换键时，键盘上的切换键指示灯（如果有）将亮起，而当取消切换键时，其指示灯将熄灭。</para>
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern short GetKeyState(int nVirtKey);

        /// <summary>
        /// 检索指定虚拟键的状态。状态指定按键是向上，向下还是切换（打开，关闭-每次按下按键时交替显示）。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getkeystate </para>
        /// </summary>
        /// <param name="nVirtKey">虚拟键 <see cref="VirtualKeyCode"/>。如果所需的虚拟键是字母或数字（A到Z，a到z或0到9）， 则必须将 nVirtKey 设置为该字符的 ASCII 值。对于其他密钥，它必须是虚拟密钥代码。</param>
        /// <returns>返回值指定指定虚拟键的状态，如下所示：
        /// <para>如果高位为1，则按键按下；否则为0。否则，它会上升。</para>
        /// <para>如果低位为1，则切换键。如果打开了一个键（例如 CAPS LOCK 键），则会对其进行切换。如果低位为0，则此键处于关闭状态且不切换。切换键时，键盘上的切换键指示灯（如果有）将亮起，而当取消切换键时，其指示灯将熄灭。</para>
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern short GetKeyState(VirtualKeyCode nVirtKey);

        /// <summary>
        /// 确定在调用函数时按键是向上还是向下，以及在先前调用 <see cref="GetAsyncKeyState(int)"/> 之后是否按下了该键。
        /// <para>该 <see cref="GetAsyncKeyState(int)"/> 功能可与鼠标按钮。但是，它检查物理鼠标按钮的状态，而不是检查物理按钮映射到的逻辑鼠标按钮的状态。</para>
        /// <para>例如，调用 <see cref="GetAsyncKeyState(int)"/>(VK_LBUTTON) 始终返回物理鼠标左键的状态，而不管它是映射到逻辑鼠标左键还是逻辑右键。您可以通过调用确定系统当前的物理鼠标按钮到逻辑鼠标按钮的映射 <see cref="GetSystemMetrics"/>(SM_SWAPBUTTON)。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getasynckeystate </para>
        /// </summary>
        /// <param name="vKey">虚拟密钥代码 <see cref="VirtualKeyCode"/>，您可以使用左和右区分常数来指定某些键</param>
        /// <returns>如果函数成功，则返回值指定自上次调用 <see cref="GetAsyncKeyState(int)"/> 以来是否按下了该键，以及该键当前处于向上还是向下。
        /// <para>如果设置了最高有效位，则该键处于按下状态；如果设置了最低有效位，则在上一次调用 <see cref="GetAsyncKeyState(int)"/> 之后按下了该键。但是，您不应该依赖于此最后的行为。</para>
        /// <para>在以下情况下，返回值为零：</para>
        /// <para>1.当前桌面不是活动桌面</para>
        /// <para>2.前台线程属于另一个进程，并且桌面不允许挂钩或日志记录。</para>
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern short GetAsyncKeyState(int vKey);

        /// <summary>
        /// 确定在调用函数时按键是向上还是向下，以及在先前调用 <see cref="GetAsyncKeyState(int)"/> 之后是否按下了该键。
        /// <para>该 <see cref="GetAsyncKeyState(int)"/> 功能可与鼠标按钮。但是，它检查物理鼠标按钮的状态，而不是检查物理按钮映射到的逻辑鼠标按钮的状态。</para>
        /// <para>例如，调用 <see cref="GetAsyncKeyState(int)"/>(VK_LBUTTON) 始终返回物理鼠标左键的状态，而不管它是映射到逻辑鼠标左键还是逻辑右键。您可以通过调用确定系统当前的物理鼠标按钮到逻辑鼠标按钮的映射 <see cref="GetSystemMetrics"/>(SM_SWAPBUTTON)。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getasynckeystate </para>
        /// </summary>
        /// <param name="vKey">虚拟密钥代码 <see cref="VirtualKeyCode"/>，您可以使用左和右区分常数来指定某些键</param>
        /// <returns>如果函数成功，则返回值指定自上次调用 <see cref="GetAsyncKeyState(int)"/> 以来是否按下了该键，以及该键当前处于向上还是向下。
        /// <para>如果设置了最高有效位，则该键处于按下状态；如果设置了最低有效位，则在上一次调用 <see cref="GetAsyncKeyState(int)"/> 之后按下了该键。但是，您不应该依赖于此最后的行为。</para>
        /// <para>在以下情况下，返回值为零：</para>
        /// <para>1.当前桌面不是活动桌面</para>
        /// <para>2.前台线程属于另一个进程，并且桌面不允许挂钩或日志记录。</para>
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern short GetAsyncKeyState(VirtualKeyCode vKey);

        /// <summary>
        /// 将指定的虚拟键代码和键盘状态转换为相应的一个或多个字符。该功能使用输入语言和键盘布局手柄识别的物理键盘布局来翻译代码。
        /// <para>提供给 ToAscii 函数的参数可能不足以转换虚拟键代码，因为先前的死键存储在键盘布局中。</para>
        /// <para>通常，ToAscii 基于虚拟键代码执行转换。但是，在某些情况下，uScanCode 参数的第 15 位 可用于区分按键和释放按键。扫描代码用于翻译 ALT + 数字键组合。</para>
        /// <para>尽管 NUM LOCK 是会影响键盘行为的切换键，但是 ToAscii 会忽略 lpKeyState（VK_NUMLOCK）的切换设置（低位）， 因为 仅 uVirtKey 参数足以将光标移动键（VK_HOME，VK_INSERT等）与数字键（VK_DECIMAL，VK_NUMPAD0 - VK_NUMPAD9）。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-toascii </para>
        /// </summary>
        /// <param name="uVirtKey">要转换的虚拟密钥代码 <see cref="VirtualKeyCode"/></param>
        /// <param name="uScanCode">要转换的密钥的硬件扫描代码。如果按下键（未按下），则此值的高位被设置。</param>
        /// <param name="lpbKeyState">[const BYTE *] 指向包含当前键盘状态的 256 字节数组的指针。数组中的每个元素（字节）都包含一个键的状态。如果设置了字节的高位，则按键被按下（按下）。低位（如果已设置）指示按键已打开。在此功能中，仅 CAPS LOCK 键的切换位相关。NUM LOCK 和 SCROLL LOCK 键的切换状态将被忽略。</param>
        /// <param name="lpChar">接收翻译后的一个或多个字符的缓冲区。</param>
        /// <param name="uFlags">如果菜单处于活动状态，则此参数必须为1，否则为0。</param>
        /// <returns>如果指定的键是死键，则返回值为负。否则，它是以下值之一。
        /// <para>0.指定的虚拟键没有针对键盘当前状态的转换。</para>
        /// <para>1.一个字符被复制到缓冲区。</para>
        /// <para>2.两个字符被复制到缓冲区。当无法将存储在键盘布局中的死键字符（重音符或变音符）与指定的虚拟键组成单个字符时，通常会发生这种情况。</para>
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int ToAscii(VirtualKeyCode uVirtKey, uint uScanCode, byte[] lpbKeyState, byte[] lpChar, uint uFlags);

        /// <summary>
        /// 将虚拟键代码转换（映射）为扫描代码或字符值，或将扫描代码转换为虚拟键代码。
        /// <para>要指定用于翻译指定代码的键盘布局的句柄，请使用 <see cref="MapVirtualKeyEx"/> 函数。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-mapvirtualkeya </para>
        /// </summary>
        /// <param name="uCode"><see cref="VirtualKeyCode"/> 或扫描代码。如何解释此值取决于 uMapType 参数的值。</param>
        /// <param name="uMapType">参数的值取决于 uCode 参数的值 <see cref="MapVKType"/></param>
        /// <returns>返回值可以是扫描代码，虚拟键代码或字符值，具体取决于 uCode 和 uMapType 的值。如果没有转换，则返回值为零。</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern uint MapVirtualKey(uint uCode, MapVKType uMapType);
        #endregion

        /// <summary>
        /// 确定在调用线程的消息队列中是否有鼠标按钮或键盘消息。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-getinputstate </para>
        /// </summary>
        /// <returns>如果队列包含一个或多个新的鼠标按钮或键盘消息，则返回值为非零。如果队列中没有新的鼠标按钮或键盘消息，则返回值为零。</returns>
        [DllImport("user32.dll")]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetInputState();
    }

    /// <summary>
    /// WindowsAPI User32库，扩展常用/通用，功能/函数，扩展示例，以及使用方式ad
    /// </summary>
    public static partial class User32Extension
    {
    }

}
