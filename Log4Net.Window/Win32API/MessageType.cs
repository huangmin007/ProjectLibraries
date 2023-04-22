using System;

namespace Win32API.User32
{

    #region Enumerations
    /// <summary>
    /// Windows Message Type (Window消息类型)
    /// <para>消息标识符值：系统：0x0000-0x03FF(WM_USER-1), 私人消息：0x8000-0xBFFF，RegisterWindowMessage：0xC000-0xFFFF</para>
    /// <para>注意：可能有部份值未添加到 <see cref="MessageType"/> 枚举(未找到，或是未证实) </para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/winmsg/about-messages-and-message-queues?redirectedfrom=MSDN </para>
    /// <para>消息类型：https://docs.microsoft.com/zh-cn/windows/win32/winmsg/about-messages-and-message-queues?redirectedfrom=MSDN#message-types </para>
    /// <para>窗口类消息：https://docs.microsoft.com/en-us/windows/win32/winmsg/window-notifications </para>
    /// <para>原生类消息：https://docs.microsoft.com/en-us/windows/win32/inputdev/raw-input-notifications </para>
    /// <para>键盘类消息：https://docs.microsoft.com/en-us/windows/win32/inputdev/keyboard-input-notifications </para>
    /// <para>鼠标类消息：https://docs.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications </para>
    /// <para>钩子类消息：https://docs.microsoft.com/en-us/windows/win32/winmsg/hook-notifications </para>
    /// </summary>
    public enum MessageType:uint
    {
        
        #region WM General
        #region Clipboard Messages
        WM_CUT = 0x0300,
        WM_COPY = 0x0301,
        WM_PASTE = 0x0302,
        WM_CLEAR = 0x0303,
        WM_UNDO = 0x0304,
        #endregion

        #region Clipboard Notifications
        WM_ASKCBFORMATNAME = 0x030C,
        WM_CHANGECBCHAIN = 0x030D,
        WM_CLIPBOARDUPDATE = 0x031D,
        WM_DESTROYCLIPBOARD = 0x0307,
        WM_DRAWCLIPBOARD = 0x0308,
        WM_HSCROLLCLIPBOARD = 0x030E,
        WM_PAINTCLIPBOARD = 0x0309,
        WM_RENDERALLFORMATS = 0x0306,
        WM_RENDERFORMAT = 0x0305,
        WM_SIZECLIPBOARD = 0x030B,
        WM_VSCROLLCLIPBOARD = 0x030A,
        #endregion

        #region Common Dialog Box Notifications
        #endregion

        #region Cursor Notifications
        /// <summary>
        ///如果鼠标引起光标在某个窗口中移动且鼠标输入没有被捕获时，就发消息给某个窗口
        /// </summary>
        WM_SETCURSOR = 0x20,
        #endregion

        #region Data Copy Message
        /// <summary>
        ///当一个应用程序传递数据给另一个应用程序时发送此消息
        /// </summary>
        WM_COPYDATA = 0x4A,
        #endregion

        #region Desktop Window Manager Messages
        WM_DWMCOMPOSITIONCHANGED = 0x031E,
        WM_DWMNCRENDERINGCHANGED = 0x031F,
        WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320,
        WM_DWMWINDOWMAXIMIZEDCHANGE = 0x0321,
        WM_DWMSENDICONICTHUMBNAIL = 0x0323,
        WM_DWMSENDICONICLIVEPREVIEWBITMAP = 0x0326,
        #endregion

        #region Device Management Messages
        /// <summary>
        /// Device Change
        /// <para>wParam <see cref="DeviceBroadcastType"/></para>
        /// <para>lParm <see cref="DEV_BROADCAST_HDR"/></para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/devio/wm-devicechange </para>
        /// </summary>
        WM_DEVICECHANGE = 0x219,
        #endregion

        #region Dialog Box Notifications
        /// <summary>
        ///当一个对话框控件将要被绘制前发送此消息给它的父窗口通过响应这条消息，所有者窗口可以通过使用给定的相关显示设备的句柄来设置对话框的文本背景颜色
        /// </summary>
        WM_CTLCOLORDLG = 0x136,
        /// <summary>
        ///当一个模态对话框或菜单进入空载状态时发送此消息给它的所有者，一个模态对话框或菜单进入空载状态就是在处理完一条或几条先前的消息后没有消息它的列队中等待
        /// </summary>
        WM_ENTERIDLE = 0x121,
        /// <summary>
        ///发送此消息给某个与对话框程序关联的控件，widdows控制方位键和TAB键使输入进入此控件通过应
        /// </summary>
        WM_GETDLGCODE = 0x87,
        /// <summary>
        ///在一个对话框程序被显示前发送此消息给它，通常用此消息初始化控件和执行其它任务
        /// </summary>
        WM_INITDIALOG = 0x110,
        /// <summary>
        ///发送此消息给一个对话框程序去更改焦点位置
        /// </summary>
        WM_NEXTDLGCTL = 0x28,
        #endregion

        #region Dynamic Data Exchange Messages
        #endregion

        #region Dynamic Data Exchange Notifications
        #endregion

        #region Hook Notifications
        /// <summary>
        /// 用户取消应用程序的日记活动时发布到应用程序。该消息发布时带有NULL窗口句柄
        /// </summary>
        WM_CANCELJOURNAL = 0x004B,
        /// <summary>
        ///此消息由基于计算机的训练程序发送，通过 <see cref="WH_JOURNALPALYBACK"/> 的 hook 程序分离出用户输入消息
        /// </summary>
        WM_QUEUESYNC = 0x23,
        #endregion

        #region Keyboard Accelerator Messages
        WM_CHANGEUISTATE = 0x0127,
        /// <summary>
        ///当一个菜单将要被激活时发送此消息，它发生在用户菜单条中的某项或按下某个菜单键，它允许程序在显示前更改菜单
        /// </summary>
        WM_INITMENU = 0x116,
        WM_QUERYUISTATE = 0x0129,
        WM_UPDATEUISTATE = 0x0128,
        #endregion

        #region Keyboard Accelerator Notifications
        /// <summary>
        ///当一个下拉菜单或子菜单将要被激活时发送此消息，它允许程序在它显示前更改菜单，而不要改变全部
        /// </summary>
        WM_INITMENUPOPUP = 0x117,
        /// <summary>
        ///当用户选择一条菜单项时发送此消息给菜单的所有者（一般是窗口）
        /// </summary>
        WM_MENUSELECT = 0x11F,
        /// <summary>
        ///当菜单已被激活用户按下了某个键（不同于加速键），发送此消息给菜单的所有者
        /// </summary>
        WM_MENUCHAR = 0x120,
        /// <summary>
        /// 当WM_SYSKEYDOWN消息被TRANSLATEMESSAGE函数翻译后提交此消息给拥有焦点的窗口
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/keyboard-input-notifications </para>
        /// </summary>
        WM_SYSCHAR = 0x106,
        /// <summary>
        ///当用户选择窗口菜单的一条命令或///当用户选择最大化或最小化时那个窗口会收到此消息
        /// </summary>
        WM_SYSCOMMAND = 0x112,
        #endregion

        #region Keyboard Input Messages
        /// <summary>
        ///应用程序发送此消息让一个窗口与一个热键相关连
        /// </summary>
        WM_SETHOTKEY = 0x32,
        /// <summary>
        ///应用程序发送此消息来判断热键与某个窗口是否有关联
        /// </summary>
        WM_GETHOTKEY = 0x33,
        #endregion

        #region Keyboard Input Notifications
        /// <summary>
        /// 一个窗口被激活或失去激活状态
        /// </summary>
        WM_ACTIVATE = 0x06,
        WM_APPCOMMAND = 0x0319,
        /// <summary>
        /// 按下某键，并已发出WM_KEYDOWN， WM_KEYUP消息
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/keyboard-input-notifications </para>
        /// </summary>
        WM_CHAR = 0x102,
        /// <summary>
        /// 当用translatemessage函数翻译WM_KEYUP消息时发送此消息给拥有焦点的窗口
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/keyboard-input-notifications </para>
        /// </summary>
        WM_DEADCHAR = 0x103,
        /// <summary>
        /// Hot Key, WM_HOTKEY 与 <see cref="WM_GETHOTKEY"/> 和 <see cref="WM_SETHOTKEY"/> 热键无关。该 WM_HOTKEY 消息被用于通用的热键发送而 WM_SETHOTKEY 和 WM_GETHOTKEY 消息涉及窗口激活热键。
        /// <para>lParam <see cref="RhkModifier"/></para>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/keyboard-input-notifications </para>
        /// </summary>
        WM_HOTKEY = 0x0312,
        /// <summary>
        /// WM_KEYDOWN 按下一个键
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/keyboard-input-notifications </para>
        /// </summary>
        WM_KEYDOWN = 0x0100,
        /// <summary>
        /// 释放一个键
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/keyboard-input-notifications </para>
        /// </summary>
        WM_KEYUP = 0x0101,
        /// <summary>
        /// 一个窗口失去焦点
        /// </summary>
        WM_KILLFOCUS = 0x08,
        /// <summary>
        /// 一个窗口获得焦点
        /// </summary>
        WM_SETFOCUS = 0x07,
        /// <summary>
        /// 当WM_SYSKEYDOWN消息被TRANSLATEMESSAGE函数翻译后发送此消息给拥有焦点的窗口
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/keyboard-input-notifications </para>
        /// </summary>
        WM_SYSDEADCHAR = 0x107,
        /// <summary>
        /// 当用户按住ALT键同时按下其它键时提交此消息给拥有焦点的窗口
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/keyboard-input-notifications </para>
        /// </summary>
        WM_SYSKEYDOWN = 0x104,
        /// <summary>
        /// 当用户释放一个键同时ALT 键还按着时提交此消息给拥有焦点的窗口
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/keyboard-input-notifications </para>
        /// </summary>
        WM_SYSKEYUP = 0x105,

        WM_UNICHAR = 0x0109,
        #endregion

        #region Menu Notifications
        /// <summary>
        ///当用户选择一条菜单命令项或当某个控件发送一条消息给它的父窗口，一个快捷键被翻译
        /// </summary>
        WM_COMMAND = 0x111,
        /// <summary>
        /// 当用户某个窗口中点击了一下右键就发送此消息给这个窗口
        /// </summary>
        WM_CONTEXTMENU = 0x7B,
        WM_ENTERMENULOOP = 0x0211,
        WM_EXITMENULOOP = 0x0212,
        WM_GETTITLEBARINFOEX = 0x033F,
        WM_MENUCOMMAND = 0x0126,
        WM_MENUDRAG = 0x0123,
        WM_MENUGETOBJECT = 0x0124,
        WM_MENURBUTTONUP = 0x0122,
        WM_NEXTMENU = 0x0213,
        WM_UNINITMENUPOPUP = 0x0125,
        #endregion

        #region Mouse Input Notifications
        /// <summary>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications </para>
        /// </summary>
        WM_CAPTURECHANGED = 0x0215,
        /// <summary>
        /// 双击鼠标左键
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications </para>
        /// </summary>
        WM_LBUTTONDBLCLK = 0x203,
        /// <summary>
        /// 按下鼠标左键
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications </para>
        /// </summary>
        WM_LBUTTONDOWN = 0x201,
        /// <summary>
        /// 释放鼠标左键
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications </para>
        /// </summary>
        WM_LBUTTONUP = 0x202,
        /// <summary>
        /// 按下鼠标中键
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications </para>
        /// </summary>
        WM_MBUTTONDOWN = 0x207,
        /// <summary>
        /// 释放鼠标中键
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications </para>
        /// </summary>
        WM_MBUTTONUP = 0x208,
        /// <summary>
        /// 双击鼠标中键
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications </para>
        /// </summary>
        WM_MBUTTONDBLCLK = 0x209,
        /// <summary>
        ///当光标在某个非激活的窗口中而用户正按着鼠标的某个键发送此消息给///当前窗口
        /// </summary>
        WM_MOUSEACTIVATE = 0x21,
        WM_MOUSEHOVER = 0x02A1,
        /// <summary>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications </para>
        /// </summary>
        WM_MOUSEHWHEEL = 0x020E,
        WM_MOUSELEAVE = 0x02A3,
        /// <summary>
        /// 移动鼠标时发生，同WM_MOUSEFIRST
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications </para>
        /// </summary>
        WM_MOUSEMOVE = 0x200,
        /// <summary>
        /// 当鼠标轮子转动时发送此消息个当前有焦点的控件
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications </para>
        /// </summary>
        WM_MOUSEWHEEL = 0x20A,
        /// <summary>
        ///移动鼠标，按住或释放鼠标时发生
        /// </summary>
        WM_NCHITTEST = 0x84,
        /// <summary>
        ///当光标在一个窗口的非客户区同时按下鼠标左键时提交此消息
        /// </summary>
        WM_NCLBUTTONDOWN = 0xA1,
        /// <summary>
        ///当用户释放鼠标左键同时光标某个窗口在非客户区十发送此消息
        /// </summary>
        WM_NCLBUTTONUP = 0xA2,
        /// <summary>
        ///当用户双击鼠标左键同时光标某个窗口在非客户区十发送此消息
        /// </summary>
        WM_NCLBUTTONDBLCLK = 0xA3,
        /// <summary>
        ///当光标在一个窗口的非客户区内移动时发送此消息给这个窗口 非客户区为：窗体的标题栏及窗 的边框体
        /// </summary>
        WM_NCMOUSEMOVE = 0xA0,
        WM_NCMOUSEHOVER = 0x02A0,
        WM_NCMOUSELEAVE = 0x02A2,
        /// <summary>
        ///当用户按下鼠标中键同时光标又在窗口的非客户区时发送此消息
        /// </summary>
        WM_NCMBUTTONDOWN = 0xA7,
        /// <summary>
        ///当用户释放鼠标中键同时光标又在窗口的非客户区时发送此消息
        /// </summary>
        WM_NCMBUTTONUP = 0xA8,
        /// <summary>
        ///当用户双击鼠标中键同时光标又在窗口的非客户区时发送此消息
        /// </summary>
        WM_NCMBUTTONDBLCLK = 0xA9,
        /// <summary>
        ///当用户按下鼠标右键同时光标又在窗口的非客户区时发送此消息
        /// </summary>
        WM_NCRBUTTONDOWN = 0xA4,
        /// <summary>
        ///当用户释放鼠标右键同时光标又在窗口的非客户区时发送此消息
        /// </summary>
        WM_NCRBUTTONUP = 0xA5,
        /// <summary>
        ///当用户双击鼠标右键同时光标某个窗口在非客户区十发送此消息
        /// </summary>
        WM_NCRBUTTONDBLCLK = 0xA6,

        WM_NCXBUTTONDOWN = 0xAB,
        WM_NCXBUTTONUP = 0xAC,
        WM_NCXBUTTONDBLCLK = 0xAD,
        /// <summary>
        /// 按下鼠标右键
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications </para>
        /// </summary>
        WM_RBUTTONDOWN = 0x204,
        /// <summary>
        /// 释放鼠标右键
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications </para>
        /// </summary>
        WM_RBUTTONUP = 0x205,
        /// <summary>
        /// 双击鼠标右键
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications </para>
        /// </summary>
        WM_RBUTTONDBLCLK = 0x206,
        /// <summary>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications </para>
        /// </summary>
        WM_XBUTTONDOWN = 0x020B,
        /// <summary>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications </para>
        /// </summary>
        WM_XBUTTONUP = 0x020C,
        /// <summary>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications </para>
        /// </summary>
        WM_XBUTTONDBLCLK = 0x020D,
        #endregion

        #region Multiple Document Interface Messages
        WM_MDICREATE = 0x0220,
        WM_MDIDESTROY = 0x0221,
        WM_MDIACTIVATE = 0x0222,
        WM_MDIRESTORE = 0x0223,
        WM_MDINEXT = 0x0224,
        WM_MDIMAXIMIZE = 0x0225,
        WM_MDITILE = 0x0226,
        WM_MDICASCADE = 0x0227,
        WM_MDIICONARRANGE = 0x0228,
        WM_MDIGETACTIVE = 0x0229,
        WM_MDISETMENU = 0x0230,
        WM_MDIREFRESHMENU = 0x0234,
        #endregion

        #region Raw Input Notifications
        /// <summary>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/raw-input-notifications </para>
        /// </summary>
        WM_INPUT_DEVICE_CHANGE = 0xFE,
        /// <summary>
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/inputdev/raw-input-notifications </para>
        /// </summary>
        WM_INPUT = 0xFF,
        #endregion

        #region Scroll Bar Notifications
        /// <summary>
        ///当一个滚动条控件将要被绘制时发送此消息给它的父窗口通过响应这条消息，所有者窗口可以通过使用给定的相关显示设备的句柄来设置滚动条的背景颜色
        /// </summary>
        WM_CTLCOLORSCROLLBAR = 0x137,
        /// <summary>
        ///当一个窗口标准水平滚动条产生一个滚动事件时发送此消息给那个窗口，也发送给拥有它的控件
        /// </summary>
        WM_HSCROLL = 0x114,
        /// <summary>
        ///当一个窗口标准垂直滚动条产生一个滚动事件时发送此消息给那个窗口也，发送给拥有它的控件
        /// </summary>
        WM_VSCROLL = 0x115,
        #endregion

        #region Timer Notifications
        /// <summary>
        ///发生了定时器事件
        /// </summary>
        WM_TIMER = 0x113,
        #endregion

        #region Window Messages
        /// <summary>
        /// get h menu
        /// </summary>
        MN_GETHMENU = 0x1E1,
        /// <summary>
        ///当窗口背景必须被擦除时（例在窗口改变大小时）
        /// </summary>
        WM_ERASEBKGND = 0x14,
        /// <summary>
        ///应用程序发送此消息得到当前控件绘制文本的字体
        /// </summary>
        WM_GETFONT = 0x31,
        /// <summary>
        ///应用程序发送此消息来设置一个窗口的文本
        /// </summary>
        WM_SETTEXT = 0x0C,
        /// <summary>
        ///应用程序发送此消息来复制对应窗口的文本到缓冲区
        /// </summary>
        WM_GETTEXT = 0x0D,
        /// <summary>
        ///得到与一个窗口有关的文本的长度（不包含空字符）
        /// </summary>
        WM_GETTEXTLENGTH = 0x0E,
        /// <summary>
        ///当绘制文本时程序发送此消息得到控件要用的颜色
        /// </summary>
        WM_SETFONT = 0x30,
        /// <summary>
        ///程序发送此消息让一个新的大图标或小图标与某个窗口关联
        /// </summary>
        WM_SETICON = 0x80,
        #endregion

        #region Window Notifications
        /// <summary>
        ///发此消息给应用程序哪个窗口是激活的，哪个是非激活的
        /// </summary>
        WM_ACTIVATEAPP = 0x1C,
        /// <summary>
        ///发送此消息来取消某种正在进行的摸态（操作）
        /// </summary>
        WM_CANCELMODE = 0x1F,
        /// <summary>
        ///发送此消息给MDI子窗口///当用户点击此窗口的标题栏，或///当窗口被激活，移动，改变大小
        /// </summary>
        WM_CHILDACTIVATE = 0x22,
        /// <summary>
        ///当一个窗口或应用程序要关闭时发送一个信号
        /// </summary>
        WM_CLOSE = 0x10,
        /// <summary>
        ///显示内存已经很少了
        /// </summary>
        WM_COMPACTING = 0x41,
        /// <summary>
        /// 创建一个窗口
        /// </summary>
        WM_CREATE = 0x01,
        /// <summary>
        /// 当一个窗口被破坏时发送
        /// </summary>
        WM_DESTROY = 0x02,
        WM_DPICHANGED = 0x02E0,
        /// <summary>
        ///一个窗口改变成Enable状态
        /// </summary>
        WM_ENABLE = 0x0A,
        WM_ENTERSIZEMOVE = 0x0231,
        WM_EXITSIZEMOVE = 0x0232,
        /// <summary>
        ///此消息发送给某个窗口来返回与某个窗口有关连的大图标或小图标的句柄
        /// </summary>
        WM_GETICON = 0x7F,
        /// <summary>
        ///此消息发送给窗口当它将要改变大小或位置
        /// </summary>
        WM_GETMINMAXINFO = 0x24,
        /// <summary>
        ///当用户选择某种输入语言，或输入语言的热键改变
        /// </summary>
        WM_INPUTLANGCHANGEREQUEST = 0x50,
        /// <summary>
        ///当平台现场已经被改变后发送此消息给受影响的最顶级窗口
        /// </summary>
        WM_INPUTLANGCHANGE = 0x51,
        /// <summary>
        /// 在移动窗口后发送。
        /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/winmsg/wm-move </para>
        /// </summary>
        WM_MOVE = 0x03,
        WM_MOVING = 0x0216,
        /// <summary>
        ///此消息发送给某个窗口仅当它的非客户区需要被改变来显示是激活还是非激活状态
        /// </summary>
        WM_NCACTIVATE = 0x86,
        /// <summary>
        ///当某个窗口的客户区域必须被核算时发送此消息
        /// </summary>
        WM_NCCALCSIZE = 0x83,
        /// <summary>
        ///当某个窗口第一次被创建时，此消息在WM_CREATE消息发送前发送
        /// </summary>
        WM_NCCREATE = 0x81,
        /// <summary>
        ///此消息通知某个窗口，非客户区正在销毁
        /// </summary>
        WM_NCDESTROY = 0x82,
        /// <summary>
        /// null
        /// </summary>
        WM_NULL = 0x00,
        /// <summary>
        ///此消息发送给最小化窗口，当此窗口将要被拖放而它的类中没有定义图标，应用程序能返回一个图标或光标的句柄，当用户拖放图标时系统显示这个图标或光标
        /// </summary>
        WM_QUERYDRAGICON = 0x37,
        /// <summary>
        ///当用户窗口恢复以前的大小位置时，把此消息发送给某个图标
        /// </summary>
        WM_QUERYOPEN = 0x13,
        /// <summary>
        ///用来结束程序运行
        /// </summary>
        WM_QUIT = 0x12,
        /// <summary>
        ///当隐藏或显示窗口是发送此消息给这个窗口
        /// </summary>
        WM_SHOWWINDOW = 0x18,
        /// <summary>
        ///改变一个窗口的大小
        /// </summary>
        WM_SIZE = 0x05,
        WM_SIZING = 0x0214,
        /// <summary>
        ///当调用SETWINDOWLONG函数将要改变一个或多个 窗口的风格时发送此消息给那个窗口
        /// </summary>
        WM_STYLECHANGING = 0x7C,
        /// <summary>
        ///当调用SETWINDOWLONG函数一个或多个 窗口的风格后发送此消息给那个窗口
        /// </summary>
        WM_STYLECHANGED = 0x7D,
        WM_THEMECHANGED = 0x031A,
        /// <summary>
        ///当用户已经登入或退出后发送此消息给所有的窗口，///当用户登入或退出时系统更新用户的具体设置信息，在用户更新设置时系统马上发送此消息
        /// </summary>
        WM_USERCHANGED = 0x54,
        /// <summary>
        ///发送此消息给那个窗口的大小和位置已经被改变时，来调用 setwindowpos 函数或其它窗口管理函数
        /// </summary>
        WM_WINDOWPOSCHANGED = 0x47,
        /// <summary>
        ///发送此消息给那个窗口的大小和位置将要被改变时，来调用 setwindowpos 函数或其它窗口管理函数
        /// </summary>
        WM_WINDOWPOSCHANGING = 0x46,
        #endregion
        #endregion

        #region Message Constants
        /// <summary>
        /// user
        /// </summary>
        WM_USER = 0x0400,
        /// <summary>
        /// app
        /// </summary>
        WM_APP = 0x8000,
        #endregion

        #region Windows Touch Input Messages
        WM_TOUCH = 0x0240,
        #endregion

        #region Painting and Drawing Messages
        /// <summary>
        ///当显示器的分辨率改变后发送此消息给所有的窗口
        /// </summary>
        WM_DISPLAYCHANGE = 0x7E,
        /// <summary>
        ///程序发送此消息给某个窗口当它（窗口）的框架必须被绘制时
        /// </summary>
        WM_NCPAINT = 0x85,
        /// <summary>
        ///要求一个窗口重画自己
        /// </summary>
        WM_PAINT = 0x0F,
        WM_PRINT = 0x0317,
        WM_PRINTCLIENT = 0x0318,
        /// <summary>
        ///设置窗口是否能重画
        /// </summary>
        WM_SETREDRAW = 0x0B,
        /// <summary>
        /// sys cpaint
        /// </summary>
        WM_SYNCPAINT = 0x88,
        #endregion

        #region System Shutdown Messages
        /// <summary>
        ///当系统进程发出 <see cref="WM_QUERYENDSESSION"/> 消息后，此消息发送给应用程序，通知它对话是否结束
        /// </summary>
        WM_ENDSESSION = 0x16,
        /// <summary>
        ///当用户选择结束对话框或程序自己调用 ExitWindows 函数
        /// </summary>
        WM_QUERYENDSESSION = 0x11,
        #endregion

        #region Pointer Input Messages        
        WM_POINTERDEVICECHANGE = 0x238,
        WM_POINTERDEVICEINRANGE = 0x239,
        WM_POINTERDEVICEOUTOFRANGE = 0x23A,
        WM_NCPOINTERUPDATE = 0x0241,
        WM_NCPOINTERDOWN = 0x0242,
        WM_NCPOINTERUP = 0x0243,
        WM_POINTERUPDATE = 0x0245,
        WM_POINTERDOWN = 0x0246,
        WM_POINTERUP = 0x0247,
        WM_POINTERENTER = 0x0249,
        WM_POINTERLEAVE = 0x024A,
        WM_POINTERACTIVATE = 0x024B,
        WM_POINTERCAPTURECHANGED = 0x024C,
        WM_TOUCHHITTESTING = 0x024D,
        WM_POINTERWHEEL = 0x024E,
        WM_POINTERHWHEEL = 0x024F,
        DM_POINTERHITTEST = 0x0250,
        WM_POINTERROUTEDTO = 0x0251,
        WM_POINTERROUTEDAWAY = 0x0252,
        WM_POINTERROUTEDRELEASED = 0x0253,
        #endregion

        #region Input Method Manager Messages
        WM_IME_SETCONTEXT = 0x0281,
        WM_IME_NOTIFY = 0x0282,
        WM_IME_CONTROL = 0x0283,
        WM_IME_COMPOSITIONFULL = 0x0284,
        WM_IME_SELECT = 0x0285,
        WM_IME_CHAR = 0x0286,
        WM_IME_REQUEST = 0x0288,
        WM_IME_KEYDOWN = 0x0290,
        WM_IME_KEYUP = 0x0291,
        #endregion

        /// <summary>
        ///当系统颜色改变时，发送此消息给所有顶级窗口
        /// </summary>
        WM_SYSCOLORCHANGE = 0x15,
        /// <summary>
        /// win min change
        /// </summary>
        WM_WININICHANGE = 0x1A,
        /// <summary>
        /// dev mode change
        /// </summary>
        WM_DEVMODECHANGE = 0x1B,
        
        /// <summary>
        ///当系统的字体资源库变化时发送此消息给所有顶级窗口
        /// </summary>
        WM_FONTCHANGE = 0x1D,
        /// <summary>
        ///当系统的时间变化时发送此消息给所有顶级窗口
        /// </summary>
        WM_TIMECHANGE = 0x1E,
        
        /// <summary>
        ///发送给最小化窗口当它图标将要被重画
        /// </summary>
        WM_PAINTICON = 0x26,
        /// <summary>
        ///此消息发送给某个最小化窗口，仅///当它在画图标前它的背景必须被重画
        /// </summary>
        WM_ICONERASEBKGND = 0x27,
        
        /// <summary>
        ///每当打印管理列队增加或减少一条作业时发出此消息
        /// </summary>
        WM_SPOOLERSTATUS = 0x2A,
        /// <summary>
        ///当button，combobox，listbox，menu的可视外观改变时发送
        /// </summary>
        WM_DRAWITEM = 0x2B,
        /// <summary>
        ///当button, combo box, list box, list view control, or menu item 被创建时
        /// </summary>
        WM_MEASUREITEM = 0x2C,
        /// <summary>
        /// delete item
        /// </summary>
        WM_DELETEITEM = 0x2D,
        /// <summary>
        ///此消息有一个LBS_WANTKEYBOARDINPUT风格的发出给它的所有者来响应WM_KEYDOWN消息
        /// </summary>
        WM_VKEYTOITEM = 0x2E,
        /// <summary>
        ///此消息由一个LBS_WANTKEYBOARDINPUT风格的列表框发送给他的所有者来响应WM_CHAR消息
        /// </summary>
        WM_CHARTOITEM = 0x2F, 

        /// <summary>
        ///发送此消息来判定combobox或listbox新增加的项的相对位置
        /// </summary>
        WM_COMPAREITEM = 0x39,
        /// <summary>
        /// get object
        /// </summary>
        WM_GETOBJECT = 0x3D,
        
        /// <summary>
        /// comm notify
        /// </summary>
        WM_COMMNOTIFY = 0x44,        
        /// <summary>
        ///当系统将要进入暂停状态时发送此消息
        /// </summary>
        WM_POWER = 0x48,
        
        /// <summary>
        ///当某个控件的某个事件已经发生或这个控件需要得到一些信息时，发送此消息给它的父窗口
        /// </summary>
        WM_NOTIFY = 0x4E,
        
        /// <summary>
        ///当程序已经初始化windows帮助例程时发送此消息给应用程序
        /// </summary>
        WM_TCARD = 0x52,
        /// <summary>
        ///此消息显示用户按下了F1，如果某个菜单是激活的，就发送此消息个此窗口关联的菜单，否则就发送给有焦点的窗口，如果///当前都没有焦点，就把此消息发送给///当前激活的窗口
        /// </summary>
        WM_HELP = 0x53,
        
        /// <summary>
        /// 公用控件，自定义控件和他们的父窗口通过此消息来判断控件是使用ANSI还是UNICODE结构
        /// </summary>
        WM_NOTIFYFORMAT = 0x55,        
 
        
        //WM_KEYLAST = 0x108,        
        //WM_KEYLAST      =                0x0109,

        
        /// <summary>=
        ///在windows绘制消息框前发送此消息给消息框的所有者窗口，通过响应这条消息，所有者窗口可以通过使用给定的相关显示设备的句柄来设置消息框的文本和背景颜色
        /// </summary>
        WM_CTLCOLORMSGBOX = 0x132,
        /// <summary>
        ///当一个编辑型控件将要被绘制时发送此消息给它的父窗口通过响应这条消息，所有者窗口可以通过使用给定的相关显示设备的句柄来设置编辑框的文本和背景颜色
        /// </summary>
        WM_CTLCOLOREDIT = 0x133,
        /// <summary>
        ///当一个列表框控件将要被绘制前发送此消息给它的父窗口通过响应这条消息，所有者窗口可以通过使用给定的相关显示设备的句柄来设置列表框的文本和背景颜色
        /// </summary>
        WM_CTLCOLORLISTBOX = 0x134,
        /// <summary>
        ///当一个按钮控件将要被绘制时发送此消息给它的父窗口通过响应这条消息，所有者窗口可以通过使用给定的相关显示设备的句柄来设置按纽的文本和背景颜色
        /// </summary>
        WM_CTLCOLORBTN = 0x135, 
        /// <summary>
        ///当一个静态控件将要被绘制时发送此消息给它的父窗口通过响应这条消息，所有者窗口可以 通过使用给定的相关显示设备的句柄来设置静态控件的文本和背景颜色
        /// </summary>
        WM_CTLCOLORSTATIC = 0x138,       

        WM_PARENTNOTIFY = 0x0210,        
        WM_POWERBROADCAST = 0x0218,
        WM_DROPFILES = 0x0233,
       
        WM_WTSSESSION_CHANGE = 0x02B1,

        WM_TABLET_FIRST = 0x02c0,
        WM_TABLET_LAST = 0x02df,
        
        WM_DPICHANGED_BEFOREPARENT = 0x02E2,
        WM_DPICHANGED_AFTERPARENT = 0x02E3,
        WM_GETDPISCALEDSIZE = 0x02E4,

        WM_QUERYNEWPALETTE = 0x030F,
        WM_PALETTEISCHANGING = 0x0310,
        WM_PALETTECHANGED = 0x0311,
        
        WM_HANDHELDFIRST = 0x0358,
        WM_HANDHELDLAST = 0x035F,
        WM_AFXFIRST = 0x0360,
        WM_AFXLAST = 0x037F,
        WM_PENWINFIRST = 0x0380,
        WM_PENWINLAST = 0x038F,        
    }
    #endregion


}
