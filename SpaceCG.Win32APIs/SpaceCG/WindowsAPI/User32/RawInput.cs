using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/***
 * 
 * 原始输入技术主题
 * 系统如何向应用程序提供原始输入以及应用程序如何接收和处理该输入。原始输入有时称为通用输入。
 * 参考：https://docs.microsoft.com/zh-cn/windows/win32/inputdev/raw-input
 * 
**/

namespace SpaceCG.WindowsAPI.User32
{

    #region Enumerations
    /// <summary>
    /// <see cref="RAWINPUTDEVICE.dwFlags"/> 的值它可以是零（默认值）或以下值之一。默认情况下，只要具有窗口焦点，操作系统就会将原始输入从具有指定顶级集合（TLC）的设备发送到已注册的应用程序。
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-rawinputdevice </para>
    /// </summary>
    [Flags]
    public enum RawInputFlags
    {
        /// <summary>
        /// Default
        /// </summary>
        RIDEV_DEFAULT = 0x00000000,
        /// <summary>
        /// 如果设置，则从包含列表中删除顶级集合。这告诉操作系统停止从与顶级集合匹配的设备读取数据。
        /// </summary>
        RIDEV_REMOVE = 0x00000001,
        /// <summary>
        /// 如果设置，则指定在阅读完整的使用情况页面时要排除的顶级集合。该标志仅影响已经使用 RIDEV_PAGEONLY 指定使用页面的 TLC 。
        /// </summary>
        RIDEV_EXCLUDE = 0x00000010,
        /// <summary>
        /// 如果设置，则指定其顶级集合来自指定的 usUsagePage 的所有设备。请注意，usUsage 必须为零。要排除特定的顶级集合，请使用 RIDEV_EXCLUDE。
        /// </summary>
        RIDEV_PAGEONLY = 0x00000020,
        /// <summary>
        /// 如果设置，将阻止 usUsagePage 或 usUsage 指定的任何设备生成旧消息。这仅适用于鼠标和键盘。请参阅备注。
        /// </summary>
        RIDEV_NOLEGACY = 0x00000030,
        /// <summary>
        /// 如果设置，则即使呼叫者不在前台，也可以使呼叫者接收输入。请注意，必须指定 hwndTarget。
        /// </summary>
        RIDEV_INPUTSINK = 0x00000100,
        /// <summary>
        /// 如果设置，则单击鼠标按钮不会激活其他窗口。
        /// </summary>
        RIDEV_CAPTUREMOUSE = 0x00000200,
        /// <summary>
        /// 如果设置，则不处理应用程序定义的键盘设备热键。但是，系统热键；例如，ALT+TAB 和 CTRL+ALT+DEL 仍然可以处理。默认情况下，将处理所有键盘热键。RIDEV_NOHOTKEYS 的话可以连指定 RIDEV_NOLEGACY 没有指定和 hwndTarget 为 NULL。
        /// </summary>
        RIDEV_NOHOTKEYS = 0x00000200,
        /// <summary>
        /// 如果设置，将处理应用程序命令键。仅当为键盘设备指定了 RIDEV_NOLEGACY 时，才能指定 RIDEV_APPKEYS。
        /// </summary>
        RIDEV_APPKEYS = 0x00000400,
        /// <summary>
        /// 如果设置，则仅当前台应用程序不对其进行处理时，调用方才能在后台接收输入。换句话说，如果没有为原始输入注册前台应用程序，则注册的后台应用程序将接收输入。
        /// </summary>
        RIDEV_EXINPUTSINK = 0x00001000,
        /// <summary>
        /// 如果设置，则使调用者可以接收 <see cref="MessageType.WM_INPUT_DEVICE_CHANGE"/> 通知，以通知设备到达和设备删除。
        /// </summary>
        RIDEV_DEVNOTIFY = 0x00002000,
    }

    /// <summary>
    /// 原始输入的设备类型。是 <see cref="RAWINPUTHEADER.dwType"/>, <see cref="RAWINPUTDEVICELIST.dwType"/> 值，可以是以下值之一。
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-rawinputheader </para>
    /// </summary>
    public enum RawInputType : uint
    {
        /// <summary>
        /// 原始输入来自鼠标。
        /// </summary>
        RIM_TYPEMOUSE = 0,
        /// <summary>
        /// 原始输入来自键盘。
        /// </summary>
        RIM_TYPEKEYBOARD = 1,
        /// <summary>
        /// 原始输入来自不是键盘或鼠标的某些设备。
        /// </summary>
        RIM_TYPEHID = 2,
    }

    /// <summary>
    /// <see cref="RAWMOUSE.usFlags"/> 的值或值组合
    /// </summary>
    [Flags]
    public enum RawMouseFlags : ushort
    {
        /// <summary>
        /// 鼠标移动数据是相对于最后鼠标位置的。
        /// </summary>
        MOUSE_MOVE_RELATIVE = 0x00,
        /// <summary>
        /// 鼠标移动数据基于绝对位置。
        /// </summary>
        MOUSE_MOVE_ABSOLUTE = 0x01,
        /// <summary>
        /// 鼠标坐标映射到虚拟桌面（对于多监视器系统）。
        /// </summary>
        MOUSE_VIRTUAL_DESKTOP = 0x02,
        /// <summary>
        /// 鼠标属性已更改；应用程序需要查询鼠标属性。
        /// </summary>
        MOUSE_ATTRIBUTES_CHANGED = 0x04,
    }

    /// <summary>
    /// <see cref="RAWMOUSE.usButtonFlags"/> 成员的一个或多个值
    /// </summary>
    [Flags]
    public enum RawInputMouseFlags : ushort
    {
        /// <summary>
        /// 左按钮更改为向下。
        /// </summary>
        RI_MOUSE_LEFT_BUTTON_DOWN = 0x0001,
        /// <summary>
        /// 左按钮变为上按钮。
        /// </summary>
        RI_MOUSE_LEFT_BUTTON_UP = 0x0002,
        /// <summary>
        /// 中键更改为向下。
        /// </summary>
        RI_MOUSE_MIDDLE_BUTTON_DOWN = 0x0010,
        /// <summary>
        /// 中键更改为向上。
        /// </summary>
        RI_MOUSE_MIDDLE_BUTTON_UP = 0x0020,
        /// <summary>
        /// 右键更改为向下。
        /// </summary>
        RI_MOUSE_RIGHT_BUTTON_DOWN = 0x0004,
        /// <summary>
        /// 右键更改为向上。
        /// </summary>
        RI_MOUSE_RIGHT_BUTTON_UP = 0x0008,
        /// <summary>
        /// RI_MOUSE_LEFT_BUTTON_DOWN
        /// </summary>
        RI_MOUSE_BUTTON_1_DOWN = 0x0001,
        /// <summary>
        /// RI_MOUSE_LEFT_BUTTON_UP
        /// </summary>
        RI_MOUSE_BUTTON_1_UP = 0x0002,
        /// <summary>
        /// RI_MOUSE_RIGHT_BUTTON_DOWN
        /// </summary>
        RI_MOUSE_BUTTON_2_DOWN = 0x0004,
        /// <summary>
        /// RI_MOUSE_RIGHT_BUTTON_UP
        /// </summary>
        RI_MOUSE_BUTTON_2_UP = 0x0008,
        /// <summary>
        /// RI_MOUSE_MIDDLE_BUTTON_DOWN
        /// </summary>
        RI_MOUSE_BUTTON_3_DOWN = 0x0010,
        /// <summary>
        /// RI_MOUSE_MIDDLE_BUTTON_UP
        /// </summary>
        RI_MOUSE_BUTTON_3_UP = 0x0020,
        /// <summary>
        /// XBUTTON1更改为向下。
        /// </summary>
        RI_MOUSE_BUTTON_4_DOWN = 0x0040,
        /// <summary>
        /// XBUTTON1更改为up。
        /// </summary>
        RI_MOUSE_BUTTON_4_UP = 0x0080,
        /// <summary>
        /// XBUTTON2更改为向下。
        /// </summary>
        RI_MOUSE_BUTTON_5_DOWN = 0x100,
        /// <summary>
        /// XBUTTON2更改为up。
        /// </summary>
        RI_MOUSE_BUTTON_5_UP = 0x0200,
        /// <summary>
        /// 原始输入来自鼠标滚轮。车轮增量存储在 usButtonData 中。
        /// </summary>
        RI_MOUSE_WHEEL = 0x0400,
    }

    /// <summary>
    /// <see cref="RAWKEYBOARD.Flags"/> 的值之一
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-rawkeyboard </para>
    /// </summary>
    public enum RawInputKeyFlags : ushort
    {
        /// <summary>
        /// The key is down.
        /// </summary>
        RI_KEY_MAKE = 0,
        /// <summary>
        /// The key is up.
        /// </summary>
        RI_KEY_BREAK = 1,
        /// <summary>
        /// 扫描代码具有 E0 前缀。
        /// </summary>
        RI_KEY_E0 = 2,
        /// <summary>
        /// 扫描代码具有 E1 前缀。
        /// </summary>
        RI_KEY_E1 = 4,

        RI_KEY_TERMSRV_SET_LED = 8,

        RI_KEY_TERMSRV_SHADOW = 0x10,
    }

    /// <summary>
    /// <see cref="User32.GetRawInputData(IntPtr, RIDFlag, IntPtr, ref uint, uint)"/> 函数参数 uiCommand 值之一
    /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getrawinputdata </para>
    /// </summary>
    public enum RIDFlag : uint
    {
        /// <summary>
        /// 从 <see cref="RAWINPUT"/> 结构获取原始数据。
        /// </summary>
        RID_INPUT = 0x10000003,

        /// <summary>
        /// 从 <see cref="RAWINPUT"/> 结构获取标头信息。
        /// </summary>
        RID_HEADER = 0x10000005,
    }

    /// <summary>
    /// <see cref="User32.GetRawInputDeviceInfo(IntPtr, uint, IntPtr, ref uint)"/> 函数参数 uiCommand 值之一
    /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getrawinputdeviceinfoa </para>
    /// </summary>
    public enum RIDIFlag : uint
    {
        /// <summary>
        /// pData 指向包含设备名称的字符串。仅对于此 uiCommand，pcbSize 中的值是字符计数（而不是字节计数）。
        /// </summary>
        RIDI_DEVICENAME = 0x20000007,
        /// <summary>
        /// pData 指向 <see cref="RID_DEVICE_INFO"/> 结构。
        /// </summary>
        RIDI_DEVICEINFO = 0x2000000B,
        /// <summary>
        /// pData 指向先前解析的数据。
        /// </summary>
        RIDI_PREPARSEDDATA = 0x20000005,
    }
    #endregion


    #region Structures
    /// <summary>
    /// 定义原始输入设备的信息。(RAWINPUTDEVICE, *PRAWINPUTDEVICE, *LPRAWINPUTDEVICE)
    /// <para>如果为鼠标或键盘设置了 <see cref="RawInputFlags.RIDEV_NOLEGACY"/>，则系统不会为该应用程序的设备生成任何旧消息。例如，如果将鼠标 TLC 设置为 RIDEV_NOLEGACY，则不会生成 WM_LBUTTONDOWN 和相关的旧鼠标消息。同样，如果将键盘 TLC设置为 RIDEV_NOLEGACY，则不会生成 WM_KEYDOWN 和相关的旧式键盘消息。</para>
    /// <para>如果 <see cref="RawInputFlags.RIDEV_REMOVE"/> 设置和 hwndTarget 成员未设置为 NULL，则参数验证将失败。</para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-rawinputdevice </para>
    /// </summary>
    public struct RAWINPUTDEVICE
    {
        /// <summary>
        /// 原始输入设备的顶级收集使用情况页面。
        /// </summary>
        public ushort usUsagePage;
        /// <summary>
        /// 顶级集合原始输入设备的用法。
        /// </summary>
        public ushort usUsage;
        /// <summary>
        /// 模式标志，用于指定如何解释 usUsagePage 和 usUsage 提供的信息。
        /// </summary>
        public RawInputFlags dwFlags;
        /// <summary>
        /// 目标窗口的句柄。如果为 NULL，则跟随键盘焦点。
        /// </summary>
        public IntPtr hwndTarget;

        /// <summary>
        /// <see cref="RAWINPUTDEVICE"/> 结构体大小，以字节为单位。
        /// </summary>
        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE));

        public override string ToString()
        {
            return $"[RAWINPUTDEVICE] usUsagePage:{usUsagePage}, usUsage:{usUsage}, dwFlags:{dwFlags}, hwndTarget:{hwndTarget}";
        }
    }

    /// <summary>
    /// 包含有关原始输入设备的信息。(RAWINPUTDEVICELIST, *PRAWINPUTDEVICELIST)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-rawinputdevicelist </para>
    /// </summary>
    public struct RAWINPUTDEVICELIST
    {
        /// <summary>
        /// 原始输入设备的句柄。
        /// </summary>
        public IntPtr hDevice;
        /// <summary>
        /// 设备的类型
        /// </summary>
        public RawInputType dwType;

        /// <summary>
        /// <see cref="RAWINPUTDEVICELIST"/> 结构体大小，以字节为单位。
        /// </summary>
        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST));

        public override string ToString()
        {
            return $"hDevice:{hDevice}, dwType:{dwType}";
        }
    }

    /// <summary>
    /// 原始输入数据类型，联合体 Raw Union Data
    /// <para>注意：为了解决 32 与 64 位平台软件的兼容性问题，有才了这个结构体 </para>
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct RAWINPUTDATA
    {
        /// <summary>
        /// 如果原始输入 <see cref="RAWINPUTHEADER.dwType"/> 类型为 <see cref="RawInputType.RIM_TYPEMOUSE"/>, 则为 <see cref="RAWMOUSE"/> 结构体数据。
        /// </summary>
        [FieldOffset(0)]
        public RAWMOUSE mouse;
        /// <summary>
        /// 如果原始输入 <see cref="RAWINPUTHEADER.dwType"/> 类型为 <see cref="RawInputType.RIM_TYPEKEYBOARD"/>, 则为 <see cref="RAWKEYBOARD"/> 结构体数据。
        /// </summary>
        [FieldOffset(0)]
        public RAWKEYBOARD keyboard;
        /// <summary>
        /// 如果原始输入 <see cref="RAWINPUTHEADER.dwType"/> 类型为 <see cref="RawInputType.RIM_TYPEHID"/>, 则为 <see cref="RAWHID"/> 结构体数据。
        /// </summary>
        [FieldOffset(0)]
        public RAWHID hid;
    }

    /// <summary>
    /// 包含设备的原始输入。(RAWINPUT, *PRAWINPUT, *LPRAWINPUT)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-rawinput </para>
    /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getrawinputbuffer </para>
    /// </summary>
    //[StructLayout(LayoutKind.Explicit)]
    public struct RAWINPUT
    {
        /// <summary>
        /// 原始输入标头数据。
        /// </summary>
        //[FieldOffset(0)]
        public RAWINPUTHEADER header;

        /// <summary>
        /// 原始输入数据
        /// </summary>
        public RAWINPUTDATA data;

#if _32位_64位
        /// <summary>
        /// 如果原始输入 <see cref="RAWINPUTHEADER.dwType"/> 类型为 <see cref="RawInputType.RIM_TYPEMOUSE"/>, 则为 <see cref="RAWMOUSE"/> 结构体数据。
        /// </summary>
        [FieldOffset(24)]   //[FieldOffset(16 + 8)]
        public RAWMOUSE mouse;
        /// <summary>
        /// 如果原始输入 <see cref="RAWINPUTHEADER.dwType"/> 类型为 <see cref="RawInputType.RIM_TYPEKEYBOARD"/>, 则为 <see cref="RAWKEYBOARD"/> 结构体数据。
        /// </summary>
        [FieldOffset(24)]   //[FieldOffset(16 + 8)]
        public RAWKEYBOARD keyboard;
        /// <summary>
        /// 如果原始输入 <see cref="RAWINPUTHEADER.dwType"/> 类型为 <see cref="RawInputType.RIM_TYPEHID"/>, 则为 <see cref="RAWHID"/> 结构体数据。
        /// </summary>
        [FieldOffset(24)]   //[FieldOffset(16 + 8)]
        public RAWHID hid;
#endif

        public override string ToString()
        {
            if (header.dwType == RawInputType.RIM_TYPEMOUSE)
                return $"[RAWINPUT] header:[{header}], mouse:[{data.mouse}]";
            else if (header.dwType == RawInputType.RIM_TYPEKEYBOARD)
                return $"[RAWINPUT] header:[{header}], keyboard:[{data.keyboard}]";
            else
                return $"[RAWINPUT] header:[{header}], hid:[{data.hid}]";
        }
    }

    /// <summary>
    /// 包含作为原始输入数据一部分的标头信息。(RAWINPUTHEADER, *PRAWINPUTHEADER, *LPRAWINPUTHEADER)
    /// <para>要获取有关设备的更多信息，请在对 <see cref="User32.GetRawInputDeviceInfo(IntPtr, uint, IntPtr, ref uint)"/> 的调用中使用 hDevice。</para>
    /// <para>注意：<see cref="IntPtr"/> 类型数据，在 32 位软件上是占 4 个字节，在 64 位软件上是占 8 个字节</para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-rawinputheader </para>
    /// </summary>
    public struct RAWINPUTHEADER
    {
        /// <summary>
        /// 原始输入的类型。
        /// </summary>
        [MarshalAs(UnmanagedType.U4)]
        public RawInputType dwType;
        /// <summary>
        /// 整个输入数据包的大小（以字节为单位）。这包括 <see cref="RAWINPUT"/> 加上 <see cref="RAWHID"/> 可变长度数组中可能的其他输入报告。
        /// </summary>
        public uint dwSize;
        /// <summary>
        /// 生成原始输入数据的设备的句柄。
        /// </summary>
        public IntPtr hDevice;
        /// <summary>
        /// 在 <see cref="MessageType.WM_INPUT"/> 消息的 wParam 参数中传递的值 。
        /// </summary>
        public IntPtr wParam;

        /// <summary>
        /// <see cref="RAWINPUTHEADER"/> 结构大小，以字节为单位。
        /// <para>注意：<see cref="IntPtr"/> 类型数据，在 32 位软件上是占 4 个字节，在 64 位软件上是占 8 个字节</para>
        /// </summary>
        public readonly static uint Size = (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER));

        public override string ToString()
        {
            return $"[RAWINPUTHEADER] type:{dwType}, dwSize:{dwSize}, hDevice:{hDevice}, wParam:{wParam}";
        }
    }

    /// <summary>
    /// 包含有关鼠标状态的信息。(RAWMOUSE, *PRAWMOUSE, *LPRAWMOUSE)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-rawmouse </para>
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct RAWMOUSE
    {
        /// <summary>
        /// 鼠标状态
        /// </summary>
        [FieldOffset(0)]
        [MarshalAs(UnmanagedType.U2)]
        public RawMouseFlags usFlags;
        /// <summary>
        /// 保留。
        /// </summary>
        [FieldOffset(2)]
        [MarshalAs(UnmanagedType.U4)]
        public uint ulButtons;
        /// <summary>
        /// 鼠标按钮的过渡状态。该成员可以是一个或多个值。
        /// </summary>
        [FieldOffset(2)]
        [MarshalAs(UnmanagedType.U2)]
        public RawInputMouseFlags usButtonFlags;
        /// <summary>
        /// 如果 usButtonFlags 是 RI_MOUSE_WHEEL，则此成员是带符号的值，用于指定车轮增量。
        /// </summary>
        [FieldOffset(4)]
        [MarshalAs(UnmanagedType.U2)]
        public ushort usButtonData;
        /// <summary>
        /// 鼠标按钮的原始状态。
        /// </summary>
        [FieldOffset(6)]
        public uint ulRawButtons;
        /// <summary>
        /// X 方向上的运动。这是带符号的相对运动或绝对运动，具体取决于 usFlags 的值。
        /// </summary>
        [FieldOffset(10)]
        public int lLastX;
        /// <summary>
        /// Y 方向上的运动。这是带符号的相对运动或绝对运动，具体取决于 usFlags 的值。
        /// </summary>
        [FieldOffset(14)]
        public int lLastY;
        /// <summary>
        /// 事件的设备特定的附加信息。
        /// </summary>
        [FieldOffset(18)]
        public uint ulExtraInformation;

        public override string ToString()
        {
            return $"[RAWMOUSE] usFlags:{usFlags}, ulButtons:{ulButtons}, usButtonFlags:{usButtonFlags}, usButtonData:{usButtonData}, ulRawButtons:{ulRawButtons}, lLastX:{lLastX}, lLastY:{lLastY}, ulExtraInformation:{ulExtraInformation}";
        }
    }

    /// <summary>
    /// 包含有关键盘状态的信息。(RAWKEYBOARD, *PRAWKEYBOARD, *LPRAWKEYBOARD)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-rawkeyboard </para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RAWKEYBOARD
    {
        /// <summary>
        /// 按键按下时的扫描代码。键盘溢出的扫描代码为 KEYBOARD_OVERRUN_MAKE_CODE。
        /// </summary>
        public ushort MakeCode;
        /// <summary>
        /// 扫描代码信息的标志。可以是一项或多项。
        /// </summary>
        [MarshalAs(UnmanagedType.U2)]
        public RawInputKeyFlags Flags;
        /// <summary>
        /// 保留；必须为零。
        /// </summary>
        public ushort Reserved;
        /// <summary>
        /// Windows 消息兼容的虚拟键代码。有关更多信息，请参见 <see cref="VirtualKeyCode"/>。
        /// </summary>
        public ushort VKey;
        /// <summary>
        /// 相应的窗口消息，例如 <see cref="MessageType.WM_KEYDOWN"/>, <see cref="MessageType.WM_SYSKEYDOWN"/> 等。
        /// </summary>
        public uint Message;
        /// <summary>
        /// 事件的设备特定的附加信息。
        /// </summary>
        public uint ExtraInformation;

        public override string ToString()
        {
            return $"[RAWKEYBOARD] MakeCode:{MakeCode}, Flags:{Flags}, Reserved:{Reserved}, VKey:{(VirtualKeyCode)VKey}, Message:{(MessageType)Message}, ExtraInformation:{ExtraInformation}";
        }
    }

    /// <summary>
    /// 描述来自人机接口设备（HID）的原始输入的格式。(RAWHID, *PRAWHID, *LPRAWHID)
    /// <para>注意：该结构体为可变长数据结构体 </para>
    /// <para>每个 <see cref="MessageType.WM_INPUT"/> 可以指示多个输入，但是所有输入都来自相同的 HID。bRawData 数组的大小为 dwSizeHid * dwCount。</para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-rawhid </para>
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct RAWHID
    {
        /// <summary>
        /// bRawData 中每个 HID 输入的大小（以字节为单位）。
        /// </summary>
        public uint dwSizeHid;
        /// <summary>
        /// bRawData 中 HID 输入的数量。
        /// </summary>
        public uint dwCount;
        /// <summary>
        /// 原始输入数据(Buffer)，以字节数组形式。
        /// <para>参考：https://docs.microsoft.com/zh-cn/dotnet/standard/native-interop/best-practices </para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/dotnet/standard/native-interop/customize-struct-marshaling </para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/dotnet/framework/interop/default-marshaling-for-arrays?view=netframework-4.7.2 </para>
        /// </summary>
        private fixed byte bRawData[128];
        //[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1, SizeConst = 128, ArraySubType = UnmanagedType.U1)]
        //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        //public byte[] bRawData;

        //未测试
        //public IntPtr bRawData;

        /// <summary>
        /// 获取原始数据
        /// </summary>
        /// <returns></returns>
        public byte[] GetRawData()
        {
            byte[] data = new byte[dwSizeHid * dwCount];
            GetRawData(ref data);

            return data;
        }

        /// <summary>
        /// 获取原始数据，数据大小为 dwSizeHid * dwCount
        /// </summary>
        /// <param name="data"></param>
        public void GetRawData(ref byte[] data)
        {
            uint length = dwSizeHid * dwCount;

            if (data.Length < length)
                Array.Resize(ref data, (int)length);

            for (int i = 0; i < length; i++) data[i] = bRawData[i];

            //length--;
            //while(length > 0)
            //{
            //    data[length] = bRawData[length];
            //    length--;
            //}

            //Marshal.Copy(bRawData, 0, data, length);
        }

        public override string ToString()
        {
            return $"[RAWHID] dwSizeHid:{dwSizeHid}, dwCount:{dwCount}, TotalLength:{dwSizeHid * dwCount}";
        }
    }

    /// <summary>
    /// 定义来自任何设备的原始输入数据。(RID_DEVICE_INFO, *PRID_DEVICE_INFO, *LPRID_DEVICE_INFO)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-rid_device_info?redirectedfrom=MSDN </para>
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct RID_DEVICE_INFO
    {
        /// <summary>
        /// <see cref="RID_DEVICE_INFO"/> 结构的大小（以字节为单位）。
        /// </summary>
        [FieldOffset(0)]
        public uint cbSize;
        /// <summary>
        /// 原始输入数据的类型。见 <see cref="RawInputType"/>
        /// </summary>
        [FieldOffset(4)]
        [MarshalAs(UnmanagedType.U4)]
        public RawInputType dwType;
        /// <summary>
        /// 如果 dwType 为 <see cref="RawInputType.RIM_TYPEMOUSE"/>，则这是定义鼠标的 <see cref="RID_DEVICE_INFO_MOUSE"/> 结构。
        /// </summary>
        [FieldOffset(8)]
        public RID_DEVICE_INFO_MOUSE mouse;
        /// <summary>
        /// 如果 dwType 为 <see cref="RawInputType.RIM_TYPEKEYBOARD"/>，则这是定义键盘的 <see cref="RID_DEVICE_INFO_KEYBOARD"/> 结构。
        /// </summary>
        [FieldOffset(8)]
        public RID_DEVICE_INFO_KEYBOARD keyboard;
        /// <summary>
        /// 如果 dwType 为 <see cref="RawInputType.RIM_TYPEHID"/>，则这是定义 HID 设备的 <see cref="RID_DEVICE_INFO_HID"/> 结构。
        /// </summary>
        [FieldOffset(8)]
        public RID_DEVICE_INFO_HID hid;

        public override string ToString()
        {
            if (dwType == RawInputType.RIM_TYPEMOUSE)
                return $"[RAWINPUT] cbSize:{cbSize}, dwType:{dwType}, mouse:[{mouse}]";
            else if (dwType == RawInputType.RIM_TYPEKEYBOARD)
                return $"[RAWINPUT] cbSize:{cbSize}, dwType:{dwType}, keyboard:[{keyboard}]";
            else
                return $"[RAWINPUT] cbSize:{cbSize}, dwType:{dwType}, hid:[{hid}]";
        }
    }

    /// <summary>
    /// 定义来自指定人机界面设备（HID）的原始输入数据。(RID_DEVICE_INFO_HID, *PRID_DEVICE_INFO_HID)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-rid_device_info_hid?redirectedfrom=MSDN </para>
    /// </summary>
    public struct RID_DEVICE_INFO_HID
    {
        /// <summary>
        /// HID的供应商标识符。
        /// </summary>
        public uint dwVendorId;
        /// <summary>
        /// HID的产品标识符。
        /// </summary>
        public uint dwProductId;
        /// <summary>
        /// HID的版本号。
        /// </summary>
        public uint dwVersionNumber;
        /// <summary>
        /// 设备的顶级集合使用情况页面。
        /// </summary>
        public ushort usUsagePage;
        /// <summary>
        /// 设备的顶级集合用法。
        /// </summary>
        public ushort usUsage;

        public override string ToString()
        {
            return $"[RID_DEVICE_INFO_HID] dwVendorId:{dwVendorId}, dwProductId:{dwProductId}, dwVersionNumber:{dwVersionNumber}, usUsagePage:{usUsagePage}, usUsage:{usUsage}";
        }
    }

    /// <summary>
    /// 定义来自指定键盘的原始输入数据。(RID_DEVICE_INFO_KEYBOARD, *PRID_DEVICE_INFO_KEYBOARD)
    /// <para>For the keyboard, the Usage Page is 0x01 and the Usage is 0x06.</para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-rid_device_info_keyboard?redirectedfrom=MSDN </para>
    /// </summary>
    public struct RID_DEVICE_INFO_KEYBOARD
    {
        /// <summary>
        /// 键盘的类型。
        /// </summary>
        public uint dwType;
        /// <summary>
        /// 键盘的子类型。
        /// </summary>
        public uint dwSubType;
        /// <summary>
        /// 扫描码模式。
        /// </summary>
        public uint dwKeyboardMode;
        /// <summary>
        /// 键盘上的功能键数。
        /// </summary>
        public uint dwNumberOfFunctionKeys;
        /// <summary>
        /// 键盘上LED指示灯的数量。
        /// </summary>
        public uint dwNumberOfIndicators;
        /// <summary>
        /// 键盘上的键总数。
        /// </summary>
        public uint dwNumberOfKeysTotal;

        public override string ToString()
        {
            return $"[RID_DEVICE_INFO_KEYBOARD] dwType:{dwType}, dwSubType:{dwSubType}, dwKeyboardMode:{dwKeyboardMode}, dwNumberOfFunctionKeys:{dwNumberOfFunctionKeys}, dwNumberOfIndicators:{dwNumberOfIndicators}, dwNumberOfKeysTotal:{dwNumberOfKeysTotal}";
        }
    }

    /// <summary>
    /// 定义来自指定鼠标的原始输入数据。(RID_DEVICE_INFO_MOUSE, *PRID_DEVICE_INFO_MOUSE)
    /// <para>For the mouse, the Usage Page is 0x01 and the Usage is 0x02.</para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-rid_device_info_mouse?redirectedfrom=MSDN </para>
    /// </summary>
    public struct RID_DEVICE_INFO_MOUSE
    {
        /// <summary>
        /// 鼠标设备的标识符。
        /// </summary>
        public uint dwId;
        /// <summary>
        /// 鼠标的按钮数。
        /// </summary>
        public uint dwNumberOfButtons;
        /// <summary>
        /// 每秒的数据点数。此信息可能不适用于每个鼠标设备。
        /// </summary>
        public uint dwSampleRate;
        /// <summary>
        /// 如果鼠标具有滚轮可水平滚动，则为TRUE；否则为TRUE。否则为FALSE。
        /// </summary>
        [MarshalAs(UnmanagedType.Bool)]
        public bool fHasHorizontalWheel;

        public override string ToString()
        {
            return $"[RID_DEVICE_INFO_MOUSE] dwId:{dwId}, dwNumberOfButtons:{dwNumberOfButtons}, fHasHorizontalWheel:{fHasHorizontalWheel}";
        }
    }
    #endregion


    #region Notifications
    #endregion


    public static partial class User32
    {
        #region RegisterRawInputDevices
        /// <summary>
        /// 注册提供原始输入数据的设备。
        /// <para>要接收 <see cref="MessageType.WM_INPUT"/> 消息，应用程序必须首先使用 <see cref="RegisterRawInputDevices"/> 注册原始输入设备。默认情况下，应用程序不接收原始输入。</para>
        /// <para>要接收 <see cref="MessageType.WM_INPUT_DEVICE_CHANGE"/> 消息，应用程序必须为 <see cref="RAWINPUTDEVICE"/> 结构的 usUsagePage 和 usUsage 字段指定的每个设备类指定 RIDEV_DEVNOTIFY 标志 。默认情况下，应用程序不会收到 有关原始输入设备到达和移除的 <see cref="MessageType.WM_INPUT_DEVICE_CHANGE"/> 通知。</para>
        /// <para>如果 <see cref="RAWINPUTDEVICE"/> 结构设置了 <see cref="RawInputFlags.RIDEV_REMOVE"/> 标志，并且 hwndTarget 参数未设置为 NULL，则参数验证将失败。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerrawinputdevices </para>
        /// </summary>
        /// <param name="pRawInputDevices"><see cref="RAWINPUTDEVICE"/> 结构的数组，代表提供原始输入的设备。</param>
        /// <param name="uiNumDevices"><see cref="RAWINPUTDEVICE"/> 结构由指向 pRawInputDevices。</param>
        /// <param name="cbSize"><see cref="RAWINPUTDEVICE"/> 结构的大小（以字节为单位）。</param>
        /// <returns>如果函数成功，则为 TRUE；否则为 false。否则为 FALSE。如果函数失败，请调用 <see cref="Marshal.GetLastWin32Error"/> 以获取更多信息。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);
        #endregion

        #region GetRegisteredRawInputDevices
        /// <summary>
        /// 检索有关当前应用程序的原始输入设备的信息。
        /// <para>要从设备接收原始输入，应用程序必须使用 <see cref="RegisterRawInputDevices"/> 对其进行注册。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getregisteredrawinputdevices </para>
        /// </summary>
        /// <param name="pRawInputDevices">应用程序的 <see cref="RAWINPUTDEVICE"/> 结构的数组。</param>
        /// <param name="puiNumDevices"> pRawInputDevices 中的 <see cref="RAWINPUTDEVICE"/> 结构的数量。</param>
        /// <param name="cbSize"><see cref="RAWINPUTDEVICE"/> 结构的大小（以字节为单位）。</param>
        /// <returns>如果成功，该函数将返回一个非负数，即写入缓冲区的 <see cref="RAWINPUTDEVICE"/> 结构的数目。
        ///     <para>如果 pRawInputDevices 缓冲区太小或为 NULL，则该函数将最后一个错误设置为 ERROR_INSUFFICIENT_BUFFER，返回 -1，并将 puiNumDevices 设置为所需的设备数。
        ///     如果函数由于任何其他原因而失败，则返回 -1。有关更多详细信息，请调用 <see cref="Marshal.GetLastWin32Error"/>。</para>
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetRegisteredRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, ref uint puiNumDevices, uint cbSize);
        /// <summary>
        /// 检索有关当前应用程序的原始输入设备的信息。
        /// <para>要从设备接收原始输入，应用程序必须使用 <see cref="RegisterRawInputDevices"/> 对其进行注册。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getregisteredrawinputdevices </para>
        /// </summary>
        /// <param name="pRawInputDevices">应用程序的 <see cref="RAWINPUTDEVICE"/> 结构的数组。</param>
        /// <param name="puiNumDevices"> pRawInputDevices 中的 <see cref="RAWINPUTDEVICE"/> 结构的数量。</param>
        /// <param name="cbSize"><see cref="RAWINPUTDEVICE"/> 结构的大小（以字节为单位）。</param>
        /// <returns>如果成功，该函数将返回一个非负数，即写入缓冲区的 <see cref="RAWINPUTDEVICE"/> 结构的数目。
        ///     <para>如果 pRawInputDevices 缓冲区太小或为 NULL，则该函数将最后一个错误设置为 ERROR_INSUFFICIENT_BUFFER，返回 -1，并将 puiNumDevices 设置为所需的设备数。
        ///     如果函数由于任何其他原因而失败，则返回 -1。有关更多详细信息，请调用 <see cref="Marshal.GetLastWin32Error"/>。</para>
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetRegisteredRawInputDevices(IntPtr pRawInputDevices, ref uint puiNumDevices, uint cbSize);
        #endregion

        #region GetRawInputData
        /// <summary>
        /// 检索来自指定设备的原始输入。
        /// <para><see cref="GetRawInputData(IntPtr, RIDFlag, IntPtr, ref uint, uint)"/> 一次获取原始输入一个 <see cref="RAWINPUT"/> 结构。相反，<see cref="GetRawInputBuffer(IntPtr, ref uint, uint)"/> 获取一个 <see cref="RAWINPUT"/> 结构的数组。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getrawinputdata </para>
        /// </summary>
        /// <param name="hRawInput"><see cref="RAWINPUT"/> 结构的句柄。这个来自 lParam 的在 <see cref="MessageType.WM_INPUT"/>。 </param>
        /// <param name="uiCommand">命令标志, 是 <see cref="RIDFlag"/>值之一 </param>
        /// <param name="pData">指向来自 <see cref="RAWINPUT"/> 结构的数据的指针。这取决于 uiCommand 的值 。如果 pData 为 NULL，则以 pcbSize 返回所需的缓冲区大小。</param>
        /// <param name="pcbSize">pData 中数据的大小（以字节为单位） 。</param>
        /// <param name="cbSizeHeader"><see cref="RAWINPUTHEADER"/> 结构的大小（以字节为单位）。</param>
        /// <returns>如果 pData 为 NULL 并且函数成功，则返回值为 0。如果 pData 不为 NULL 并且函数成功，则返回值为复制到 pData 中的字节数。如果有错误，则返回值为（UINT）-1。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetRawInputData(RAWINPUT hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);
        /// <summary>
        /// 检索来自指定设备的原始输入。
        /// <para><see cref="GetRawInputData(IntPtr, RIDFlag, IntPtr, ref uint, uint)"/> 一次获取原始输入一个 <see cref="RAWINPUT"/> 结构。相反，<see cref="GetRawInputBuffer(IntPtr, ref uint, uint)"/> 获取一个 <see cref="RAWINPUT"/> 结构的数组。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getrawinputdata </para>
        /// </summary>
        /// <param name="hRawInput"><see cref="RAWINPUT"/> 结构的句柄。这个来自 lParam 的在 <see cref="MessageType.WM_INPUT"/>。 </param>
        /// <param name="uiCommand">命令标志, 是 <see cref="RIDFlag"/>值之一 </param>
        /// <param name="pData">指向来自 <see cref="RAWINPUT"/> 结构的数据的指针。这取决于 uiCommand 的值 。如果 pData 为 NULL，则以 pcbSize 返回所需的缓冲区大小。</param>
        /// <param name="pcbSize">pData 中数据的大小（以字节为单位） 。</param>
        /// <param name="cbSizeHeader"><see cref="RAWINPUTHEADER"/> 结构的大小（以字节为单位）。</param>
        /// <returns>如果 pData 为 NULL 并且函数成功，则返回值为 0。如果 pData 不为 NULL 并且函数成功，则返回值为复制到 pData 中的字节数。如果有错误，则返回值为（UINT）-1。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetRawInputData(IntPtr hRawInput, RIDFlag uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);
        /// <summary>
        /// 检索来自指定设备的原始输入。
        /// <para><see cref="GetRawInputData(IntPtr, RIDFlag, IntPtr, ref uint, uint)"/> 一次获取原始输入一个 <see cref="RAWINPUT"/> 结构。相反，<see cref="GetRawInputBuffer(IntPtr, ref uint, uint)"/> 获取一个 <see cref="RAWINPUT"/> 结构的数组。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getrawinputdata </para>
        /// </summary>
        /// <param name="hRawInput"><see cref="RAWINPUT"/> 结构的句柄。这个来自 lParam 的在 <see cref="MessageType.WM_INPUT"/>。 </param>
        /// <param name="uiCommand">命令标志, 是 <see cref="RIDFlag"/>值之一 </param>
        /// <param name="pData">指向来自 <see cref="RAWINPUT"/> 结构的数据的指针。这取决于 uiCommand 的值 。如果 pData 为 NULL，则以 pcbSize 返回所需的缓冲区大小。</param>
        /// <param name="pcbSize">pData 中数据的大小（以字节为单位） 。</param>
        /// <param name="cbSizeHeader"><see cref="RAWINPUTHEADER"/> 结构的大小（以字节为单位）。</param>
        /// <returns>如果 pData 为 NULL 并且函数成功，则返回值为 0。如果 pData 不为 NULL 并且函数成功，则返回值为复制到 pData 中的字节数。如果有错误，则返回值为（UINT）-1。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetRawInputData(IntPtr hRawInput, RIDFlag uiCommand, ref RAWINPUT pData, ref uint pcbSize, uint cbSizeHeader);
        #endregion

        #region GetRawInputBuffer
        /// <summary>
        /// 对原始输入数据执行缓冲读取。
        /// <para>注意：要获得原始输入缓冲区的正确大小，请不要使用 pcbSize，而应使用 pcbSize * 8。为确保 <see cref="GetRawInputBuffer(IntPtr, ref uint, uint)"/> 在 WOW64 上正常运行，必须将 <see cref="RAWINPUT"/> 结构对齐 8 个字节。</para>
        /// <para>使用 <see cref="GetRawInputBuffer(IntPtr, ref uint, uint)"/>，原始输入数据被缓存在 <see cref="RAWINPUT"/> 结构的数组中。对于无缓冲读取，请使用 <see cref="GetMessage"/> 函数读取原始输入数据。所述 <see cref="NEXTRAWINPUTBLOCK"/> 宏允许应用程序遍历的阵列 <see cref="RAWINPUT"/> 结构。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getrawinputbuffer </para>
        /// </summary>
        /// <param name="pData">指向包含原始输入数据的 <see cref="RAWINPUT"/> 结构的缓冲区的指针。如果为 NULL，则在 pcbSize 中返回所需的最小缓冲区（以字节为单位）。</param>
        /// <param name="pcbSize"> <see cref="RAWINPUT"/> 结构的大小（以字节为单位）。</param>
        /// <param name="cbSizeHeader"><see cref="RAWINPUTHEADER"/> 结构的大小（以字节为单位）。</param>
        /// <returns>如果 pData 为 NULL 并且函数成功，则返回值为零。如果 pData 不为 NULL 并且函数成功，则返回值是写入 pData 的 <see cref="RAWINPUT"/> 结构 的数量。
        ///     <para>如果发生错误，则返回值为（UINT）-1。调用 <see cref="Marshal.GetLastWin32Error"/> 作为错误代码。</para>
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetRawInputBuffer(RAWINPUT[] pData, ref uint pcbSize, uint cbSizeHeader);
        /// <summary>
        /// 对原始输入数据执行缓冲读取。
        /// <para>注意：要获得原始输入缓冲区的正确大小，请不要使用 pcbSize，而应使用 pcbSize * 8。为确保 <see cref="GetRawInputBuffer(IntPtr, ref uint, uint)"/> 在 WOW64 上正常运行，必须将 <see cref="RAWINPUT"/> 结构对齐 8 个字节。</para>
        /// <para>使用 <see cref="GetRawInputBuffer(IntPtr, ref uint, uint)"/>，原始输入数据被缓存在 <see cref="RAWINPUT"/> 结构的数组中。对于无缓冲读取，请使用 <see cref="GetMessage"/> 函数读取原始输入数据。所述 <see cref="NEXTRAWINPUTBLOCK"/> 宏允许应用程序遍历的阵列 <see cref="RAWINPUT"/> 结构。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getrawinputbuffer </para>
        /// </summary>
        /// <param name="pData">指向包含原始输入数据的 <see cref="RAWINPUT"/> 结构的缓冲区的指针。如果为 NULL，则在 pcbSize 中返回所需的最小缓冲区（以字节为单位）。</param>
        /// <param name="pcbSize"> <see cref="RAWINPUT"/> 结构的大小（以字节为单位）。</param>
        /// <param name="cbSizeHeader"><see cref="RAWINPUTHEADER"/> 结构的大小（以字节为单位）。</param>
        /// <returns>如果 pData 为 NULL 并且函数成功，则返回值为零。如果 pData 不为 NULL 并且函数成功，则返回值是写入 pData 的 <see cref="RAWINPUT"/> 结构 的数量。
        ///     <para>如果发生错误，则返回值为（UINT）-1。调用 <see cref="Marshal.GetLastWin32Error"/> 作为错误代码。</para>
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetRawInputBuffer(IntPtr pData, ref uint pcbSize, uint cbSizeHeader);
        #endregion

        #region GetRawInputDeviceInfo
        /// <summary>
        /// 检索有关原始输入设备的信息。
        /// <para>注意：因为字符集的不同，实际应为 pcbSize * 2，才能保证在不同语言的系统上正常运行。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getrawinputdeviceinfoa </para>
        /// </summary>
        /// <param name="hDevice">原始输入设备的句柄。这个来自 hDevice 成员 <see cref="RAWINPUTHEADER"/> 或 <see cref="GetRawInputDeviceList(IntPtr, ref uint, uint)"/>。</param>
        /// <param name="uiCommand">指定将在 pData 中返回什么数据，参考 <see cref="RIDIFlag"/> </param>
        /// <param name="pData">指向包含 uiCommand 指定的信息的缓冲区的指针 。如果 uiCommand 是 <see cref="RIDIFlag.RIDI_DEVICEINFO"/>，设定 cbSize 成员 RID_DEVICE_INFO 来 sizeof(RID_DEVICE_INFO) 调用之前 <see cref="GetRawInputDeviceInfo(IntPtr, uint, IntPtr, ref uint)"/>。</param>
        /// <param name="pcbSize">pData 中数据的大小（以字节为单位） 。</param>
        /// <returns>如果成功，此函数将返回一个非负数，指示复制到 pData 的字节数 。
        ///     <para>如果 pData 不足以容纳数据，则该函数返回-1。如果 pData 为 NULL，则该函数返回零值。在这两种情况下， pcbSize 均设置为 pData 缓冲区所需的最小大小 。</para>
        ///     <para>调用 <see cref="Marshal.GetLastWin32Error"/> 以识别其他任何错误。</para>
        /// </returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);
        /// <summary>
        /// 检索有关原始输入设备的信息。
        /// <para>注意：因为字符集的不同，实际应为 pcbSize * 2，才能保证在不同语言的系统上正常运行。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getrawinputdeviceinfoa </para>
        /// </summary>
        /// <param name="hDevice">原始输入设备的句柄。这个来自 hDevice 成员 <see cref="RAWINPUTHEADER"/> 或 <see cref="GetRawInputDeviceList(IntPtr, ref uint, uint)"/>。</param>
        /// <param name="uiCommand">指定将在 pData 中返回什么数据，参考 <see cref="RIDIFlag"/> </param>
        /// <param name="pData">指向包含 uiCommand 指定的信息的缓冲区的指针 。如果 uiCommand 是 <see cref="RIDIFlag.RIDI_DEVICEINFO"/>，设定 cbSize 成员 RID_DEVICE_INFO 来 sizeof(RID_DEVICE_INFO) 调用之前 <see cref="GetRawInputDeviceInfo(IntPtr, uint, IntPtr, ref uint)"/>。</param>
        /// <param name="pcbSize">pData 中数据的大小（以字节为单位） 。</param>
        /// <returns>如果成功，此函数将返回一个非负数，指示复制到 pData 的字节数 。
        ///     <para>如果 pData 不足以容纳数据，则该函数返回-1。如果 pData 为 NULL，则该函数返回零值。在这两种情况下， pcbSize 均设置为 pData 缓冲区所需的最小大小 。</para>
        ///     <para>调用 <see cref="Marshal.GetLastWin32Error"/> 以识别其他任何错误。</para>
        /// </returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetRawInputDeviceInfo(IntPtr hDevice, RIDIFlag uiCommand, IntPtr pData, ref uint pcbSize);
        #endregion

        #region GetRawInputDeviceList
        /// <summary>
        /// 枚举连接到系统的原始输入设备。
        /// <para>此功能返回的设备是鼠标，键盘和其他人机接口设备（HID）设备。要获取有关所连接设备的更多详细信息，请使用 <see cref="RAWINPUTDEVICELIST"/> 中的 hDevice 调用 <see cref="GetRawInputDeviceInfo(IntPtr, uint, IntPtr, ref uint)"/>。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getrawinputdevicelist </para>
        /// </summary>
        /// <param name="pRawInputDeviceList">用于连接到系统的设备的 <see cref="RAWINPUTDEVICELIST"/> 结构的数组。如果为 NULL，则在 puiNumDevices 中返回设备数。</param>
        /// <param name="puiNumDevices">如果 pRawInputDeviceList 为 NULL，则该函数将使用连接到系统的设备数填充此变量；否则，此变量指定 pRawInputDeviceList 指向的缓冲区中可以包含的 <see cref="RAWINPUTDEVICELIST"/> 结构的数量。如果该值小于连接到系统的设备数，则该函数返回此变量中的实际设备数，并失败，并显示 ERROR_INSUFFICIENT_BUFFER。</param>
        /// <param name="cbSize"><see cref="RAWINPUTDEVICELIST"/> 结构的大小，以字节为单位。</param>
        /// <returns>如果函数成功，则返回值是 pRawInputDeviceList 指向的缓冲区中存储的设备数 。发生任何其他错误时，函数将返回（UINT）-1，而 <see cref="Marshal.GetLastWin32Error"/> 将返回错误指示。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetRawInputDeviceList(RAWINPUTDEVICELIST[] pRawInputDeviceList, ref uint puiNumDevices, uint cbSize);
        /// <summary>
        /// 枚举连接到系统的原始输入设备。        
        /// <para>此功能返回的设备是鼠标，键盘和其他人机接口设备（HID）设备。要获取有关所连接设备的更多详细信息，请使用 <see cref="RAWINPUTDEVICELIST"/> 中的 hDevice 调用 <see cref="GetRawInputDeviceInfo(IntPtr, uint, IntPtr, ref uint)"/>。</para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getrawinputdevicelist </para>
        /// </summary>
        /// <param name="pRawInputDeviceList">用于连接到系统的设备的 <see cref="RAWINPUTDEVICELIST"/> 结构的数组。如果为 NULL，则在 puiNumDevices 中返回设备数。</param>
        /// <param name="puiNumDevices">如果 pRawInputDeviceList 为 NULL，则该函数将使用连接到系统的设备数填充此变量；否则，此变量指定 pRawInputDeviceList 指向的缓冲区中可以包含的 <see cref="RAWINPUTDEVICELIST"/> 结构的数量。如果该值小于连接到系统的设备数，则该函数返回此变量中的实际设备数，并失败，并显示 ERROR_INSUFFICIENT_BUFFER。</param>
        /// <param name="cbSize"><see cref="RAWINPUTDEVICELIST"/> 结构的大小，以字节为单位。</param>
        /// <returns>如果函数成功，则返回值是 pRawInputDeviceList 指向的缓冲区中存储的设备数 。发生任何其他错误时，函数将返回（UINT）-1，而 <see cref="Marshal.GetLastWin32Error"/> 将返回错误指示。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetRawInputDeviceList(IntPtr pRawInputDeviceList, ref uint puiNumDevices, uint cbSize);
        #endregion

        #region DefRawInputProc
        /// <summary>
        /// 调用默认原始输入过程，以为应用程序未处理的任何原始输入消息提供默认处理。此功能确保处理所有消息。使用窗口过程接收到的相同参数调用 <see cref="DefRawInputProc(IntPtr, int, uint)"/>。
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-defrawinputproc </para>
        /// </summary>
        /// <param name="paRawInput"><see cref="RAWINPUT"/> 结构的数组。</param>
        /// <param name="nInput"> paRawInput 数组数量</param>
        /// <param name="cbSizeHeader"><see cref="RAWINPUTHEADER"/> 结构的大小（以字节为单位）。</param>
        /// <returns>如果成功，函数将返回 S_OK。否则，它将返回错误值。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr DefRawInputProc(RAWINPUT[] paRawInput, int nInput, uint cbSizeHeader);
        /// <summary>
        /// 调用默认原始输入过程，以为应用程序未处理的任何原始输入消息提供默认处理。此功能确保处理所有消息。使用窗口过程接收到的相同参数调用 <see cref="DefRawInputProc(IntPtr, int, uint)"/>。
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-defrawinputproc </para>
        /// </summary>
        /// <param name="paRawInput"><see cref="RAWINPUT"/> 结构的数组。</param>
        /// <param name="nInput"> paRawInput 数组数量</param>
        /// <param name="cbSizeHeader"><see cref="RAWINPUTHEADER"/> 结构的大小（以字节为单位）。</param>
        /// <returns>如果成功，函数将返回 S_OK。否则，它将返回错误值。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr DefRawInputProc(IntPtr paRawInput, int nInput, uint cbSizeHeader);
        #endregion


        #region Macros    
        /// <summary>
        /// 检索从输入代码的 wParam 在 <see cref="MessageType.WM_INPUT"/>。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-get_rawinput_code_wparam?redirectedfrom=MSDN </para>
        /// </summary>
        /// <param name="wParam">此参数可以是 0:RIM_INPUT, 1:RIM_INPUTSINK</param>
        /// <returns></returns>
        public static int GET_RAWINPUT_CODE_WPARAM(IntPtr wParam) { return ((wParam.ToInt32()) & 0xFF); }

        /// <summary>
        /// 检索 <see cref="RAWINPUT"/> 结构数组中下一个结构的位置。
        /// <para>重复调用此宏以遍历 <see cref="RAWINPUT"/> 结构的数组。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-nextrawinputblock </para>
        /// </summary>
        /// <param name="ptr">指向 <see cref="RAWINPUT"/> 结构数组中的结构的指针。</param>
        /// <returns></returns>
        public static IntPtr NEXTRAWINPUTBLOCK(IntPtr ptr)
        {
            RAWINPUT input = (RAWINPUT)Marshal.PtrToStructure(ptr, typeof(RAWINPUT));
            //return ptr += (int)(input.header.dwSize);// + Marshal.SystemDefaultCharSize);
            return IntPtr.Add(ptr, (int)input.header.dwSize);
        }

        #endregion
    }

    /// <summary>
    /// WindowsAPI User32库，扩展常用/通用，功能/函数，扩展示例，以及使用方式
    /// </summary>
    public static partial class User32Extension
    {
        /// <summary>
        /// 跟据原始输入设备句柄获取设备名称
        /// </summary>
        /// <param name="hDevice">原始输入设备句柄</param>
        /// <param name="name">设备名称</param>
        /// <returns></returns>
        public static bool TryGetRawInputDeviceName(IntPtr hDevice, ref string name)
        {
            if (hDevice == null || hDevice == IntPtr.Zero) return false;

            //step 01，get device data size
            uint pcbSize = 0;
            int result = User32.GetRawInputDeviceInfo(hDevice, RIDIFlag.RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);
            if (result != 0)
            {
                Exception ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                Console.WriteLine("LastWin32Error Code:{0},{1}  Message:{2}", Marshal.GetLastWin32Error(), Marshal.GetHRForLastWin32Error(), ex.Message);
                return false;
            }

            //step 02, get device info
            IntPtr pData = Marshal.AllocHGlobal((int)pcbSize * 2); //这里一定要比 pcbSize 实际大小大
            result = User32.GetRawInputDeviceInfo(hDevice, RIDIFlag.RIDI_DEVICENAME, pData, ref pcbSize);
            if (result != pcbSize)
            {
                Marshal.FreeHGlobal(pData);
                Exception ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                Console.WriteLine("LastWin32Error Code:{0},{1}  Message:{2}", Marshal.GetLastWin32Error(), Marshal.GetHRForLastWin32Error(), ex.Message);
                return false;
            }

            name = Marshal.PtrToStringAuto(pData, (int)pcbSize);
            Marshal.FreeHGlobal(pData);

            return true;
        }
        /// <summary>
        /// 获取原始输入设备名称列表。以设备句柄为 key, 设备名称为 value
        /// </summary>
        /// <returns>返回 以设备句柄为 key, 设备名称为值的可读字典。</returns>
        public static IReadOnlyDictionary<IntPtr, String> GetRawInputDevicesName()
        {
            // step 01, get devices data size
            uint deviceCount = 0;
            uint cbSize = (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST));

            int result = User32.GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, cbSize);
            if (result != 0)
            {
                Exception ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                Console.WriteLine("LastWin32Error Code:{0},{1}  Message:{2}", Marshal.GetLastWin32Error(), Marshal.GetHRForLastWin32Error(), ex.Message);
                return null;
            }

            // setp 02, get devices data
            IntPtr devicesList = Marshal.AllocHGlobal((int)(deviceCount * cbSize));
            result = User32.GetRawInputDeviceList(devicesList, ref deviceCount, cbSize);
            if (result != deviceCount)
            {
                Marshal.FreeHGlobal(devicesList);
                Exception ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                Console.WriteLine("LastWin32Error Code:{0},{1}  Message:{2}", Marshal.GetLastWin32Error(), Marshal.GetHRForLastWin32Error(), ex.Message);
                return null;
            }

            // step 03, get devices name
            Dictionary<IntPtr, String> devices = new Dictionary<IntPtr, string>((int)deviceCount);
            for (int i = 0; i < deviceCount; i++)
            {
                RAWINPUTDEVICELIST rid = Marshal.PtrToStructure<RAWINPUTDEVICELIST>(IntPtr.Add(devicesList, (int)(cbSize * i)));

                string name = null;
                if (TryGetRawInputDeviceName(rid.hDevice, ref name))
                    devices.Add(rid.hDevice, name);
            }

            // step 04, free
            Marshal.FreeHGlobal(devicesList);

            return devices;
        }

        /// <summary>
        /// 跟据原始输入设备句柄获取设备信息
        /// </summary>
        /// <param name="hDevice">原始输入设备句柄</param>
        /// <param name="deviceInfo">设备信息</param>
        /// <returns></returns>
        public static bool TryGetRawInputDeviceInfo(IntPtr hDevice, ref RID_DEVICE_INFO deviceInfo)
        {
            if (hDevice == null || hDevice == IntPtr.Zero) return false;

            //step 01，get device info size
            uint pcbSize = 0;
            int result = User32.GetRawInputDeviceInfo(hDevice, RIDIFlag.RIDI_DEVICEINFO, IntPtr.Zero, ref pcbSize);
            if (result != 0)
            {
                Exception ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                Console.WriteLine("LastWin32Error Code:{0},{1}  Message:{2}", Marshal.GetLastWin32Error(), Marshal.GetHRForLastWin32Error(), ex.Message);
                return false;
            }

            //step 02, get device info
            IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
            result = User32.GetRawInputDeviceInfo(hDevice, RIDIFlag.RIDI_DEVICEINFO, pData, ref pcbSize);
            if (result != pcbSize)
            {
                Marshal.FreeHGlobal(pData);
                Exception ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                Console.WriteLine("LastWin32Error Code:{0},{1}  Message:{2}", Marshal.GetLastWin32Error(), Marshal.GetHRForLastWin32Error(), ex.Message);
                return false;
            }

            deviceInfo = (RID_DEVICE_INFO)Marshal.PtrToStructure(pData, typeof(RID_DEVICE_INFO));
            Marshal.FreeHGlobal(pData);
            return true;
        }
        /// <summary>
        /// 获取原始输入设备信息列表。。以设备句柄为 key, 设备信息为 value
        /// </summary>
        /// <returns></returns>
        public static IReadOnlyDictionary<IntPtr, RID_DEVICE_INFO> GetRawIntputDevicesInfo()
        {
            // step 01, get devices data size
            uint deviceCount = 0;
            uint cbSize = RAWINPUTDEVICELIST.Size;

            int result = User32.GetRawInputDeviceList(IntPtr.Zero, ref deviceCount, cbSize);
            if (result != 0)
            {
                Exception ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                Console.WriteLine("LastWin32Error Code:{0},{1}  Message:{2}", Marshal.GetLastWin32Error(), Marshal.GetHRForLastWin32Error(), ex.Message);
                return null;
            }

            // setp 02, get devices data
            IntPtr devicesList = Marshal.AllocHGlobal((int)(deviceCount * cbSize));
            result = User32.GetRawInputDeviceList(devicesList, ref deviceCount, cbSize);
            if (result != deviceCount)
            {
                Marshal.FreeHGlobal(devicesList);
                Exception ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                Console.WriteLine("LastWin32Error Code:{0},{1}  Message:{2}", Marshal.GetLastWin32Error(), Marshal.GetHRForLastWin32Error(), ex.Message);
                return null;
            }

            // step 03, get devices name
            Dictionary<IntPtr, RID_DEVICE_INFO> devices = new Dictionary<IntPtr, RID_DEVICE_INFO>((int)deviceCount);
            for (int i = 0; i < deviceCount; i++)
            {
                RAWINPUTDEVICELIST rid = Marshal.PtrToStructure<RAWINPUTDEVICELIST>(IntPtr.Add(devicesList, (int)(cbSize * i)));

                RID_DEVICE_INFO info = new RID_DEVICE_INFO();
                if (TryGetRawInputDeviceInfo(rid.hDevice, ref info))
                    devices.Add(rid.hDevice, info);
            }

            // step 04, free
            Marshal.FreeHGlobal(devicesList);

            return devices;
        }

        /// <summary>
        /// 获取原始输入 <see cref="RAWINPUTHEADER"/> 结构数据
        /// </summary>
        /// <param name="lParam"><see cref="MessageType.WM_INPUT"/> lParam 参数</param>
        /// <param name="header">输出 <see cref="RAWINPUTHEADER"/> 结构体。</param>
        /// <returns></returns>
        public static bool TryGetRawInputHeader(IntPtr lParam, ref RAWINPUTHEADER header)
        {
            if (lParam == null || lParam == IntPtr.Zero) return false;

            uint pcbSize = RAWINPUTHEADER.Size;
            IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
            int result = User32.GetRawInputData(lParam, RIDFlag.RID_HEADER, pData, ref pcbSize, RAWINPUTHEADER.Size);
            
            if (result != pcbSize)
            {
                Marshal.FreeHGlobal(pData);

                Exception ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                Console.WriteLine("LastWin32Error Code:{0},{1}  Message:{2}", Marshal.GetLastWin32Error(), Marshal.GetHRForLastWin32Error(), ex.Message);
                return false;
            }

            header = (RAWINPUTHEADER)Marshal.PtrToStructure(pData, typeof(RAWINPUTHEADER));
            Marshal.FreeHGlobal(pData);

            return true;
        }

        /// <summary>
        /// 获取原始输入 <see cref="RAWINPUT"/> 结构数据
        /// </summary>
        /// <param name="lParam"><see cref="MessageType.WM_INPUT"/> lParam 参数</param>
        /// <param name="data">输出 <see cref="RAWINPUT"/> 结构体。</param>
        /// <param name="dwSize"> <see cref="RAWINPUTHEADER.dwSize"/></param>
        /// <returns></returns>
        public static bool TryGetRawInputData(IntPtr lParam, ref RAWINPUT data, ref uint dwSize)
        {
            if (lParam == null || lParam == IntPtr.Zero) return false;

            IntPtr pData = Marshal.AllocHGlobal((int)dwSize);
            int result = User32.GetRawInputData(lParam, RIDFlag.RID_INPUT, pData, ref dwSize, RAWINPUTHEADER.Size);
            if (result != dwSize)
            {
                Marshal.FreeHGlobal(pData);

                Exception ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                Console.WriteLine("LastWin32Error Code:{0},{1}  Message:{2}", Marshal.GetLastWin32Error(), Marshal.GetHRForLastWin32Error(), ex.Message);
                return false;
            }

            data = (RAWINPUT)Marshal.PtrToStructure(pData, typeof(RAWINPUT));
            Marshal.FreeHGlobal(pData);

            return true;
        }

        /// <summary>
        /// 获取原始输入 <see cref="RAWINPUT"/> 结构数据
        /// </summary>
        /// <param name="lParam"><see cref="MessageType.WM_INPUT"/> lParam 参数</param>
        /// <param name="data">输出 <see cref="RAWINPUT"/> 结构体。</param>
        /// <returns></returns>
        public static bool TryGetRawInputData(IntPtr lParam, ref RAWINPUT data)
        {
            if (lParam == null || lParam == IntPtr.Zero) return false;

            // step 01: get input size
            uint pcbSize = 0;
            int result = User32.GetRawInputData(lParam, RIDFlag.RID_INPUT, IntPtr.Zero, ref pcbSize, RAWINPUTHEADER.Size);
            if (result != 0)
            {
                Exception ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                Console.WriteLine("LastWin32Error Code:{0},{1}  Message:{2}", Marshal.GetLastWin32Error(), Marshal.GetHRForLastWin32Error(), ex.Message);
                return false;
            }

            // step 02: get input data
            IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
            result = User32.GetRawInputData(lParam, RIDFlag.RID_INPUT, pData, ref pcbSize, RAWINPUTHEADER.Size);
            if (result != pcbSize)
            {
                Marshal.FreeHGlobal(pData);
                Exception ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                Console.WriteLine("LastWin32Error Code:{0},{1}  Message:{2}", Marshal.GetLastWin32Error(), Marshal.GetHRForLastWin32Error(), ex.Message);
                return false;
            }

            data = (RAWINPUT)Marshal.PtrToStructure(pData, typeof(RAWINPUT));
            Marshal.FreeHGlobal(pData);

            return true;
        }

    }

}
