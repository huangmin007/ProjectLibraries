using System;
using System.Runtime.InteropServices;

namespace SpaceCG.WindowsAPI
{

    #region Enumerations
    /// <summary>
    /// 标识线程，进程或窗口的每英寸点数（dpi）设置。
    /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/windef/ne-windef-dpi_awareness </para>
    /// </summary>
    public enum DpiAwaeness
    {
        /// <summary>
        /// DPI意识无效。这是无效的DPI感知值。
        /// </summary>
        INVALID,
        /// <summary>
        /// DPI不知道。此过程无法适应DPI更改，并且始终假定比例因子为100％（96 DPI）。系统将在其他任何DPI设置上自动缩放它。
        /// </summary>
        UNAWARE,
        /// <summary>
        /// 系统DPI感知。此过程无法适应DPI更改。它将一次查询DPI，并在该过程的整个生命周期中使用该值。
        /// <para>如果DPI更改，则该过程将不会调整为新的DPI值。当DPI从系统值更改时，系统会自动按比例将其放大或缩小。</para>
        /// </summary>
        SYSTEM_AWARE,
        /// <summary>
        /// 每个监视器DPI感知。创建DPI时，此过程将对其进行检查，并在DPI更改时调整比例因子。这些过程不会被系统自动缩放
        /// </summary>
        PER_MONITOR_AWARE
    }

    /// <summary>
    /// 标识窗口的 DPI 托管行为。此行为允许在线程中创建的窗口托管具有不同 DPI_AWARENESS_CONTEXT 的子窗口 
    /// <para>https://docs.microsoft.com/en-us/windows/win32/api/windef/ne-windef-dpi_hosting_behavior </para>
    /// </summary>
    public enum DpiHostingBehavior
    {
        /// <summary>
        /// DPI托管行为无效。如果先前的 SetThreadDpiHostingBehavior 调用使用了无效的参数，通常会发生这种情况。
        /// </summary>
        INVALID,
        /// <summary>
        /// 默认的 DPI 托管行为。关联的窗口行为正常，无法使用不同的 DPI_AWARENESS_CONTEXT 创建或重新父级子窗口。
        /// </summary>
        DEFAULT,
        /// <summary>
        /// 混合的 DPI 托管行为。这样可以使用不同的 DPI_AWARENESS_CONTEXT 创建和重新创建父窗口。这些子窗口将由OS独立缩放。
        /// </summary>
        MIXED
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
    /// SIZE 结构定义矩形的宽度和高度。(SIZE, *PSIZE, *LPSIZE)
    /// <para>存储在此结构中的矩形尺寸可以对应于视口范围，窗口范围，文本范围，位图尺寸或某些扩展功能的长宽比过滤器。</para>
    /// <para>参考：https://docs.microsoft.com/en-us/windows/win32/api/windef/ns-windef-size </para>
    /// </summary>
    public struct SIZE
    {
        /// <summary>
        /// 指定矩形的宽度。单位取决于使用此结构的功能。
        /// </summary>
        public int cx;
        /// <summary>
        /// 指定矩形的高度。单位取决于使用此结构的功能。
        /// </summary>
        public int cy;
        /// <summary>
        /// SIZE 结构体
        /// </summary>
        /// <param name="cx"></param>
        /// <param name="cy"></param>
        public SIZE(int cx, int cy)
        {
            this.cx = cx;
            this.cy = cy;
        }
        /// <summary>
        /// @ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[SIZE] cx:{cx}, xy:{cy}";
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
    /// File Time (FILETIME, *PFILETIME, *LPFILETIME)
    /// File System time stamps are represented with the following structure:
    /// </summary>
    public struct FILETIME
    {
        /// <summary>
        /// low date time
        /// </summary>
        public uint dwLowDateTime;
        /// <summary>
        /// high date time
        /// </summary>
        public uint dwHighDateTime;
        /// <summary>
        /// @ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[FILETIME] dwLowDateTime:{dwLowDateTime}, dwHighDateTime:{dwHighDateTime}";
        }
    }
    #endregion


    #region Deletages
    #endregion


    #region Notifications
    #endregion


    /// <summary>
    /// WinDef.h
    /// </summary>
    public static partial class WinDef
    {
        /// <summary>
        /// path max chars 260
        /// </summary>
        public const int MAX_PATH = 260;

        #region Functions
        #endregion

    }
}