using System;
using System.Runtime.InteropServices;

/***
 * 
 * 指针输入消息和通知 Pointer Input Messages and Notifications
 * 参考：https://docs.microsoft.com/zh-cn/windows/win32/api/_inputmsg/ 
 * 
 * 
**/

namespace SpaceCG.WindowsAPI.User32
{
    #region Enumerations
    /// <summary>
    /// Touch 反馈模式
    /// <para> <see cref="User32.InitializeTouchInjection"/>  函数参数 dwMode 的值之一</para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/previous-versions/windows/desktop/input_touchinjection/constants </para>
    /// </summary>
    public enum TouchFeedbackMode:uint
    {
        /// <summary>
        /// 指定默认的触摸可视化。最终的用户在 Pen and Touch 控制面板中的设置可能会抑制注入的触摸反馈。
        /// </summary>
        DEFAULT = 0x1,
        /// <summary>
        /// 指定间接触摸可视化。注入的触摸反馈将覆盖“笔和触摸”控制面板中的最终用户设置。
        /// </summary>
        INDIRECT = 0x2,
        /// <summary>
        /// 指定没有触摸可视化。TOUCH_FEEDBACK_INDIRECT | TOUCH_FEEDBACK_NONE 应用程序和控件提供的触摸反馈可能不会受到影响。
        /// </summary>
        NONE = 0x3,
    }

    /// <summary>
    /// 指针输入类型。
    /// <para> <see cref="POINTER_INFO"/> 结构体字段 <see cref="POINTER_INFO.pointerType"/> 的值之一 </para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ne-winuser-tagpointer_input_type </para>
    /// </summary>
    public enum PointerInputType: uint
    {
        /// <summary>
        /// 通用指针类型。此类型永远不会出现在指针消息或指针数据中。一些数据查询功能允许调用者将查询限制为特定的指针类型。所述 PT_POINTER 类型可以在这些功能被用来指定该查询是包括所有类型的指针
        /// </summary>
        PT_POINTER = 1,
        /// <summary>
        /// 触摸指针类型。
        /// </summary>
        PT_TOUCH = 2,
        /// <summary>
        /// 笔指针类型。
        /// </summary>
        PT_PEN = 3,
        /// <summary>
        /// 鼠标指针类型。
        /// </summary>
        PT_MOUSE = 4,
        /// <summary>
        /// 触摸板指针类型（Windows 8.1和更高版本）。
        /// </summary>
        PT_TOUCHPAD = 5
    };

    /// <summary>
    /// <see cref="POINTER_INFO"/> 结构体字段 <see cref="POINTER_INFO.pointerFlags"/> 的值之一或值组合
    /// <para> XBUTTON1 和 XBUTTON2 是许多鼠标设备上使用的其他按钮。它们返回与标准鼠标按钮相同的数据。</para>
    /// <para>注入的输入将发送到运行注入过程的会话的桌面。有用于由以下组合所指示触摸输入注射（交互式和悬停）两个输入状态 pointerFlags ：</para>
    /// <para>INRANGE | UPDATE  Touch 触摸悬停开始或移动</para>
    /// <para>INRANGE | INCONTACT | DOWN    触摸向下</para>
    /// <para>INRANGE | INCONTACT | UPDATE  触摸接触动作</para>
    /// <para>INRANGE | UP  触摸向上并过渡到悬停</para>
    /// <para>UPDATE    触摸悬停结束</para>
    /// <para>UP 触摸结束</para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/previous-versions/windows/desktop/inputmsg/pointer-flags-contants </para>
    /// </summary>
    [Flags]
    public enum PointerFlags
    {
        /// <summary>
        /// 默认
        /// </summary>
        NONE = 0x00000000,
        /// <summary>
        /// 指示新指针的到达。
        /// </summary>
        NEW = 0x00000001,
        /// <summary>
        /// 指示此指针继续存在。如果未设置此标志，则表明指针已离开检测范围。
        /// <para>此标志通常不设置仅当指针悬停叶检测范围（UPDATE是组），或当在与窗口面叶片的检测范围相接触的指针（UP是集）。</para>
        /// </summary>
        INRANGE = 0x00000002,
        /// <summary>
        /// 指示该指针与数字转换器表面接触。未设置此标志时，表示悬停指针。
        /// </summary>
        INCONTACT = 0x00000004,
        /// <summary>
        /// 指示主要操作，类似于鼠标左键按下。触摸指针与数字化仪表面接触时会设置此标志。
        /// <para>笔指针在未按下任何按钮的情况下与数字化仪表面接触时，会设置此标志。当鼠标左键按下时，鼠标指针将设置此标志。</para>
        /// </summary>
        FIRSTBUTTON = 0x00000010,
        /// <summary>
        /// 指示辅助操作，类似于鼠标右键按下。触摸指针不使用此标志。
        /// <para>当笔筒按钮按下时，笔指针与数字转换器表面接触时会设置此标志。当鼠标右键按下时，鼠标指针会设置此标志。</para>
        /// </summary>
        SECONDBUTTON = 0x00000020,
        /// <summary>
        /// 类似于按下鼠标滚轮的按钮。触摸指针不使用此标志。
        /// <para>笔指针不使用此标志。按下鼠标滚轮按钮时，鼠标指针会设置此标志。</para>
        /// </summary>
        THIRDBUTTON = 0x00000040,
        /// <summary>
        /// 类似于第一个扩展鼠标（XButton1）按下按钮。触摸指针不使用此标志。
        /// <para>笔指针不使用此标志。当第一个扩展鼠标（XBUTTON1）按钮按下时，鼠标指针将设置此标志。</para>
        /// </summary>
        FOURTHBUTTON = 0x00000080,
        /// <summary>
        /// 类似于按下第二个扩展鼠标（XButton2）的按钮。触摸指针不使用此标志。
        /// <para>笔指针不使用此标志。当第二个扩展鼠标（XBUTTON2）按钮按下时，鼠标指针将设置此标志。</para>
        /// </summary>
        FIFTHBUTTON = 0x00000100,
        /// <summary>
        /// 指示该指针已被指定为主指针。主指针是一个单一的指针，它可以执行超出非主指针可用的动作的动作。例如，当主指针与窗口的表面接触时，它可以通过向其发送WM_POINTERACTIVATE消息来为窗口提供激活机会。
        /// <para>根据系统上所有当前用户的交互（鼠标，触摸，笔等）来标识主指针。因此，主指针可能未与您的应用程序关联。多点触摸交互中的第一个联系人被设置为主指针。一旦标识了主要指针，则必须先提起所有联系人，然后才能将新的联系人标识为主要指针。对于不处理指针输入的应用程序，只有主指针的事件被提升为鼠标事件。</para>
        /// </summary>
        PRIMARY = 0x00002000,
        /// <summary>
        /// 置信度是来自源设备的关于指针是表示预期交互还是意外交互的建议，这尤其与PT_TOUCH指针有关，在PT_TOUCH指针中，意外交互（例如用手掌）可以触发输入。此标志的存在指示源设备对该输入是预期交互的一部分具有高置信度。
        /// </summary>
        CONFIDENCE = 0x000004000,
        /// <summary>
        /// 指示指针以异常方式离开，例如，当系统收到该指针的无效输入或具有活动指针的设备突然离开时。如果接收输入的应用程序可以这样做，则应将交互视为未完成，并撤销相关指针的任何影响。
        /// </summary>
        CANCELED = 0x000008000,
        /// <summary>
        /// 指示该指针已转换为向下状态；也就是说，它与数字转换器表面接触。
        /// </summary>
        DOWN = 0x00010000,
        /// <summary>
        /// 表示这是一个简单的更新，不包括指针状态更改。
        /// </summary>
        UPDATE = 0x00020000,
        /// <summary>
        /// 指示该指针已转换为向上状态；也就是说，与数字转换器表面的接触结束了。
        /// </summary>
        UP = 0x00040000,
        /// <summary>
        /// 指示与指针轮相关的输入。对于鼠标指针，这等效于鼠标滚轮（WM_MOUSEHWHEEL）的操作。
        /// </summary>
        WHEEL = 0x00080000,
        /// <summary>
        /// 指示与指针h轮相关联的输入。对于鼠标指针，这等效于鼠标水平滚动轮（WM_MOUSEHWHEEL）的操作。
        /// </summary>
        HWHEEL = 0x00100000,
        /// <summary>
        /// 指示此指针已被另一个元素捕获（关联），并且原始元素丢失了捕获（请参见WM_POINTERCAPTURECHANGED）。
        /// </summary>
        CAPTURECHANGED = 0x00200000,
        /// <summary>
        /// 指示此指针具有关联的转换。
        /// </summary>
        HASTRANSFORM = 0x00400000,
    }

    /// <summary>
    /// <see cref="POINTER_INFO"/> 结构体字段 <see cref="POINTER_INFO.buttonChangeType"/> 的值之一
    /// <para>标识与指针关联的按钮状态的变化 <see cref="PointerFlags"/></para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ne-winuser-pointer_button_change_type </para>
    /// </summary>
    public enum PointerButtonChangeType
    {
        /// <summary>
        /// 按钮状态无变化。
        /// </summary>
        NONE,
        /// <summary>
        /// 第一个按钮转换为按下状态。
        /// </summary>
        FIRSTBUTTON_DOWN,
        /// <summary>
        /// 第一个按钮转换为释放状态。
        /// </summary>
        FIRSTBUTTON_UP,
        /// <summary>
        /// 第二个按钮转换为按下状态。
        /// </summary>
        SECONDBUTTON_DOWN,
        /// <summary>
        /// 第二个按钮转换为释放状态。
        /// </summary>
        SECONDBUTTON_UP,
        /// <summary>
        /// 第三个按钮转换为按下状态。
        /// </summary>
        THIRDBUTTON_DOWN,
        /// <summary>
        /// 第三个按钮转换为释放状态。
        /// </summary>
        THIRDBUTTON_UP,
        /// <summary>
        /// 第四个按钮转换为按下状态。
        /// </summary>
        FOURTHBUTTON_DOWN,
        /// <summary>
        /// 第四个按钮转换为释放状态。
        /// </summary>
        FOURTHBUTTON_UP,
        /// <summary>
        /// 第五个按钮转换为按下状态。
        /// </summary>
        FIFTHBUTTON_DOWN,
        /// <summary>
        /// 第五个按钮转换为释放状态。
        /// </summary>
        FIFTHBUTTON_UP
    }

    /// <summary>
    /// <see cref="POINTERTOUCHINFO"/> 结构体字段 <see cref="POINTERTOUCHINFO.touchFlags"/> 的值之一
    /// <para>参考：https://docs.microsoft.com/zh-cn/previous-versions/windows/desktop/inputmsg/touch-flags-constants </para>
    /// </summary>
    [Flags]
    public enum TouchFlags
    {
        /// <summary>
        /// The default value.
        /// </summary>
        NONE = 0x00000000,
    }

    /// <summary>
    /// <see cref="POINTER_TOUCH_INFO"/> 结构体字段 <see cref="POINTER_TOUCH_INFO.touchMask"/> 的值之一或值组合
    /// <para>参考：https://docs.microsoft.com/zh-cn/previous-versions/windows/desktop/inputmsg/touch-mask-constants </para>
    /// </summary>
    [Flags]
    public enum TouchMask
    {
        /// <summary>
        /// 默认。所有可选字段均无效。
        /// </summary>
        NONE = 0x00000000,
        /// <summary>
        /// 关系
        /// </summary>
        CONTACTAREA = 0x00000001,
        /// <summary>
        /// 方向
        /// </summary>
        ORIENTATION = 0x00000002,
        /// <summary>
        /// 压力
        /// </summary>
        PRESSURE = 0x00000004,
    }

    /// <summary>
    /// 结构体 <see cref="TOUCHINPUT"/> 属性 dwFlags 的值之一或值组合
    /// <para> 如果计算机上的目标硬件不支持悬停，则当设置 <see cref="TOUCHEVENTF_UP"/> 标志时，将清除 <see cref="TOUCHEVENTF_INRANGE"/>  标志。如果计算机上的目标硬件支持悬停，则将分别设置 TOUCHEVENTF_UP 和 TOUCHEVENTF_INRANGE 标志</para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-touchinput </para>
    /// </summary>
    [Flags]
    public enum TouchEventFlags
    {
        /// <summary>
        /// 发生了移动。不能与 <see cref="TOUCHEVENTF_DOWN"/> 结合使用。
        /// </summary>
        TOUCHEVENTF_MOVE = 0x0001,
        /// <summary>
        /// 通过新的联系人建立了相应的接触点。不能与 <see cref="TOUCHEVENTF_MOVE"/> 或 <see cref="TOUCHEVENTF_UP"/> 结合使用。
        /// </summary>
        TOUCHEVENTF_DOWN = 0x0002,
        /// <summary>
        /// 触摸点已删除。
        /// </summary>
        TOUCHEVENTF_UP = 0x0004,
        /// <summary>
        /// 接触点在范围内。此标志用于在兼容硬件上启用触摸悬浮支持。不需要支持悬停的应用程序可以忽略此标志。
        /// </summary>
        TOUCHEVENTF_INRANGE = 0x0008,
        /// <summary>
        /// 指示此 <see cref="TOUCHINPUT"/> 结构对应于主要接触点。有关主要接触点的更多信息，请参见以下文本。
        /// </summary>
        TOUCHEVENTF_PRIMARY = 0x0010,
        /// <summary>
        /// 使用 <see cref="GetTouchInputInfo"/> 接收时，此输入未合并。
        /// </summary>
        TOUCHEVENTF_NOCOALESCE = 0x0020,
        /// <summary>
        /// 触摸事件来自用户的手掌。
        /// </summary>
        TOUCHEVENTF_PALM = 0x0080,
    }

    /// <summary>
    /// 结构体 <see cref="TOUCHINPUT.dwMask"/> 的值之一或值组合
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-touchinput </para>
    /// </summary>
    [Flags]
    public enum TouchMaskFlags
    {
        /// <summary>
        /// cxContact 和 cyContact 有效。有关主要接触点的更多信息，请参见以下文本。
        /// </summary>
        TOUCHINPUTMASKF_CONTACTAREA = 0x0004,
        /// <summary>
        /// dwExtraInfo 有效。
        /// </summary>
        TOUCHINPUTMASKF_EXTRAINFO = 0x0002,
        /// <summary>
        /// 系统时间在 <see cref="TOUCHINPUT"/> 结构中设置。
        /// </summary>
        TOUCHINPUTMASKF_TIMEFROMSYSTEM = 0x0001,
    }
    #endregion


    #region Structures
    /// <summary>
    /// <see cref="POINTERTOUCHINFO"/> 结构体字段 <see cref="POINTERTOUCHINFO.pointerInfo"/> 的值
    /// <para>包含所有指针类型共有的基本指针信息。应用程序可以使用 <see cref="User32.GetPointerInfo"/>，<see cref="User32.GetPointerFrameInfo"/>，<see cref="User32.GetPointerInfoHistory"/> 和 <see cref="User32.GetPointerFrameInfoHistory"/> 函数检索此信息。</para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-pointer_info </para>
    /// </summary>
    public struct POINTER_INFO
    {
        /// <summary>
        /// <see cref="PointerInputType"/> 枚举中的一个值，它指定指针类型。
        /// </summary>
        public PointerInputType pointerType;
        /// <summary>
        /// 一个在其生存期内唯一标识指针的标识符。指针在首次检测到时就存在，而在超出检测范围时结束其存在。请注意，如果某个物理实体（手指或笔）超出了检测范围，然后又再次被检测到，则将其视为新的指针，并可以为其分配新的指针标识符。
        /// </summary>
        public uint pointerId;
        /// <summary>
        /// 源设备在单个输入帧中报告更新的多个指针共有的标识符。例如，并行模式多点触摸数字转换器可以在单次更新中向系统报告多个触摸触点的位置。
        /// </summary>
        public uint frameId;
        /// <summary>
        /// 可以是来自 <see cref="PointerFlags"/> 常量的标志的任何合理组合。
        /// </summary>
        public PointerFlags pointerFlags;
        /// <summary>
        /// 处理可用于原始输入设备 API 和数字转换器设备 API 调用的源设备。
        /// </summary>
        public IntPtr sourceDevice;
        /// <summary>
        /// 此消息所针对的窗口。如果通过与该窗口建立联系来隐式捕获指针，或者使用指针捕获 API 显式地捕获指针，则这就是捕获窗口。如果未捕获指针，则这是生成此消息时指针所在的窗口。
        /// </summary>
        public IntPtr hwndTarget;
        /// <summary>
        /// 指针的预测屏幕坐标，以像素为单位。
        /// </summary>
        public POINT ptPixelLocation;
        /// <summary>
        /// 针的预测屏幕坐标，以 HIMETRIC 单位。
        /// </summary>
        public POINT ptHimetricLocation;
        /// <summary>
        /// 指针的屏幕坐标，以像素为单位。有关调整的屏幕坐标，请参见 ptPixelLocation
        /// </summary>
        public POINT ptPixelLocationRaw;
        /// <summary>
        /// 指针的屏幕坐标，以 HIMETRIC 单位。有关调整的屏幕坐标，请参见 ptHimetricLocation。
        /// </summary>
        public POINT ptHimetricLocationRaw;
        /// <summary>
        /// 0或消息的时间戳，基于收到消息时的系统滴答计数。
        /// </summary>
        public uint dwTime;
        /// <summary>
        /// 合并到此消息中的输入计数。此计数与调用 <see cref="User32.GetPointerInfoHistory"/> 可以返回的条目总数相匹配。如果未发生合并，则对于消息表示的单个输入，此计数为1。
        /// </summary>
        public uint historyCount;
        /// <summary>
        /// InputData
        /// </summary>
        public int InputData;
        /// <summary>
        /// 指示在生成输入时按下了哪些键盘修饰键。可以为零或以下值的组合。
        /// POINTER_MOD_SHIFT –按下了SHIFT键。
        /// POINTER_MOD_CTRL –按下CTRL键。
        /// Pointer info key states defintions.
        /// #define POINTER_MOD_SHIFT   (0x0004)    // Shift key is held down.
        /// #define POINTER_MOD_CTRL    (0x0008)    // Ctrl key is held down.
        /// </summary>
        public uint dwKeyStates;
        /// <summary>
        /// 收到指针消息时的高分辨率性能计数器的值（高精度，64 位替代 dwTime）。当触摸数字化仪硬件在其输入报告中支持扫描时间戳信息时，可以校准该值。
        /// </summary>
        public ulong performanceCount;
        /// <summary>
        /// <see cref="PointerButtonChangeType"/> 枚举中的一个值，用于指定此输入与先前输入之间的按钮状态更改。
        /// </summary>
        public PointerButtonChangeType buttonChangeType;
    }

    /// <summary>
    /// 指针类型共有的基本触摸信息。
    /// <para><see cref="User32.InjectTouchInput"/> 函数参数 contacts 触摸数据集合</para>
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-pointer_touch_info </para>
    /// </summary>
    public struct POINTER_TOUCH_INFO
    {
        /// <summary>
        /// 嵌入式 <see cref="POINTER_INFO"/> 标头结构。
        /// </summary>
        public POINTER_INFO pointerInfo;
        /// <summary>
        /// 目前没有，为 0 。
        /// </summary>
        public TouchFlags touchFlags;
        /// <summary>
        /// 指示哪个可选字段包含有效值。该成员可以是零，也可以是“触摸蒙版”常量的值的任意组合。
        /// </summary>
        public TouchMask touchMask;
        /// <summary>
        /// 接触区域的预测屏幕坐标，以像素为单位。默认情况下，如果设备不报告接触区域，则此字段默认为以指针位置为中心的 0×0 矩形。
        /// <para>预测值基于数字化仪报告的指针位置和指针的运动。该校正可以补偿由于感测和处理数字化仪上的指针位置时固有的延迟而导致的视觉滞后。这适用于 PT_TOUCH 类型的指针。</para>
        /// </summary>
        public RECT rcContact;
        /// <summary>
        /// 接触区域的原始屏幕坐标，以像素为单位。有关调整的屏幕坐标，请参见 rcContact。
        /// </summary>
        public RECT rcContactRaw;
        /// <summary>
        /// 指针方向，其值介于0到359之间，其中 0 表示与 x 轴对齐并从左到右指向的触摸指针；增大的值表示沿顺时针方向的旋转度。
        /// <para>如果设备未报告方向，则此字段默认为 0。</para>
        /// </summary>
        public uint orientation;
        /// <summary>
        /// 笔压标准化为 0 到 1024 之间的范围。如果设备未报告压力，则默认值为 0。
        /// </summary>
        public uint pressure;
    }

    /// <summary>
    /// 封装用于触摸输入的数据。(TOUCHINPUT, *PTOUCHINPUT)
    /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/ns-winuser-touchinput </para>
    /// </summary>
    public struct TOUCHINPUT
    {
        /// <summary>
        /// 触摸输入的x坐标（水平点）。该成员以物理屏幕坐标的百分之一像素表示。
        /// </summary>
        public int x;
        /// <summary>
        /// 触摸输入的y坐标（垂直点）。该成员以物理屏幕坐标的百分之一像素表示。
        /// </summary>
        public int y;
        /// <summary>
        /// 源输入设备的设备句柄。触摸输入提供程序会在运行时为每个设备提供唯一的提供程序。
        /// </summary>
        public IntPtr hSource;
        /// <summary>
        /// 区分特定触摸输入的触摸点标识符。从接触点下降到恢复接触点，此值在触摸接触序列中保持一致。以后可以将 ID 重新用于后续联系人。
        /// </summary>
        public uint dwID;
        /// <summary>
        /// 一组位标志，用于指定触摸点按下，释放和运动的各个方面。
        /// </summary>
        public TouchEventFlags dwFlags;
        /// <summary>
        /// 一组位标志，用于指定结构中的哪些可选字段包含有效值。可选字段中有效信息的可用性是特定于设备的。仅当在 dwMask 中设置了相应的位时，应用程序才应使用可选的字段值。
        /// </summary>
        public TouchMaskFlags dwMask;
        /// <summary>
        /// 事件的时间戳，以毫秒为单位。消费应用程序应注意，系统不对此字段执行任何验证；当未设置 <see cref="TouchMaskFlags.TOUCHINPUTMASKF_TIMEFROMSYSTEM"/>  标志时，此字段中值的准确性和顺序完全取决于触摸输入提供程序。
        /// </summary>
        public uint dwTime;
        /// <summary>
        /// 与触摸事件关联的附加值。
        /// </summary>
        public IntPtr dwExtraInfo;
        /// <summary>
        /// 在物理屏幕坐标中，触摸接触区域的宽度以百分之一像素为单位。仅当 dwMask 成员设置了 <see cref="TouchMaskFlags.TOUCHEVENTFMASK_CONTACTAREA"/> 标志时，此值才有效。
        /// </summary>
        public uint cxContact;
        /// <summary>
        /// 在物理屏幕坐标中，触摸接触区域的高度以百分之一像素为单位。仅当 dwMask 成员设置了 <see cref="TouchMaskFlags.TOUCHEVENTFMASK_CONTACTAREA"/> 标志时，此值才有效。
        /// </summary>
        public uint cyContact;

        /// <summary>
        /// <see cref="TOUCHINPUT"/> 结构体大小，以字节为单位。
        /// </summary>
        public static readonly uint Size = (uint)Marshal.SizeOf(typeof(TOUCHINPUT));
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
        /// 最大同时触摸常量数,Specifies the maximum number of simultaneous contacts.
        /// </summary>
        public const uint MAX_TOUCH_COUNT = 256;

        /// <summary>
        /// 为调用的应用程序配置触摸注入上下文，并初始化该应用程序可以注入的最大同时 接触 数量。
        /// <para>注意：<see cref="InitializeTouchInjection"/> 必须在对 <see cref="InjectTouchInput"/> 的任何调用之前。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-initializetouchinjection </para>
        /// </summary>
        /// <param name="maxCount">触摸触点的最大数量。
        ///     <para>所述 maxCount 参数必须大于 0 且小于或等于 <see cref="MAX_TOUCH_COUNT"/>(256) 已在 winuser.h 定义。</para>
        /// </param>
        /// <param name="dwMode">接触反馈模式 <see cref="TouchFeedbackMode"/>。该 dwMode 参数必须是 <see cref="TouchFeedbackMode.DEFAULT"/>，<see cref="TouchFeedbackMode.INDIRECT"/>，或 <see cref="TouchFeedbackMode.NONE"/>。</param>
        /// <returns>如果函数成功，则返回值为 TRUE。如果函数失败，则返回值为 FALSE。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool InitializeTouchInjection(uint maxCount, TouchFeedbackMode dwMode);

        /// <summary>
        /// 模拟触摸输入。
        /// <para>注意：<see cref="InitializeTouchInjection"/> 必须在对 <see cref="InjectTouchInput"/> 的任何调用之前。</para>
        /// <para>如果指定 <see cref="POINTER_INFO.performanceCount"/> 字段，则在实际注入时，时间戳将以 0.1 毫秒的分辨率转换为当前时间。如果自定义 <see cref="POINTER_INFO.performanceCount"/> 导致与上一次注入相同的.1毫秒窗口，则 API 将返回错误（ERROR_NOT_READY），并且不会注入数据。虽然不会立即因错误使注入无效，但下一次成功的注入必须具有 <see cref="POINTER_INFO.performanceCount"/> 值，该值与先前成功的注入之间至少相隔 0.1 毫秒。同样，如果使用该字段，则自定义 <see cref="POINTER_INFO.dwTime"/> 值必须至少相隔 1 毫秒。</para>
        /// <para>如果在注入参数中同时指定了 <see cref="POINTER_INFO.dwTime"/> 和 <see cref="POINTER_INFO.performanceCount"/>，则 <see cref="InjectTouchInput"/> 失败，并显示错误代码（ERROR_INVALID_PARAMETER）。一旦注入应用程序以 <see cref="POINTER_INFO.dwTime"/> 或 <see cref="POINTER_INFO.performanceCount"/> 参数启动，则时间戳记字段必须正确填写。一旦注入序列开始，注入就无法将自定义时间戳字段从一个切换为另一个。</para>
        /// <para>如果未指定 <see cref="POINTER_INFO.dwTime"/> 或 <see cref="POINTER_INFO.performanceCount"/> 值，则 <see cref="InjectTouchInput"/> 会根据 API 调用的时间分配时间戳。如果调用之间的间隔小于0.1毫秒，则 API 可能返回错误（ERROR_NOT_READY）。该错误不会立即使输入无效，但是注入应用程序需要再次重试同一帧以确保注入成功。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-injecttouchinput </para>
        /// </summary>
        /// <param name="count">contacts 中数组的大小；计数的最大值由 <see cref="InitializeTouchInjection"/> 函数的 maxCount 参数指定。</param>
        /// <param name="contacts">代表桌面上所有 contacts 的 <see cref="POINTER_TOUCH_INFO"/> 结构的数组。每个 contact 的屏幕坐标必须在桌面范围内。</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。要获取扩展的错误信息，请调用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return:MarshalAs(UnmanagedType.Bool)]
        public static extern bool InjectTouchInput(uint count, POINTER_TOUCH_INFO[] contacts);

        /// <summary>
        /// 检索有关与特定触摸输入句柄关联的触摸输入的详细信息。
        /// <para>调用 <see cref="CloseTouchInputHandle"/> 不会空闲内存在一个调用中检索值相关联 <see cref="GetTouchInputInfo"/>。传递到 <see cref="GetTouchInputInfo"/> 的结构中的值 将一直有效，直到您删除它们为止。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-gettouchinputinfo </para>
        /// </summary>
        /// <param name="hTouchInput">在触摸消息的 lParam 中接收到的触摸输入手柄。如果此句柄无效，则函数失败，并显示 ERROR_INVALID_HANDLE。请注意，在成功调用 <see cref="CloseTouchInputHandle"/> 或将其传递给 <see cref="DefWindowProc"/>，<see cref="PostMessage"/>，<see cref="SendMessage"/> 或其变体之一之后，该句柄无效。</param>
        /// <param name="cInputs">pInputs 数组中的结构数。理想地，这应该至少等于与消息 wParam 中指示的消息关联的接触点的数量。如果 cInputs 小于接触点的数量，则该函数仍将成功执行，并使用有关 cInputs 接触点的信息填充 pInputs 缓冲区。</param>
        /// <param name="pInputs">指向 <see cref="TOUCHINPUT"/> 结构数组的指针，以接收有关与指定触摸输入手柄关联的触摸点的信息。</param>
        /// <param name="cbSize">单个 <see cref="TOUCHINPUT"/> 结构的大小（以字节为单位, sizeof(<see cref="TOUCHINPUT"/>)）。如果 cbSize 不是单个 <see cref="TOUCHINPUT"/> 结构的大小，则该函数失败，并显示 ERROR_INVALID_PARAMETER。</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。若要获取扩展的错误信息，请使用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/> 函数。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetTouchInputInfo(IntPtr hTouchInput, uint cInputs, TOUCHINPUT[] pInputs, int cbSize);

        /// <summary>
        /// 关闭触摸输入句柄，释放与其关联的过程内存，并使该句柄无效。
        /// <para>调用 <see cref="CloseTouchInputHandle"/> 不会空闲内存在一个调用中检索值相关联 <see cref="GetTouchInputInfo"/>。传递到 <see cref="GetTouchInputInf"/> 的结构中的值 将一直有效，直到您删除它们为止。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-closetouchinputhandle </para>
        /// </summary>
        /// <param name="hTouchInput">在触摸消息的 lParam 中接收到的触摸输入手柄。如果此句柄无效，则函数失败，并显示 ERROR_INVALID_HANDLE。请注意，在成功调用 <see cref="CloseTouchInputHandle"/> 或将其传递给 <see cref="DefWindowProc"/>，<see cref="PostMessage"/>，<see cref="SendMessage"/> 或其变体之一之后，该句柄无效。</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。若要获取扩展的错误信息，请使用 <see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/> 函数。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseTouchInputHandle(IntPtr hTouchInput);

        /// <summary>
        /// 将窗口注册为可触摸的。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-registertouchwindow </para>
        /// </summary>
        /// <param name="hwnd">正在注册的窗口的句柄。如果调用线程不拥有指定的窗口，则该函数将失败，并显示 ERROR_ACCESS_DENIED。</param>
        /// <param name="ulFlags">一组位标记，用于指定可选的修改。
        /// <para>RegisterTouchWindow flag values </para>
        /// <para>#define TWF_FINETOUCH       (0x00000001)</para>
        /// <para>#define TWF_WANTPALM        (0x00000002)</para>
        /// </param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RegisterTouchWindow(IntPtr hwnd, uint ulFlags);

        /// <summary>
        /// 将窗口注册为不再具有触摸功能。
        /// <para>即使指定的窗口先前未注册为具有触摸功能，<see cref="UnregisterTouchWindow"/> 函数也会成功。</para>
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-unregistertouchwindow </para>
        /// </summary>
        /// <param name="hwnd">窗口的句柄。如果调用线程不拥有指定的窗口，则该函数将失败，并显示 ERROR_ACCESS_DENIED。</param>
        /// <returns>如果函数成功，则返回值为非零。如果函数失败，则返回值为零。若要获取扩展的错误信息，请使用 GetLastError 函数。</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnregisterTouchWindow(IntPtr hwnd);

        /// <summary>
        /// 检查指定的窗口是否具有触摸功能，并有选择地检索为该窗口的触摸功能设置的修改器标志。
        /// <para>参考：https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/nf-winuser-istouchwindow </para>
        /// </summary>
        /// <param name="hwnd">窗口的句柄。如果调用线程不在指定窗口所在的桌面上，则该函数失败，并显示 ERROR_ACCESS_DENIED。</param>
        /// <param name="ulFlags">ULONG变量的地址，用于接收指定窗口的触摸功能的修饰符标志。
        /// <para>pulFlags输出参数的值：</para>
        /// <para>#define TWF_FINETOUCH       (0x00000001)</para>
        /// <para>#define TWF_WANTPALM        (0x00000002)</para>
        /// </param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsTouchWindow(IntPtr hwnd, out uint ulFlags);
    }

    /// <summary>
    /// WindowsAPI User32库，扩展常用/通用，功能/函数，扩展示例，以及使用方式ad
    /// </summary>
    public static partial class User32Extension
    {
    }

}
