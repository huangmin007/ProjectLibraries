using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Test2
{
    // Token: 0x02000017 RID: 23
    internal class NativeMethods
    {
        // Token: 0x0600016B RID: 363
        [DllImport("user32.dll")]
        internal static extern IntPtr SetClipboardViewer(IntPtr hWndNewViewer);

        // Token: 0x0600016C RID: 364
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

        // Token: 0x0600016D RID: 365
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        // Token: 0x0600016E RID: 366
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, int action, IntPtr changeInfo);

        // Token: 0x0600016F RID: 367
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PostMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        // Token: 0x06000170 RID: 368
        [DllImport("user32.dll")]
        internal static extern IntPtr GetDesktopWindow();

        // Token: 0x06000171 RID: 369
        [DllImport("user32.dll")]
        internal static extern IntPtr GetWindowDC(IntPtr hWnd);

        // Token: 0x06000172 RID: 370
        [DllImport("user32.dll")]
        internal static extern int DrawText(IntPtr hDC, string lpString, int nCount, ref NativeMethods.RECT lpRect, uint uFormat);

        // Token: 0x06000173 RID: 371
        [DllImport("gdi32.dll")]
        internal static extern uint SetTextColor(IntPtr hdc, int crColor);

        // Token: 0x06000174 RID: 372
        [DllImport("gdi32.dll")]
        internal static extern uint SetBkColor(IntPtr hdc, int crColor);

        // Token: 0x06000175 RID: 373
        [DllImport("user32.dll")]
        internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        // Token: 0x06000176 RID: 374
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        // Token: 0x06000177 RID: 375
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        // Token: 0x06000178 RID: 376
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        // Token: 0x06000179 RID: 377
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        // Token: 0x0600017A RID: 378
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        // Token: 0x0600017B RID: 379
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        // Token: 0x0600017C RID: 380
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowText(IntPtr hWnd, string lpString);

        // Token: 0x0600017D RID: 381
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int GetWindowTextLength(IntPtr hWnd);

        // Token: 0x0600017E RID: 382
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        // Token: 0x0600017F RID: 383
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyIcon(IntPtr handle);

        // Token: 0x06000180 RID: 384
        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetCursorPos(int X, int Y);

        // Token: 0x06000181 RID: 385
        //[DllImport("user32", SetLastError = true)]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //internal static extern bool GetCursorPos(ref Point p);

        // Token: 0x06000182 RID: 386
        [DllImport("advapi32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, ref IntPtr TokenHandle);

        // Token: 0x06000183 RID: 387
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DuplicateToken(IntPtr ExistingTokenHandle, int SECURITY_IMPERSONATION_LEVEL, ref IntPtr DuplicateTokenHandle);

        // Token: 0x06000184 RID: 388
        [DllImport("advapi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DuplicateTokenEx(IntPtr ExistingTokenHandle, uint dwDesiredAccess, ref NativeMethods.SECURITY_ATTRIBUTES lpThreadAttributes, int TokenType, int ImpersonationLevel, ref IntPtr DuplicateTokenHandle);

        // Token: 0x06000185 RID: 389
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ConvertStringSidToSid(string StringSid, out IntPtr ptrSid);

        // Token: 0x06000186 RID: 390
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetTokenInformation(IntPtr TokenHandle, NativeMethods.TOKEN_INFORMATION_CLASS TokenInformationClass, ref NativeMethods.TOKEN_MANDATORY_LABEL TokenInformation, uint TokenInformationLength);

        // Token: 0x06000187 RID: 391
        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments")]
        [DllImport("advapi32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine, ref NativeMethods.SECURITY_ATTRIBUTES lpProcessAttributes, ref NativeMethods.SECURITY_ATTRIBUTES lpThreadAttributes, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref NativeMethods.STARTUPINFO lpStartupInfo, out NativeMethods.PROCESS_INFORMATION lpProcessInformation);

        // Token: 0x06000188 RID: 392
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, NativeMethods.EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

        // Token: 0x06000189 RID: 393
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref NativeMethods.MonitorInfoEx lpmi);

        // Token: 0x0600018A RID: 394
        [DllImport("User32.dll")]
        internal static extern int FindWindow(string ClassName, string WindowName);

        // Token: 0x0600018B RID: 395
        [DllImport("kernel32.dll")]
        internal static extern uint GetCurrentThreadId();

        // Token: 0x0600018C RID: 396
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetThreadDesktop(uint dwThreadId);

        // Token: 0x0600018D RID: 397
        [DllImport("user32.dll")]
        internal static extern short GetAsyncKeyState(IntPtr vKey);

        // Token: 0x0600018E RID: 398
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // Token: 0x0600018F RID: 399
        [DllImport("user32.dll")]
        internal static extern bool GetCursorInfo(out NativeMethods.CURSORINFO ci);

        // Token: 0x06000190 RID: 400
        [DllImport("kernel32", SetLastError = true)]
        internal static extern uint WaitForSingleObject(IntPtr handle, int milliseconds);

        // Token: 0x06000191 RID: 401
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool TerminateProcess(IntPtr hProcess, IntPtr exitCode);

        // Token: 0x06000192 RID: 402
        [DllImport("ntdll.dll")]
        internal static extern int NtQueryInformationProcess(IntPtr hProcess, int processInformationClass, ref NativeMethods.PROCESS_BASIC_INFORMATION processBasicInformation, uint processInformationLength, out uint returnLength);

        // Token: 0x06000193 RID: 403
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr OpenInputDesktop(uint dwFlags, [MarshalAs(UnmanagedType.Bool)] bool fInherit, uint dwDesiredAccess);

        // Token: 0x06000194 RID: 404
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetUserObjectInformation(IntPtr hObj, int nIndex, [Out] byte[] pvInfo, int nLength, out uint lpnLengthNeeded);

        // Token: 0x06000195 RID: 405
        //[DllImport("user32.dll")]
        //internal static extern IntPtr CreateIconIndirect(ref IconInfo icon);

        // Token: 0x06000196 RID: 406
        [DllImport("gdi32.dll")]
        internal static extern uint SetPixel(IntPtr hdc, int X, int Y, uint crColor);

        // Token: 0x06000197 RID: 407
        [SuppressMessage("Microsoft.Portability", "CA1901:PInvokeDeclarationsShouldBePortable")]
        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int SetWindowsHookEx(int idHook, NativeMethods.HookProc lpfn, IntPtr hMod, int dwThreadId);

        // Token: 0x06000198 RID: 408
        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int UnhookWindowsHookEx(int idHook);

        // Token: 0x06000199 RID: 409
        [SuppressMessage("Microsoft.Portability", "CA1901:PInvokeDeclarationsShouldBePortable")]
        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        internal static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);

        // Token: 0x0600019A RID: 410
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SendInput(uint nInputs, NativeMethods.INPUT[] pInputs, int cbSize);

        // Token: 0x0600019B RID: 411
        [DllImport("user32.dll", EntryPoint = "SendInput", SetLastError = true)]
        internal static extern uint SendInput64(uint nInputs, NativeMethods.INPUT64[] pInputs, int cbSize);

        // Token: 0x17000048 RID: 72
        // (get) Token: 0x0600019C RID: 412 RVA: 0x0000CAA0 File Offset: 0x0000ACA0
        // (set) Token: 0x0600019D RID: 413 RVA: 0x0000CAA7 File Offset: 0x0000ACA7
        internal static bool InjectMouseInputAvailable { get; set; }

        // Token: 0x0600019E RID: 414
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint InjectMouseInput(NativeMethods.MOUSEINPUT[] pInputs, int nInputs);

        // Token: 0x0600019F RID: 415
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetMessageExtraInfo();

        // Token: 0x060001A0 RID: 416
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint LockWorkStation();

        // Token: 0x060001A1 RID: 417
        [DllImport("user32.dll")]
        internal static extern uint MapVirtualKey(uint uCode, uint uMapType);

        // Token: 0x060001A2 RID: 418
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CloseHandle(IntPtr hSnapshot);

        // Token: 0x060001A3 RID: 419
        [DllImport("kernel32.dll")]
        internal static extern uint WTSGetActiveConsoleSessionId();

        // Token: 0x060001A4 RID: 420
        [DllImport("Wtsapi32.dll")]
        internal static extern uint WTSQueryUserToken(uint SessionId, ref IntPtr phToken);

        // Token: 0x060001A5 RID: 421
        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "1")]
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool LookupPrivilegeValue(IntPtr lpSystemName, string lpname, [MarshalAs(UnmanagedType.Struct)] ref NativeMethods.LUID lpLuid);

        // Token: 0x060001A6 RID: 422
        [DllImport("kernel32.dll")]
        internal static extern IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        // Token: 0x060001A7 RID: 423
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges, ref NativeMethods.TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        // Token: 0x060001A8 RID: 424
        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreateEnvironmentBlock(ref IntPtr lpEnvironment, IntPtr hToken, [MarshalAs(UnmanagedType.Bool)] bool bInherit);

        // Token: 0x060001A9 RID: 425
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

        // Token: 0x060001AA RID: 426
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool RevertToSelf();

        // Token: 0x060001AB RID: 427
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GlobalMemoryStatusEx([In][Out] NativeMethods.MEMORYSTATUSEX lpBuffer);

        // Token: 0x060001AC RID: 428
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetProcessDPIAware();

        // Token: 0x060001AD RID: 429
        [DllImport("Shcore.dll", SetLastError = true)]
        internal static extern int SetProcessDpiAwareness(uint type);

        // Token: 0x060001AE RID: 430
        [DllImport("secur32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool GetUserNameEx(int nameFormat, StringBuilder userName, ref uint userNameSize);

        // Token: 0x060001AF RID: 431
        [DllImport("Shcore.dll", SetLastError = true)]
        internal static extern int GetDpiForMonitor(IntPtr hMonitor, uint dpiType, out uint dpiX, out uint dpiY);

        // Token: 0x060001B0 RID: 432 RVA: 0x0000CAB0 File Offset: 0x0000ACB0
        private static string GetDNSDomain()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return null;
            }
            StringBuilder stringBuilder = new StringBuilder(1024);
            uint capacity = (uint)stringBuilder.Capacity;
            if (!NativeMethods.GetUserNameEx(12, stringBuilder, ref capacity))
            {
                return null;
            }
            string[] array = stringBuilder.ToString().Split(new char[]
            {
                '\\'
            });
            if (2 != array.Length)
            {
                return null;
            }
            return array[0];
        }

        // Token: 0x060001B1 RID: 433 RVA: 0x0000CB10 File Offset: 0x0000AD10
        internal static bool IsRunningAtMicrosoft()
        {
            string dnsdomain = NativeMethods.GetDNSDomain();
            return !string.IsNullOrEmpty(dnsdomain) && dnsdomain.EndsWith("microsoft.com", true, CultureInfo.CurrentCulture);
        }

        // Token: 0x060001B2 RID: 434 RVA: 0x00002294 File Offset: 0x00000494
        private NativeMethods()
        {
        }

        // Token: 0x04000164 RID: 356
        internal const uint WAIT_OBJECT_0 = 0U;

        // Token: 0x04000165 RID: 357
        internal const int WM_SHOW_DRAG_DROP = 1024;

        // Token: 0x04000166 RID: 358
        internal const int WM_HIDE_DRAG_DROP = 1025;

        // Token: 0x04000167 RID: 359
        internal const int WM_CHECK_EXPLORER_DRAG_DROP = 1026;

        // Token: 0x04000168 RID: 360
        internal const int WM_QUIT = 1027;

        // Token: 0x04000169 RID: 361
        internal const int WM_SWITCH = 1028;

        // Token: 0x0400016A RID: 362
        internal const int WM_HIDE_DD_HELPER = 1029;

        // Token: 0x0400016B RID: 363
        internal const int WM_SHOW_SETTINGS_FORM = 1030;

        // Token: 0x0400016C RID: 364
        internal static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        // Token: 0x0400016D RID: 365
        internal const uint SWP_NOSIZE = 1U;

        // Token: 0x0400016E RID: 366
        internal const uint SWP_NOMOVE = 2U;

        // Token: 0x0400016F RID: 367
        internal const uint SWP_NOZORDER = 4U;

        // Token: 0x04000170 RID: 368
        internal const uint SWP_NOREDRAW = 8U;

        // Token: 0x04000171 RID: 369
        internal const uint SWP_SHOWWINDOW = 64U;

        // Token: 0x04000172 RID: 370
        internal const uint SWP_HIDEWINDOW = 128U;

        // Token: 0x04000173 RID: 371
        internal const int UOI_FLAGS = 1;

        // Token: 0x04000174 RID: 372
        internal const int UOI_NAME = 2;

        // Token: 0x04000175 RID: 373
        internal const int UOI_TYPE = 3;

        // Token: 0x04000176 RID: 374
        internal const int UOI_USER_SID = 4;

        // Token: 0x04000177 RID: 375
        internal const uint DESKTOP_WRITEOBJECTS = 128U;

        // Token: 0x04000178 RID: 376
        internal const uint DESKTOP_READOBJECTS = 1U;

        // Token: 0x04000179 RID: 377
        internal const uint DF_ALLOWOTHERACCOUNTHOOK = 1U;

        // Token: 0x0400017A RID: 378
        internal const uint GENERIC_WRITE = 1073741824U;

        // Token: 0x0400017B RID: 379
        internal const uint GENERIC_ALL = 268435456U;

        // Token: 0x0400017C RID: 380
        internal const int CCHDEVICENAME = 32;

        // Token: 0x0400017E RID: 382
        internal const int READ_CONTROL = 131072;

        // Token: 0x0400017F RID: 383
        internal const int STANDARD_RIGHTS_REQUIRED = 983040;

        // Token: 0x04000180 RID: 384
        internal const int STANDARD_RIGHTS_READ = 131072;

        // Token: 0x04000181 RID: 385
        internal const int STANDARD_RIGHTS_WRITE = 131072;

        // Token: 0x04000182 RID: 386
        internal const int STANDARD_RIGHTS_EXECUTE = 131072;

        // Token: 0x04000183 RID: 387
        internal const int STANDARD_RIGHTS_ALL = 2031616;

        // Token: 0x04000184 RID: 388
        internal const int SPECIFIC_RIGHTS_ALL = 65535;

        // Token: 0x04000185 RID: 389
        internal const int TOKEN_IMPERSONATE = 4;

        // Token: 0x04000186 RID: 390
        internal const int TOKEN_QUERY_SOURCE = 16;

        // Token: 0x04000187 RID: 391
        internal const int TOKEN_ADJUST_PRIVILEGES = 32;

        // Token: 0x04000188 RID: 392
        internal const int TOKEN_ADJUST_GROUPS = 64;

        // Token: 0x04000189 RID: 393
        internal const int TOKEN_ADJUST_SESSIONID = 256;

        // Token: 0x0400018A RID: 394
        internal const int TOKEN_ALL_ACCESS_P = 983295;

        // Token: 0x0400018B RID: 395
        internal const int TOKEN_ALL_ACCESS = 983551;

        // Token: 0x0400018C RID: 396
        internal const int TOKEN_READ = 131080;

        // Token: 0x0400018D RID: 397
        internal const int TOKEN_WRITE = 131296;

        // Token: 0x0400018E RID: 398
        internal const int TOKEN_EXECUTE = 131072;

        // Token: 0x0400018F RID: 399
        internal const int CREATE_NEW_PROCESS_GROUP = 512;

        // Token: 0x04000190 RID: 400
        internal const int CREATE_UNICODE_ENVIRONMENT = 1024;

        // Token: 0x04000191 RID: 401
        internal const int IDLE_PRIORITY_CLASS = 64;

        // Token: 0x04000192 RID: 402
        internal const int NORMAL_PRIORITY_CLASS = 32;

        // Token: 0x04000193 RID: 403
        internal const int HIGH_PRIORITY_CLASS = 128;

        // Token: 0x04000194 RID: 404
        internal const int REALTIME_PRIORITY_CLASS = 256;

        // Token: 0x04000195 RID: 405
        internal const int CREATE_NEW_CONSOLE = 16;

        // Token: 0x04000196 RID: 406
        internal const string SE_DEBUG_NAME = "SeDebugPrivilege";

        // Token: 0x04000197 RID: 407
        internal const string SE_RESTORE_NAME = "SeRestorePrivilege";

        // Token: 0x04000198 RID: 408
        internal const string SE_BACKUP_NAME = "SeBackupPrivilege";

        // Token: 0x04000199 RID: 409
        internal const int SE_PRIVILEGE_ENABLED = 2;

        // Token: 0x0400019A RID: 410
        internal const int ERROR_NOT_ALL_ASSIGNED = 1300;

        // Token: 0x0400019B RID: 411
        internal const uint TH32CS_SNAPPROCESS = 2U;

        // Token: 0x0400019C RID: 412
        internal const int TOKEN_DUPLICATE = 2;

        // Token: 0x0400019D RID: 413
        internal const int TOKEN_QUERY = 8;

        // Token: 0x0400019E RID: 414
        internal const int TOKEN_ADJUST_DEFAULT = 128;

        // Token: 0x0400019F RID: 415
        internal const int TOKEN_ASSIGN_PRIMARY = 1;

        // Token: 0x040001A0 RID: 416
        internal const uint MAXIMUM_ALLOWED = 33554432U;

        // Token: 0x040001A1 RID: 417
        internal const int SE_GROUP_INTEGRITY = 32;

        // Token: 0x02000063 RID: 99
        internal struct POINT
        {
            // Token: 0x0400039D RID: 925
            internal int x;

            // Token: 0x0400039E RID: 926
            internal int y;
        }

        // Token: 0x02000064 RID: 100
        internal struct CURSORINFO
        {
            // Token: 0x0400039F RID: 927
            public int cbSize;

            // Token: 0x040003A0 RID: 928
            public int flags;

            // Token: 0x040003A1 RID: 929
            public IntPtr hCursor;

            // Token: 0x040003A2 RID: 930
            public NativeMethods.POINT ptScreenPos;
        }

        // Token: 0x02000065 RID: 101
        internal struct PROCESS_BASIC_INFORMATION
        {
            // Token: 0x040003A3 RID: 931
            public int ExitStatus;

            // Token: 0x040003A4 RID: 932
            public int PebBaseAddress;

            // Token: 0x040003A5 RID: 933
            public int AffinityMask;

            // Token: 0x040003A6 RID: 934
            public int BasePriority;

            // Token: 0x040003A7 RID: 935
            public uint UniqueProcessId;

            // Token: 0x040003A8 RID: 936
            public uint InheritedFromUniqueProcessId;
        }

        // Token: 0x02000066 RID: 102
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct RECT
        {
            // Token: 0x040003A9 RID: 937
            internal int Left;

            // Token: 0x040003AA RID: 938
            internal int Top;

            // Token: 0x040003AB RID: 939
            internal int Right;

            // Token: 0x040003AC RID: 940
            internal int Bottom;
        }

        // Token: 0x02000067 RID: 103
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct MonitorInfoEx
        {
            // Token: 0x040003AD RID: 941
            internal int cbSize;

            // Token: 0x040003AE RID: 942
            internal NativeMethods.RECT rcMonitor;

            // Token: 0x040003AF RID: 943
            internal NativeMethods.RECT rcWork;

            // Token: 0x040003B0 RID: 944
            internal uint dwFlags;
        }

        // Token: 0x02000068 RID: 104
        private enum InputType
        {
            // Token: 0x040003B2 RID: 946
            INPUT_MOUSE,
            // Token: 0x040003B3 RID: 947
            INPUT_KEYBOARD,
            // Token: 0x040003B4 RID: 948
            INPUT_HARDWARE
        }

        // Token: 0x02000069 RID: 105
        [Flags]
        internal enum MOUSEEVENTF
        {
            // Token: 0x040003B6 RID: 950
            MOVE = 1,
            // Token: 0x040003B7 RID: 951
            LEFTDOWN = 2,
            // Token: 0x040003B8 RID: 952
            LEFTUP = 4,
            // Token: 0x040003B9 RID: 953
            RIGHTDOWN = 8,
            // Token: 0x040003BA RID: 954
            RIGHTUP = 16,
            // Token: 0x040003BB RID: 955
            MIDDLEDOWN = 32,
            // Token: 0x040003BC RID: 956
            MIDDLEUP = 64,
            // Token: 0x040003BD RID: 957
            XDOWN = 128,
            // Token: 0x040003BE RID: 958
            XUP = 256,
            // Token: 0x040003BF RID: 959
            WHEEL = 2048,
            // Token: 0x040003C0 RID: 960
            VIRTUALDESK = 16384,
            // Token: 0x040003C1 RID: 961
            ABSOLUTE = 32768
        }

        // Token: 0x0200006A RID: 106
        [Flags]
        internal enum KEYEVENTF
        {
            // Token: 0x040003C3 RID: 963
            KEYDOWN = 0,
            // Token: 0x040003C4 RID: 964
            EXTENDEDKEY = 1,
            // Token: 0x040003C5 RID: 965
            KEYUP = 2,
            // Token: 0x040003C6 RID: 966
            UNICODE = 4,
            // Token: 0x040003C7 RID: 967
            SCANCODE = 8
        }

        // Token: 0x0200006B RID: 107
        internal struct MOUSEINPUT
        {
            // Token: 0x040003C8 RID: 968
            internal int dx;

            // Token: 0x040003C9 RID: 969
            internal int dy;

            // Token: 0x040003CA RID: 970
            internal int mouseData;

            // Token: 0x040003CB RID: 971
            internal int dwFlags;

            // Token: 0x040003CC RID: 972
            internal int time;

            // Token: 0x040003CD RID: 973
            internal IntPtr dwExtraInfo;
        }

        // Token: 0x0200006C RID: 108
        internal struct KEYBDINPUT
        {
            // Token: 0x040003CE RID: 974
            internal short wVk;

            // Token: 0x040003CF RID: 975
            internal short wScan;

            // Token: 0x040003D0 RID: 976
            internal int dwFlags;

            // Token: 0x040003D1 RID: 977
            internal int time;

            // Token: 0x040003D2 RID: 978
            internal IntPtr dwExtraInfo;
        }

        // Token: 0x0200006D RID: 109
        internal struct HARDWAREINPUT
        {
            // Token: 0x040003D3 RID: 979
            internal int uMsg;

            // Token: 0x040003D4 RID: 980
            internal short wParamL;

            // Token: 0x040003D5 RID: 981
            internal short wParamH;
        }

        // Token: 0x0200006E RID: 110
        [SuppressMessage("Microsoft.Portability", "CA1900:ValueTypeFieldsShouldBePortable")]
        [StructLayout(LayoutKind.Explicit)]
        internal struct INPUT
        {
            // Token: 0x040003D6 RID: 982
            [FieldOffset(0)]
            internal int type;

            // Token: 0x040003D7 RID: 983
            [FieldOffset(4)]
            internal NativeMethods.MOUSEINPUT mi;

            // Token: 0x040003D8 RID: 984
            [FieldOffset(4)]
            internal NativeMethods.KEYBDINPUT ki;
        }

        // Token: 0x0200006F RID: 111
        [StructLayout(LayoutKind.Explicit)]
        internal struct INPUT64
        {
            // Token: 0x040003D9 RID: 985
            [FieldOffset(0)]
            internal int type;

            // Token: 0x040003DA RID: 986
            [FieldOffset(8)]
            internal NativeMethods.MOUSEINPUT mi;

            // Token: 0x040003DB RID: 987
            [FieldOffset(8)]
            internal NativeMethods.KEYBDINPUT ki;
        }

        // Token: 0x02000070 RID: 112
        internal struct LUID
        {
            // Token: 0x040003DC RID: 988
            internal int LowPart;

            // Token: 0x040003DD RID: 989
            internal int HighPart;
        }

        // Token: 0x02000071 RID: 113
        internal struct LUID_AND_ATRIBUTES
        {
            // Token: 0x040003DE RID: 990
            internal NativeMethods.LUID Luid;

            // Token: 0x040003DF RID: 991
            internal int Attributes;
        }

        // Token: 0x02000072 RID: 114
        internal struct TOKEN_PRIVILEGES
        {
            // Token: 0x040003E0 RID: 992
            internal int PrivilegeCount;

            // Token: 0x040003E1 RID: 993
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            internal int[] Privileges;
        }

        // Token: 0x02000073 RID: 115
        internal struct PROCESSENTRY32
        {
            // Token: 0x040003E2 RID: 994
            internal uint dwSize;

            // Token: 0x040003E3 RID: 995
            internal uint cntUsage;

            // Token: 0x040003E4 RID: 996
            internal uint th32ProcessID;

            // Token: 0x040003E5 RID: 997
            internal IntPtr th32DefaultHeapID;

            // Token: 0x040003E6 RID: 998
            internal uint th32ModuleID;

            // Token: 0x040003E7 RID: 999
            internal uint cntThreads;

            // Token: 0x040003E8 RID: 1000
            internal uint th32ParentProcessID;

            // Token: 0x040003E9 RID: 1001
            internal int pcPriClassBase;

            // Token: 0x040003EA RID: 1002
            internal uint dwFlags;

            // Token: 0x040003EB RID: 1003
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            internal string szExeFile;
        }

        // Token: 0x02000074 RID: 116
        internal struct SECURITY_ATTRIBUTES
        {
            // Token: 0x040003EC RID: 1004
            internal int Length;

            // Token: 0x040003ED RID: 1005
            internal IntPtr lpSecurityDescriptor;

            // Token: 0x040003EE RID: 1006
            internal bool bInheritHandle;
        }

        // Token: 0x02000075 RID: 117
        internal struct PROCESS_INFORMATION
        {
            // Token: 0x040003EF RID: 1007
            internal IntPtr hProcess;

            // Token: 0x040003F0 RID: 1008
            internal IntPtr hThread;

            // Token: 0x040003F1 RID: 1009
            internal uint dwProcessId;

            // Token: 0x040003F2 RID: 1010
            internal uint dwThreadId;
        }

        // Token: 0x02000076 RID: 118
        internal struct STARTUPINFO
        {
            // Token: 0x040003F3 RID: 1011
            internal int cb;

            // Token: 0x040003F4 RID: 1012
            internal string lpReserved;

            // Token: 0x040003F5 RID: 1013
            internal string lpDesktop;

            // Token: 0x040003F6 RID: 1014
            internal string lpTitle;

            // Token: 0x040003F7 RID: 1015
            internal uint dwX;

            // Token: 0x040003F8 RID: 1016
            internal uint dwY;

            // Token: 0x040003F9 RID: 1017
            internal uint dwXSize;

            // Token: 0x040003FA RID: 1018
            internal uint dwYSize;

            // Token: 0x040003FB RID: 1019
            internal uint dwXCountChars;

            // Token: 0x040003FC RID: 1020
            internal uint dwYCountChars;

            // Token: 0x040003FD RID: 1021
            internal uint dwFillAttribute;

            // Token: 0x040003FE RID: 1022
            internal uint dwFlags;

            // Token: 0x040003FF RID: 1023
            internal short wShowWindow;

            // Token: 0x04000400 RID: 1024
            internal short cbReserved2;

            // Token: 0x04000401 RID: 1025
            internal IntPtr lpReserved2;

            // Token: 0x04000402 RID: 1026
            internal IntPtr hStdInput;

            // Token: 0x04000403 RID: 1027
            internal IntPtr hStdOutput;

            // Token: 0x04000404 RID: 1028
            internal IntPtr hStdError;
        }

        // Token: 0x02000077 RID: 119
        internal struct SID_AND_ATTRIBUTES
        {
            // Token: 0x04000405 RID: 1029
            internal IntPtr Sid;

            // Token: 0x04000406 RID: 1030
            internal int Attributes;
        }

        // Token: 0x02000078 RID: 120
        internal struct TOKEN_MANDATORY_LABEL
        {
            // Token: 0x04000407 RID: 1031
            internal NativeMethods.SID_AND_ATTRIBUTES Label;
        }

        // Token: 0x02000079 RID: 121
        internal enum SECURITY_IMPERSONATION_LEVEL
        {
            // Token: 0x04000409 RID: 1033
            SecurityAnonymous,
            // Token: 0x0400040A RID: 1034
            SecurityIdentification,
            // Token: 0x0400040B RID: 1035
            SecurityImpersonation,
            // Token: 0x0400040C RID: 1036
            SecurityDelegation
        }

        // Token: 0x0200007A RID: 122
        internal enum TOKEN_TYPE
        {
            // Token: 0x0400040E RID: 1038
            TokenPrimary = 1,
            // Token: 0x0400040F RID: 1039
            TokenImpersonation
        }

        // Token: 0x0200007B RID: 123
        internal enum TOKEN_INFORMATION_CLASS
        {
            // Token: 0x04000411 RID: 1041
            TokenUser = 1,
            // Token: 0x04000412 RID: 1042
            TokenGroups,
            // Token: 0x04000413 RID: 1043
            TokenPrivileges,
            // Token: 0x04000414 RID: 1044
            TokenOwner,
            // Token: 0x04000415 RID: 1045
            TokenPrimaryGroup,
            // Token: 0x04000416 RID: 1046
            TokenDefaultDacl,
            // Token: 0x04000417 RID: 1047
            TokenSource,
            // Token: 0x04000418 RID: 1048
            TokenType,
            // Token: 0x04000419 RID: 1049
            TokenImpersonationLevel,
            // Token: 0x0400041A RID: 1050
            TokenStatistics,
            // Token: 0x0400041B RID: 1051
            TokenRestrictedSids,
            // Token: 0x0400041C RID: 1052
            TokenSessionId,
            // Token: 0x0400041D RID: 1053
            TokenGroupsAndPrivileges,
            // Token: 0x0400041E RID: 1054
            TokenSessionReference,
            // Token: 0x0400041F RID: 1055
            TokenSandBoxInert,
            // Token: 0x04000420 RID: 1056
            TokenAuditPolicy,
            // Token: 0x04000421 RID: 1057
            TokenOrigin,
            // Token: 0x04000422 RID: 1058
            TokenElevationType,
            // Token: 0x04000423 RID: 1059
            TokenLinkedToken,
            // Token: 0x04000424 RID: 1060
            TokenElevation,
            // Token: 0x04000425 RID: 1061
            TokenHasRestrictions,
            // Token: 0x04000426 RID: 1062
            TokenAccessInformation,
            // Token: 0x04000427 RID: 1063
            TokenVirtualizationAllowed,
            // Token: 0x04000428 RID: 1064
            TokenVirtualizationEnabled,
            // Token: 0x04000429 RID: 1065
            TokenIntegrityLevel,
            // Token: 0x0400042A RID: 1066
            TokenUIAccess,
            // Token: 0x0400042B RID: 1067
            TokenMandatoryPolicy,
            // Token: 0x0400042C RID: 1068
            TokenLogonSid,
            // Token: 0x0400042D RID: 1069
            MaxTokenInfoClass
        }

        // Token: 0x0200007C RID: 124
        // (Invoke) Token: 0x06000475 RID: 1141
        internal delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.RECT lprcMonitor, IntPtr dwData);

        // Token: 0x0200007D RID: 125
        // (Invoke) Token: 0x06000479 RID: 1145
        internal delegate int HookProc(int nCode, int wParam, IntPtr lParam);

        // Token: 0x0200007E RID: 126
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class MEMORYSTATUSEX
        {
            // Token: 0x0600047C RID: 1148 RVA: 0x000223F4 File Offset: 0x000205F4
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(NativeMethods.MEMORYSTATUSEX));
            }

            // Token: 0x0400042E RID: 1070
            public uint dwLength;

            // Token: 0x0400042F RID: 1071
            public uint dwMemoryLoad;

            // Token: 0x04000430 RID: 1072
            public ulong ullTotalPhys;

            // Token: 0x04000431 RID: 1073
            public ulong ullAvailPhys;

            // Token: 0x04000432 RID: 1074
            public ulong ullTotalPageFile;

            // Token: 0x04000433 RID: 1075
            public ulong ullAvailPageFile;

            // Token: 0x04000434 RID: 1076
            public ulong ullTotalVirtual;

            // Token: 0x04000435 RID: 1077
            public ulong ullAvailVirtual;

            // Token: 0x04000436 RID: 1078
            public ulong ullAvailExtendedVirtual;
        }

        // Token: 0x0200007F RID: 127
        internal enum EXTENDED_NAME_FORMAT
        {
            // Token: 0x04000438 RID: 1080
            NameUnknown,
            // Token: 0x04000439 RID: 1081
            NameFullyQualifiedDN,
            // Token: 0x0400043A RID: 1082
            NameSamCompatible,
            // Token: 0x0400043B RID: 1083
            NameDisplay,
            // Token: 0x0400043C RID: 1084
            NameUniqueId = 6,
            // Token: 0x0400043D RID: 1085
            NameCanonical,
            // Token: 0x0400043E RID: 1086
            NameUserPrincipal,
            // Token: 0x0400043F RID: 1087
            NameCanonicalEx,
            // Token: 0x04000440 RID: 1088
            NameServicePrincipal,
            // Token: 0x04000441 RID: 1089
            NameDnsDomain = 12
        }
    }
}
