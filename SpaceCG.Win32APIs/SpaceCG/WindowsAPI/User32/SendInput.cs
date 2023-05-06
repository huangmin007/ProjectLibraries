using System;
using System.Runtime.InteropServices;

/***
 * Keyboard and Mouse Input 
 * 参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-sendinput 
 * 
 * 
**/

namespace SpaceCG.WindowsAPI.User32
{
    #region Enumerations
    /// <summary>
    /// <see cref="INPUT.type"/> 字段的值之一
    /// </summary>
    public enum InputType:uint
    {
        /// <summary>
        /// 该事件是鼠标事件。使用联合的 mouse 结构。
        /// </summary>
        MOUSE = 0,
        /// <summary>
        /// 该事件是键盘事件。使用联合的 keyboard 结构。
        /// </summary>
        KEYBOARD = 1,
        /// <summary>
        /// 该事件是硬件事件。使用联合的 hardware 结构。
        /// </summary>
        HARDWARE = 2,
    }

    /// <summary>
    /// <see cref="MOUSEINPUT.dwFlags"/> 字段的值之一或值组合
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-mouseinput </para>
    /// </summary>
    [Flags]
    public enum MouseEventFlags:uint
    {
        /// <summary>
        /// 移动发生
        /// </summary>
        MOUSEEVENTF_MOVE = 0x0001,
        /// <summary>
        /// 按下左按钮。
        /// </summary>
        MOUSEEVENTF_LEFTDOWN = 0x0002,
        /// <summary>
        /// 释放左按钮。
        /// </summary>
        MOUSEEVENTF_LEFTUP = 0x0004,
        /// <summary>
        /// 按下了右按钮。
        /// </summary>
        MOUSEEVENTF_RIGHTDOWN = 0x0008,
        /// <summary>
        /// 释放了右键。
        /// </summary>
        MOUSEEVENTF_RIGHTUP = 0x0010,
        /// <summary>
        /// 按下中间按钮。
        /// </summary>
        MOUSEEVENTF_MIDDLEDOWN = 0x0020,
        /// <summary>
        /// 中间按钮被释放。
        /// </summary>
        MOUSEEVENTF_MIDDLEUP = 0x0040,
        /// <summary>
        /// 按下了X按钮。
        /// </summary>
        MOUSEEVENTF_XDOWN = 0x0080,
        /// <summary>
        /// X按钮被释放。
        /// </summary>
        MOUSEEVENTF_XUP = 0x0100,
        /// <summary>
        /// 如果鼠标带有滚轮，则滚轮已移动。移动量在 mouseData 中指定。
        /// </summary>
        MOUSEEVENTF_WHEEL = 0x0800,
        /// <summary>
        /// 如果鼠标带有滚轮，则将滚轮水平移动。移动量在 mouseData 中指定。
        /// </summary>
        MOUSEEVENTF_HWHEEL = 0x1000,
        /// <summary>
        /// 该 <see cref="MessageType.WM_MOUSEMOVE"/> 消息将不会被合并。默认行为是合并 <see cref="MessageType.WM_MOUSEMOVE"/> 消息。
        /// </summary>
        MOUSEEVENTF_MOVE_NOCOALESCE = 0x2000,
        /// <summary>
        /// 将坐标映射到整个桌面。必须与 <see cref="MOUSEEVENTF_ABSOLUTE"/> 一起使用。
        /// </summary>
        MOUSEEVENTF_VIRTUALDESK = 0x4000,
        /// <summary>
        /// 在 dx 和 dy 成员含有规范化的绝对坐标。如果未设置该标志，则dx和dy包含相对数据（自上次报告位置以来的位置变化）。无论将哪种类型的鼠标或其他定点设备连接到系统，都可以设置或不设置此标志。
        /// </summary>
        MOUSEEVENTF_ABSOLUTE = 0x8000,
    }

    /// <summary>
    /// <see cref="KEYBDINPUT.dwFlags"/> 字段的值之一或值组合
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-keybdinput </para>
    /// </summary>
    [Flags]
    public enum KeyboardEventFlags:uint
    {
        /// <summary>
        /// 如果指定，则在扫描代码之前加上前缀字节，该前缀字节的值为0xE0（224）。
        /// </summary>
        KEYEVENTF_EXTENDEDKEY = 0x0001,
        /// <summary>
        /// 如果指定，则释放键。如果未指定，则按下该键。
        /// </summary>
        KEYEVENTF_KEYUP = 0x0002,
        /// <summary>
        /// 如果指定，系统将合成 <see cref="VirtualKeyCode.PACKET"/> 击键。该WVK参数必须为零。该标志只能与 <see cref="KEYEVENTF_KEYUP"/> 标志结合使用。
        /// </summary>
        KEYEVENTF_UNICODE = 0x0004,
        /// <summary>
        /// 如果指定，则 wScan 会识别键，而 wVk 将被忽略。
        /// </summary>
        KEYEVENTF_SCANCODE = 0x0008,
    }
    #endregion


    #region Structures
    /// <summary>
    /// 通过使用 <see cref="User32.SendInput"/> 来存储信息合成输入事件，如按键、鼠标移动和鼠标点击。(INPUT, *PINPUT, *LPINPUT)
    /// <para> <see cref="INPUT.keyboard"/> 支持非键盘输入法，例如手写识别或语音识别，就好像它是使用 <see cref="KeyboardEventFlags.KEYEVENTF_UNICODE"/> 标志输入的文本一样。有关更多信息，请参见 <see cref="KEYBDINPUT"/> 部分。</para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-input </para>
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct INPUT
    {
        /// <summary>
        /// 输入事件的类型。
        /// </summary>
        [FieldOffset(0)]
        public InputType type;
        /// <summary>
        /// 有关模拟鼠标事件的信息。
        /// </summary>
        [FieldOffset(4)]
        public MOUSEINPUT mouse;
        /// <summary>
        /// 有关模拟键盘事件的信息。
        /// </summary>
        [FieldOffset(4)]
        public KEYBDINPUT keyboard;
        /// <summary>
        /// 有关模拟硬件事件的信息。
        /// </summary>
        [FieldOffset(4)]
        public HARDWAREINPUT hardware;

        /// <summary>
        /// <see cref="INPUT"/> 结构体大小，以字节为单位。
        /// </summary>
        public static readonly int Size = Marshal.SizeOf(typeof(INPUT));

        /// <summary>
        /// @ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (type == InputType.MOUSE)
                return $"[INPUT({type})] {mouse}";
            else if (type == InputType.KEYBOARD)
                return $"[INPUT({type})] {keyboard}";
            else
                return $"[INPUT({type})] {hardware}";
        }
    }

    /// <summary>
    /// 包含有关模拟鼠标事件的信息。(MOUSEINPUT, *PMOUSEINPUT, *LPMOUSEINPUT)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-mouseinput </para>
    /// </summary>
    public struct MOUSEINPUT
    {
        /// <summary>
        /// 鼠标的绝对位置或自上一次鼠标事件发生以来的运动量，取决于 dwFlags 成员的值。绝对数据指定为鼠标的x坐标；相对数据指定为移动的像素数。
        /// </summary>
        public int dx;
        /// <summary>
        /// 鼠标的绝对位置或自上一次鼠标事件发生以来的运动量，取决于 dwFlags 成员的值。绝对数据指定为鼠标的y坐标；相对数据指定为移动的像素数。
        /// </summary>
        public int dy;
        /// <summary>
        /// 如果 dwFlags 包含 <see cref="MouseEventFlags.MOUSEEVENTF_WHEEL"/>，则 mouseData 指定滚轮移动量。正值表示轮子向前旋转，远离用户；负值表示方向盘朝着用户向后旋转。一轮点击定义为 WHEEL_DELTA，即120。
        /// <para>如果 dwFlags 不包含 <see cref="MouseEventFlags.MOUSEEVENTF_WHEEL"/>，<see cref="MouseEventFlags.MOUSEEVENTF_XDOWN"/> 或 <see cref="MouseEventFlags.MOUSEEVENTF_XUP"/>，则 mouseData 应该为 0。</para>
        /// <para>如果 dwFlags 包含 <see cref="MouseEventFlags.MOUSEEVENTF_XDOWN"/> 或 <see cref="MouseEventFlags.MOUSEEVENTF_XUP"/>，则 mouseData 指定按下或释放了哪个 X 按钮。该值可以是以下标志的任意组合。</para>
        /// <para>1.XBUTTON1    0x0001  设置是否按下或释放第一个X按钮。</para>
        /// <para>2.XBUTTON2    0x0002  设置是否按下或释放第二个X按钮。</para>
        /// </summary>
        public uint mouseData;
        /// <summary>
        /// 一组位标记，用于指定鼠标移动和按钮单击的各个方面。该成员中的位可以是以下值的任何合理组合。
        /// <para>设置指定鼠标按钮状态的位标志以指示状态的变化，而不是持续的状态。例如，如果按下并按住鼠标左键，则在第一次按下左键时会设置 <see cref="MouseEventFlags.MOUSEEVENTF_LEFTDOWN"/>，但随后的动作不会设置。同样，仅在首次释放按钮时设置 <see cref="MouseEventFlags.MOUSEEVENTF_LEFTUP"/>。</para>
        /// <para>您不能在 dwFlags 参数中同时指定 <see cref="MouseEventFlags.MOUSEEVENTF_WHEEL"/> 标志和 <see cref="MouseEventFlags.MOUSEEVENTF_XDOWN"/> 或 <see cref="MouseEventFlags.MOUSEEVENTF_XUP"/> 标志，因为它们都需要使用 mouseData 字段。</para>
        /// </summary>
        public MouseEventFlags dwFlags;
        /// <summary>
        /// 事件的时间戳，以毫秒为单位。如果此参数为 0，则系统将提供其自己的时间戳。
        /// </summary>
        public uint time;
        /// <summary>
        /// 与鼠标事件关联的附加值。应用程序调用 <see cref="User32.GetMessageExtraInfo"/> 以获得此额外信息。
        /// </summary>
        public UIntPtr dwExtraInfo;
        /// <summary>
        /// 设置鼠标 x, y 位置
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public void SetPosition(int dx, int dy)
        {
            this.dx = dx;
            this.dy = dy;
        }
        
        public override string ToString()
        {
            return $"[MOUSEINPUT] dx:{dx}, dy:{dy}, mouseData:{mouseData}, dwFlags:{dwFlags}";
        }
    }

    /// <summary>
    /// 包含有关模拟键盘事件的信息。(KEYBDINPUT, *PKEYBDINPUT, *LPKEYBDINPUT)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-keybdinput </para>
    /// </summary>
    public struct KEYBDINPUT
    {
        /// <summary>
        /// 一个虚拟键码( <see cref="VirtualKeyCode"/> )。该代码必须是1到254之间的值。如果 dwFlags 成员指定 <see cref="KeyboardEventFlags.KEYEVENTF_UNICODE"/>，则wVk必须为0。
        /// </summary>
        public VirtualKeyCode wVk;
        /// <summary>
        /// 硬件 Key 扫描代码。如果 dwFlags 指定 <see cref="KeyboardEventFlags.KEYEVENTF_UNICODE"/>，则 wScan 指定要发送到前台应用程序的 Unicode 字符。
        /// </summary>
        public ushort wScan;
        /// <summary>
        /// 指定按键的标志组合
        /// </summary>
        public KeyboardEventFlags dwFlags;
        /// <summary>
        /// 事件的时间戳，以毫秒为单位。如果此参数为零，则系统将提供其自己的时间戳。
        /// </summary>
        public uint time;
        /// <summary>
        /// 与击键关联的附加值。使用 <see cref="User32.GetMessageExtraInfo"/> 函数可获得此信息。
        /// </summary>
        public UIntPtr dwExtraInfo;
        /// <summary>
        /// @ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[KEYBDINPUT] wVk:{wVk}, wScan:{wScan}, dwFlags:{dwFlags}, time:{time}";
        }
    }

    /// <summary>
    /// 包含有关由除键盘或鼠标之外的输入设备生成的模拟消息的信息。(HARDWAREINPUT, * PHARDWAREINPUT, * LPHARDWAREINPUT)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-hardwareinput </para>
    /// </summary>
    public struct HARDWAREINPUT
    {
        /// <summary>
        /// 输入硬件生成的消息。
        /// </summary>
        public uint uMsg;
        /// <summary>
        /// uMsg 的 lParam 参数的低位字(WORD == ushort)。
        /// </summary>
        public ushort wParamL;
        /// <summary>
        /// uMsg 的 lParam 参数的高位字(WORD == ushort)。
        /// </summary>
        public ushort wParamH;

        public override string ToString()
        {
            return $"[HARDWAREINPUT] uMsg:{uMsg}, wParamL:{wParamL}, wParamH:{wParamH}";
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
        /// 合成击键，鼠标动作和按钮单击。
        /// <para>第三方库：https://github.com/michaelnoonan/inputsimulator </para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-sendinput </para>
        /// </summary>
        /// <param name="cInputs">pInputs 数组中的结构数，应为 pInputs 数组长度。</param>
        /// <param name="pInputs">[LPINPUT] <seealso cref="INPUT"/> 结构的数组。每个结构代表一个要插入键盘或鼠标输入流的事件。</param>
        /// <param name="cbSize"><see cref="INPUT"/> 结构的大小（以字节为单位）。如果 cbSize 不是 <see cref="INPUT"/> 结构的大小，则该函数失败。 大小应为 Marshal.SizeOf(typeof(INPUT))</param>
        /// <returns>该函数返回成功插入键盘或鼠标输入流中的事件数。如果函数返回零，则输入已经被另一个线程阻塞。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。
        /// <para>当 UIPI 阻止此功能时，该功能将失败。请注意，<see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/> 和返回值都不会指示失败是由 UIPI 阻塞引起的。</para>
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint cInputs, INPUT[] pInputs, int cbSize);
    }

    /// <summary>
    /// WindowsAPI User32库，扩展常用/通用，功能/函数，扩展示例，以及使用方式ad
    /// </summary>
    public static partial class User32Extension
    {
    }

}
