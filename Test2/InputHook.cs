using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;
using System.Windows.Forms;
using Test2.Properties;

namespace Test2
{
    internal struct MOUSEDATA
    {
        // Token: 0x040000F5 RID: 245
        internal int x;

        // Token: 0x040000F6 RID: 246
        internal int y;

        // Token: 0x040000F7 RID: 247
        internal int wDelta;

        // Token: 0x040000F8 RID: 248
        internal int dwFlags;
    }

    // Token: 0x0200000D RID: 13
    internal struct KEYBDDATA
    {
        // Token: 0x040000F3 RID: 243
        internal int wVk;

        // Token: 0x040000F4 RID: 244
        internal int dwFlags;
    }

    // Token: 0x02000010 RID: 16
    internal enum VK : ushort
    {
        // Token: 0x0400010A RID: 266
        CAPITAL = 20,
        // Token: 0x0400010B RID: 267
        NUMLOCK = 144,
        // Token: 0x0400010C RID: 268
        SHIFT = 16,
        // Token: 0x0400010D RID: 269
        CONTROL,
        // Token: 0x0400010E RID: 270
        MENU,
        // Token: 0x0400010F RID: 271
        ESCAPE = 27,
        // Token: 0x04000110 RID: 272
        BACK = 8,
        // Token: 0x04000111 RID: 273
        TAB,
        // Token: 0x04000112 RID: 274
        RETURN = 13,
        // Token: 0x04000113 RID: 275
        PRIOR = 33,
        // Token: 0x04000114 RID: 276
        NEXT,
        // Token: 0x04000115 RID: 277
        END,
        // Token: 0x04000116 RID: 278
        HOME,
        // Token: 0x04000117 RID: 279
        LEFT,
        // Token: 0x04000118 RID: 280
        UP,
        // Token: 0x04000119 RID: 281
        RIGHT,
        // Token: 0x0400011A RID: 282
        DOWN,
        // Token: 0x0400011B RID: 283
        SELECT,
        // Token: 0x0400011C RID: 284
        PRINT,
        // Token: 0x0400011D RID: 285
        EXECUTE,
        // Token: 0x0400011E RID: 286
        SNAPSHOT,
        // Token: 0x0400011F RID: 287
        INSERT,
        // Token: 0x04000120 RID: 288
        DELETE,
        // Token: 0x04000121 RID: 289
        HELP,
        // Token: 0x04000122 RID: 290
        NUMPAD0 = 96,
        // Token: 0x04000123 RID: 291
        NUMPAD1,
        // Token: 0x04000124 RID: 292
        NUMPAD2,
        // Token: 0x04000125 RID: 293
        NUMPAD3,
        // Token: 0x04000126 RID: 294
        NUMPAD4,
        // Token: 0x04000127 RID: 295
        NUMPAD5,
        // Token: 0x04000128 RID: 296
        NUMPAD6,
        // Token: 0x04000129 RID: 297
        NUMPAD7,
        // Token: 0x0400012A RID: 298
        NUMPAD8,
        // Token: 0x0400012B RID: 299
        NUMPAD9,
        // Token: 0x0400012C RID: 300
        MULTIPLY,
        // Token: 0x0400012D RID: 301
        ADD,
        // Token: 0x0400012E RID: 302
        SEPARATOR,
        // Token: 0x0400012F RID: 303
        SUBTRACT,
        // Token: 0x04000130 RID: 304
        DECIMAL,
        // Token: 0x04000131 RID: 305
        DIVIDE,
        // Token: 0x04000132 RID: 306
        F1,
        // Token: 0x04000133 RID: 307
        F2,
        // Token: 0x04000134 RID: 308
        F3,
        // Token: 0x04000135 RID: 309
        F4,
        // Token: 0x04000136 RID: 310
        F5,
        // Token: 0x04000137 RID: 311
        F6,
        // Token: 0x04000138 RID: 312
        F7,
        // Token: 0x04000139 RID: 313
        F8,
        // Token: 0x0400013A RID: 314
        F9,
        // Token: 0x0400013B RID: 315
        F10,
        // Token: 0x0400013C RID: 316
        F11,
        // Token: 0x0400013D RID: 317
        F12,
        // Token: 0x0400013E RID: 318
        OEM_1 = 186,
        // Token: 0x0400013F RID: 319
        OEM_PLUS,
        // Token: 0x04000140 RID: 320
        OEM_COMMA,
        // Token: 0x04000141 RID: 321
        OEM_MINUS,
        // Token: 0x04000142 RID: 322
        OEM_PERIOD,
        // Token: 0x04000143 RID: 323
        OEM_2,
        // Token: 0x04000144 RID: 324
        OEM_3,
        // Token: 0x04000145 RID: 325
        MEDIA_NEXT_TRACK = 176,
        // Token: 0x04000146 RID: 326
        MEDIA_PREV_TRACK,
        // Token: 0x04000147 RID: 327
        MEDIA_STOP,
        // Token: 0x04000148 RID: 328
        MEDIA_PLAY_PAUSE,
        // Token: 0x04000149 RID: 329
        LWIN = 91,
        // Token: 0x0400014A RID: 330
        RWIN,
        // Token: 0x0400014B RID: 331
        LSHIFT = 160,
        // Token: 0x0400014C RID: 332
        RSHIFT,
        // Token: 0x0400014D RID: 333
        LCONTROL,
        // Token: 0x0400014E RID: 334
        RCONTROL,
        // Token: 0x0400014F RID: 335
        LMENU,
        // Token: 0x04000150 RID: 336
        RMENU
    }
    // Token: 0x0200003A RID: 58
    internal enum EasyMouseOption
    {
        // Token: 0x04000323 RID: 803
        Disable,
        // Token: 0x04000324 RID: 804
        Enable,
        // Token: 0x04000325 RID: 805
        Ctrl,
        // Token: 0x04000326 RID: 806
        Shift
    }

    public class InputHook
    {
        // Token: 0x0400021E RID: 542
        private int hMouseHook;

        // Token: 0x0400021F RID: 543
        private int hKeyboardHook;

        // Token: 0x04000220 RID: 544
        private static NativeMethods.HookProc MouseHookProcedure;

        // Token: 0x04000221 RID: 545
        private static NativeMethods.HookProc KeyboardHookProcedure;

        // Token: 0x04000222 RID: 546
        private static InputHook.MouseLLHookStruct mouseHookStruct;

        // Token: 0x04000223 RID: 547
        private static InputHook.KeyboardHookStruct keydbHookStruct;

        // Token: 0x04000224 RID: 548
        private static MOUSEDATA hookCallbackMouseData = default(MOUSEDATA);

        // Token: 0x04000225 RID: 549
        private static KEYBDDATA hookCallbackKeybdData = default(KEYBDDATA);

        // Token: 0x04000226 RID: 550
        private static bool winDown;

        // Token: 0x04000227 RID: 551
        private static bool ctrlDown;

        // Token: 0x04000228 RID: 552
        private static bool altDown;

        // Token: 0x04000229 RID: 553
        private static bool shiftDown;

        // Token: 0x0400022A RID: 554
        private static bool realData = true;

        // Token: 0x0400022E RID: 558
        private int ctrlTouchesDnIndex;

        // Token: 0x0400022F RID: 559
        private int ctrlTouchesUpIndex = 1;

        // Token: 0x04000230 RID: 560
        private const int CTRL_TOUCH = 4;

        // Token: 0x04000231 RID: 561
        private long[] ctrlTouches = new long[4];

        // Token: 0x04000232 RID: 562
        private static long lastHotKeyLockMachine = 0L;

        // Token: 0x02000082 RID: 130
        // (Invoke) Token: 0x06000483 RID: 1155
        internal delegate void MouseEvHandler(MOUSEDATA e, int dx, int dy);

        // Token: 0x02000083 RID: 131
        // (Invoke) Token: 0x06000487 RID: 1159
        internal delegate void KeybdEvHandler(KEYBDDATA e);

        // Token: 0x02000084 RID: 132
        private struct MouseHookStruct
        {
            // Token: 0x04000445 RID: 1093
            internal NativeMethods.POINT pt;

            // Token: 0x04000446 RID: 1094
            internal int hwnd;

            // Token: 0x04000447 RID: 1095
            internal int wHitTestCode;

            // Token: 0x04000448 RID: 1096
            internal int dwExtraInfo;
        }

        // Token: 0x02000085 RID: 133
        private struct MouseLLHookStruct
        {
            // Token: 0x04000449 RID: 1097
            internal NativeMethods.POINT pt;

            // Token: 0x0400044A RID: 1098
            internal int mouseData;

            // Token: 0x0400044B RID: 1099
            internal int flags;

            // Token: 0x0400044C RID: 1100
            internal int time;

            // Token: 0x0400044D RID: 1101
            internal int dwExtraInfo;
        }

        // Token: 0x02000086 RID: 134
        private struct KeyboardHookStruct
        {
            // Token: 0x0400044E RID: 1102
            internal int vkCode;

            // Token: 0x0400044F RID: 1103
            internal int scanCode;

            // Token: 0x04000450 RID: 1104
            internal int flags;

            // Token: 0x04000451 RID: 1105
            internal int time;

            // Token: 0x04000452 RID: 1106
            internal int dwExtraInfo;
        }

        // Token: 0x14000002 RID: 2
        // (add) Token: 0x06000250 RID: 592 RVA: 0x00013768 File Offset: 0x00011968
        // (remove) Token: 0x06000251 RID: 593 RVA: 0x000137A0 File Offset: 0x000119A0
        internal event InputHook.MouseEvHandler MouseEvent;

        // Token: 0x14000003 RID: 3
        // (add) Token: 0x06000252 RID: 594 RVA: 0x000137D8 File Offset: 0x000119D8
        // (remove) Token: 0x06000253 RID: 595 RVA: 0x00013810 File Offset: 0x00011A10
        internal event InputHook.KeybdEvHandler KeydbEvent;

        // Token: 0x17000065 RID: 101
        // (get) Token: 0x06000254 RID: 596 RVA: 0x00013845 File Offset: 0x00011A45
        // (set) Token: 0x06000255 RID: 597 RVA: 0x0001384C File Offset: 0x00011A4C
        internal static bool RealData
        {
            get
            {
                return InputHook.realData;
            }
            set
            {
                InputHook.realData = value;
            }
        }

        // Token: 0x17000066 RID: 102
        // (get) Token: 0x06000256 RID: 598 RVA: 0x00013854 File Offset: 0x00011A54
        // (set) Token: 0x06000257 RID: 599 RVA: 0x0001385B File Offset: 0x00011A5B
        internal static int SkipMouseUpCount { get; set; } = 0;

        // Token: 0x17000067 RID: 103
        // (get) Token: 0x06000258 RID: 600 RVA: 0x00013863 File Offset: 0x00011A63
        // (set) Token: 0x06000259 RID: 601 RVA: 0x0001386A File Offset: 0x00011A6A
        internal static bool SkipMouseUpDown { get; set; } = false;

        // Token: 0x17000068 RID: 104
        // (get) Token: 0x0600025A RID: 602 RVA: 0x00013872 File Offset: 0x00011A72
        internal static bool CtrlDown
        {
            get
            {
                return InputHook.ctrlDown;
            }
        }

        // Token: 0x17000069 RID: 105
        // (get) Token: 0x0600025B RID: 603 RVA: 0x00013879 File Offset: 0x00011A79
        // (set) Token: 0x0600025C RID: 604 RVA: 0x00013880 File Offset: 0x00011A80
        internal static bool EasyMouseKeyDown { get; set; }

        public InputHook()
        {
            Start();
        }


        ~InputHook()
        {
            Stop();
        }


        public void Start()
        {
            bool flag = false;
#if false
            InputHook.MouseHookProcedure = new NativeMethods.HookProc(this.MouseHookProc);
            this.hMouseHook = NativeMethods.SetWindowsHookEx(14, InputHook.MouseHookProcedure, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);
            if (this.hMouseHook == 0)
            {
                Console.WriteLine("Error installing mouse hook: " + Marshal.GetLastWin32Error().ToString(CultureInfo.CurrentCulture), false);
                flag = true;
                this.Stop();
            }
#endif
            InputHook.KeyboardHookProcedure = new NativeMethods.HookProc(this.KeyboardHookProc);
            this.hKeyboardHook = NativeMethods.SetWindowsHookEx(13, InputHook.KeyboardHookProcedure, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);
            if (this.hKeyboardHook == 0)
            {
                Console.WriteLine("Error installing keyboard hook: " + Marshal.GetLastWin32Error().ToString(CultureInfo.CurrentCulture), false);
                flag = true;
                this.Stop();
            }
            if (flag)
            {
                //if (!Common.RunOnLogonDesktop && !Common.RunOnScrSaverDesktop)
                {
                    MessageBox.Show("Error installing keyboard/mouse hook!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                    return;
                }
            }
            else
            {
                //Common.InitLastInputEventCount();
            }
        }

        public void Stop()
        {
            if (this.hMouseHook != 0)
            {
                bool flag = NativeMethods.UnhookWindowsHookEx(this.hMouseHook) != 0;
                this.hMouseHook = 0;
                if (!flag)
                {
                    Console.WriteLine("Exception uninstalling mouse hook, error code: " + Marshal.GetLastWin32Error().ToString(CultureInfo.CurrentCulture), false);
                }
            }
            if (this.hKeyboardHook != 0)
            {
                bool flag2 = NativeMethods.UnhookWindowsHookEx(this.hKeyboardHook) != 0;
                this.hKeyboardHook = 0;
                if (!flag2)
                {
                    Console.WriteLine("Exception uninstalling keyboard hook, error code: " + Marshal.GetLastWin32Error().ToString(CultureInfo.CurrentCulture), false);
                }
            }
        }
#if false
        // Token: 0x06000263 RID: 611 RVA: 0x00013A84 File Offset: 0x00011C84
        private int MouseHookProc(int nCode, int wParam, IntPtr lParam)
        {
            int result = 1;
            int num = 0;
            int num2 = 0;
            bool flag = false;
            //Common.InputEventCount += 1UL;
            try
            {
                if (!InputHook.RealData)
                {
                    InputHook.RealData = true;
                    result = NativeMethods.CallNextHookEx(this.hMouseHook, nCode, wParam, lParam);
                }
                else
                {
#if false
                    Common.RealInputEventCount += 1UL;
                    if (Common.NewDesMachineID == Common.MachineID || Common.NewDesMachineID == ID.ALL)
                    {
                        flag = true;
                        if (Common.MainFormVisible && !Common.IsDroping)
                        {
                            Common.MainFormDot();
                        }
                    }
#endif
                    if (nCode >= 0 && this.MouseEvent != null)
                    {
                        if (wParam == 514 && InputHook.SkipMouseUpCount > 0)
                        {
                            Console.WriteLine(string.Format("{0}: {1}.", "SkipMouseUpCount", InputHook.SkipMouseUpCount), false);
                            InputHook.SkipMouseUpCount--;
                            result = NativeMethods.CallNextHookEx(this.hMouseHook, nCode, wParam, lParam);
                            return result;
                        }
                        if ((wParam == 514 || wParam == 513) && InputHook.SkipMouseUpDown)
                        {
                            result = NativeMethods.CallNextHookEx(this.hMouseHook, nCode, wParam, lParam);
                            return result;
                        }
                        InputHook.mouseHookStruct = InputHook.lParamToMouseLLHookStruct(lParam);
                        InputHook.hookCallbackMouseData.dwFlags = wParam;
                        InputHook.hookCallbackMouseData.wDelta = (int)((short)(InputHook.mouseHookStruct.mouseData >> 16 & 65535));
                        if (flag)
                        {
                            InputHook.hookCallbackMouseData.x = InputHook.mouseHookStruct.pt.x;
                            InputHook.hookCallbackMouseData.y = InputHook.mouseHookStruct.pt.y;
                            //if (Setting.DrawMouse && Common.MouseCursorForm != null)
                            //{
                            //    CustomCursor.ShowFakeMouseCursor(int.MinValue, int.MinValue);
                            //}
                        }
                        else if (Common.SwitchLocation.Count > 0 && Common.NewDesMachineID != Common.MachineID && Common.NewDesMachineID != ID.ALL)
                        {
                            MouseLocation switchLocation = Common.SwitchLocation;
                            int count = switchLocation.Count;
                            switchLocation.Count = count - 1;
                            if (Common.SwitchLocation.X > Common.XY_BY_PIXEL - 100000 || Common.SwitchLocation.Y > Common.XY_BY_PIXEL - 100000)
                            {
                                InputHook.hookCallbackMouseData.x = Common.SwitchLocation.X - Common.XY_BY_PIXEL;
                                InputHook.hookCallbackMouseData.y = Common.SwitchLocation.Y - Common.XY_BY_PIXEL;
                            }
                            else
                            {
                                InputHook.hookCallbackMouseData.x = Common.SwitchLocation.X * Common.ScreenWidth / 65535 + Common.PrimaryScreenBounds.Left;
                                InputHook.hookCallbackMouseData.y = Common.SwitchLocation.Y * Common.ScreenHeight / 65535 + Common.PrimaryScreenBounds.Top;
                            }
                            Common.HideMouseCursor(false);
                        }
                        else
                        {
                            num = InputHook.mouseHookStruct.pt.x - Common.LastPos.X;
                            num2 = InputHook.mouseHookStruct.pt.y - Common.LastPos.Y;
                            InputHook.hookCallbackMouseData.x = InputHook.hookCallbackMouseData.x + num;
                            InputHook.hookCallbackMouseData.y = InputHook.hookCallbackMouseData.y + num2;
                            if (InputHook.hookCallbackMouseData.x < Common.PrimaryScreenBounds.Left)
                            {
                                InputHook.hookCallbackMouseData.x = Common.PrimaryScreenBounds.Left - 1;
                            }
                            else if (InputHook.hookCallbackMouseData.x > Common.PrimaryScreenBounds.Right)
                            {
                                InputHook.hookCallbackMouseData.x = Common.PrimaryScreenBounds.Right + 1;
                            }
                            if (InputHook.hookCallbackMouseData.y < Common.PrimaryScreenBounds.Top)
                            {
                                InputHook.hookCallbackMouseData.y = Common.PrimaryScreenBounds.Top - 1;
                            }
                            else if (InputHook.hookCallbackMouseData.y > Common.PrimaryScreenBounds.Bottom)
                            {
                                InputHook.hookCallbackMouseData.y = Common.PrimaryScreenBounds.Bottom + 1;
                            }
                            num += ((num < 0) ? (-Common.MOVE_MOUSE_RELATIVE) : Common.MOVE_MOUSE_RELATIVE);
                            num2 += ((num2 < 0) ? (-Common.MOVE_MOUSE_RELATIVE) : Common.MOVE_MOUSE_RELATIVE);
                        }
                        this.MouseEvent(InputHook.hookCallbackMouseData, num, num2);
                        Common.DragDropStep01(wParam);
                        Common.DragDropStep09(wParam);
                    }
                    if (flag)
                    {
                        result = NativeMethods.CallNextHookEx(this.hMouseHook, nCode, wParam, lParam);
                    }
                }
            }
            catch (Exception e)
            {
                Common.Log(e);
                result = NativeMethods.CallNextHookEx(this.hMouseHook, nCode, wParam, lParam);
            }
            return result;
        }
#endif

        private int KeyboardHookProc(int nCode, int wParam, IntPtr lParam)
        {
            //Common.InputEventCount += 1UL;
            if (!InputHook.RealData)
            {
                return NativeMethods.CallNextHookEx(this.hKeyboardHook, nCode, wParam, lParam);
            }
            //Common.RealInputEventCount += 1UL;
            InputHook.keydbHookStruct = InputHook.lParamToKeyboardHookStruct(lParam);
            InputHook.hookCallbackKeybdData.dwFlags = InputHook.keydbHookStruct.flags;
            InputHook.hookCallbackKeybdData.wVk = (int)((short)InputHook.keydbHookStruct.vkCode);
            if (nCode >= 0 && this.KeydbEvent != null)
            {
                if (!this.ProcessKeyEx(InputHook.keydbHookStruct.vkCode, InputHook.keydbHookStruct.flags, InputHook.hookCallbackKeybdData))
                {
                    return 1;
                }
                this.KeydbEvent(InputHook.hookCallbackKeybdData);
            }
            //if (Common.DesMachineID != ID.NONE && Common.DesMachineID != ID.ALL && Common.DesMachineID != Common.MachineID)
            //{
            //    return 1;
            //}
            //if (nCode >= 0 && Setting.UseVKMap && Setting.VKMap != null && Setting.VKMap.ContainsKey(InputHook.hookCallbackKeybdData.wVk) && !InputHook.CtrlDown)
            //{
            //    InputSimu.SendKey(InputHook.hookCallbackKeybdData);
            //    return 1;
            //}
            return NativeMethods.CallNextHookEx(this.hKeyboardHook, nCode, wParam, lParam);
        }

        // Token: 0x06000265 RID: 613 RVA: 0x00013FF0 File Offset: 0x000121F0
        private bool ProcessKeyEx(int vkCode, int flags, KEYBDDATA hookCallbackKeybdData)
        {
            VK vk;
            if ((flags & 128) != 128)
            {
                this.UpdateEasyMouseKeyDown((VK)vkCode);
                vk = (VK)vkCode;
                if (vk <= VK.DELETE)
                {
                    if (vk != VK.ESCAPE)
                    {
                        if (vk == VK.DELETE)
                        {
                            if (!InputHook.ctrlDown || !InputHook.altDown)
                            {
                                return true;
                            }
                            InputHook.ctrlDown = (InputHook.altDown = false);
                            this.KeydbEvent(hookCallbackKeybdData);
                            //if (Common.DesMachineID != ID.ALL)
                            //{
                            //    Common.SwitchToMachine(Common.MachineName.Trim());
                            //    return true;
                            //}
                            return true;
                        }
                    }
                    else
                    {
                        //if (Common.IsTopMostMessageNotNull())
                        //{
                         //   Common.HideTopMostMessage();
                        //    return true;
                       // }
                        return true;
                    }
                }
                else if (vk != (VK)76)
                {
                    if (vk - VK.LWIN <= 1)
                    {
                        InputHook.winDown = true;
                        return true;
                    }
                    switch (vk)
                    {
                        case VK.LSHIFT:
                            InputHook.shiftDown = true;
                            return true;
                        case VK.LCONTROL:
                        case VK.RCONTROL:
                            {
                                //Common.Log("VK.RCONTROL", false);
                                InputHook.ctrlDown = true;
                                //if (Setting.HotKeySwitch2AllPC == 1)
                                //{
                                //    this.ctrlTouches[this.ctrlTouchesDnIndex] = Common.GetTick();
                                //   this.ctrlTouchesDnIndex = (this.ctrlTouchesDnIndex + 2) % 4;
                                //}
                                bool flag = true;
                                for (int i = 0; i < 4; i++)
                                {
                                    if (this.ctrlTouches[i] == 0L)// || Common.GetTick() - this.ctrlTouches[i] > 400L)
                                    {
                                        flag = false;
                                        break;
                                    }
                                }
                                if (flag)// && Common.GetTick() - Common.IJustGotAKey > 1000L)
                                {
                                    this.ResetLastSwitchKeys();
                                    //Common.SwitchToMultipleMode(Common.DesMachineID != ID.ALL, true);
                                    return true;
                                }
                                return true;
                            }
                        case VK.LMENU:
                        case VK.RMENU:
                            InputHook.altDown = true;
                            return true;
                    }
                }
                else
                {
                    //if (!InputHook.winDown)
                    //{
                    //    return this.ProcessHotKeys(vkCode, hookCallbackKeybdData);
                    //}
                    InputHook.winDown = false;
                    //if (Common.DesMachineID != ID.ALL)
                    //{
                        this.KeydbEvent(hookCallbackKeybdData);
                        //Common.SwitchToMachine(Common.MachineName.Trim());
                        //return true;
                    //}
                    return true;
                }
                //Common.Log("X", false);
                //return this.ProcessHotKeys(vkCode, hookCallbackKeybdData);
            }
            InputHook.EasyMouseKeyDown = false;
            vk = (VK)vkCode;
            if (vk - VK.LWIN > 1)
            {
                switch (vk)
                {
                    case VK.LSHIFT:
                        InputHook.shiftDown = false;
                        break;
                    case VK.LCONTROL:
                    case VK.RCONTROL:
                        InputHook.ctrlDown = false;
                        //if (Setting.HotKeySwitch2AllPC == 1)
                        //{
                        //    this.ctrlTouches[this.ctrlTouchesUpIndex] = Common.GetTick();
                        //    this.ctrlTouchesUpIndex = (this.ctrlTouchesUpIndex % 4 + 2) % 4;
                        //}
                        break;
                    case VK.LMENU:
                    case VK.RMENU:
                        InputHook.altDown = false;
                        break;
                }
            }
            else
            {
                InputHook.winDown = false;
            }
            return true;
        }

        // Token: 0x06000266 RID: 614 RVA: 0x00014274 File Offset: 0x00012474
        private void UpdateEasyMouseKeyDown(VK vkCode)
        {
            EasyMouseOption easyMouse = EasyMouseOption.Enable;// (EasyMouseOption)Setting.EasyMouse;
            InputHook.EasyMouseKeyDown = ((easyMouse == EasyMouseOption.Ctrl && (vkCode == VK.LCONTROL || vkCode == VK.RCONTROL)) || (easyMouse == EasyMouseOption.Shift && (vkCode == VK.LSHIFT || vkCode == VK.RSHIFT)));
        }

        
        // Token: 0x06000269 RID: 617 RVA: 0x00014708 File Offset: 0x00012908
        internal void ResetLastSwitchKeys()
        {
            for (int i = 0; i < 4; i++)
            {
                this.ctrlTouches[i] = 0L;
            }
            InputHook.ctrlDown = (InputHook.winDown = (InputHook.altDown = false));
        }

        // Token: 0x06000261 RID: 609 RVA: 0x00013A69 File Offset: 0x00011C69
        private unsafe static InputHook.MouseLLHookStruct lParamToMouseLLHookStruct(IntPtr lParam)
        {
            return *(InputHook.MouseLLHookStruct*)((void*)lParam);
        }

        // Token: 0x06000262 RID: 610 RVA: 0x00013A76 File Offset: 0x00011C76
        private unsafe static InputHook.KeyboardHookStruct lParamToKeyboardHookStruct(IntPtr lParam)
        {
            return *(InputHook.KeyboardHookStruct*)((void*)lParam);
        }
    }

}
