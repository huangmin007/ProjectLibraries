using System;
using System.Runtime.InteropServices;
using System.Windows.Input;

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
    #endregion


    #region Structures
    #endregion


    #region Deletages
    #endregion


    #region Notifications
    #endregion


    /// <summary>
    /// user32.dll 常用/实用 函数
    /// <para>注意：<see cref="IntPtr"/> 类型数据，在 32 位软件上是占 4 个字节，在 64 位软件上是占 8 个字节</para>
    /// <para><see cref="Marshal.GetLastWin32Error"/> 或 <see cref="Marshal.GetHRForLastWin32Error"/>;  new WindowInteropHelper(Window).Handle; <see cref="KeyInterop.KeyFromVirtualKey(int)"/></para>
    /// <para>#ifdef UNICODE #define Function FunctionA #else #define Function FunctionW #endif</para>
    /// <para>如果窗口类是使用 ANSI 版本的 RegisterClass（RegisterClassA）注册的，则窗口的字符集是 ANSI。如果窗口类是使用 Unicode 版本的 RegisterClass（RegisterClassW）注册的，则窗口的字符集为 Unicode。</para>
    /// <para>LPCTSTR，LPWSTR, PTSTR, LPTSTR，L表示long指针，P表示这是一个指针，T表示 _T宏 这个宏用来表示你的字符是否使用 UNICODE, 如果你的程序定义了 UNICODE 或者其他相关的宏，那么这个字符或者字符串将被作为 UNICODE 字符串，否则就是标准的 ANSI 字符串。C表示是一个常量const。STR表示这个变量是一个字符串。例如：LPTSTR == ref string;</para>
    /// <para>参考： https://docs.microsoft.com/zh-cn/windows/win32/api/winuser/  头文件目录：C:/Program Files (x86)/Windows Kits/10/Include/10.0.18362.0/um </para>
    /// <para>本机互操作性：https://docs.microsoft.com/zh-cn/dotnet/standard/native-interop/ </para>
    /// </summary>
    public static partial class User32
    {
        #region Functions
        #endregion
    }

    /// <summary>
    /// WindowsAPI User32库，扩展常用/通用，功能/函数，扩展示例，以及使用方式
    /// </summary>
    public static partial class User32Extension
    {
    }
}
