//## HotKeyExample
//```C#
using System;
using System.Windows;
using System.Windows.Interop;
using SpaceCG.WindowsAPI.User32;
using System.Collections.Generic;

namespace Examples
{
    public class HotKeyExample
    {
        private IntPtr handle;
        private List<int> HotKeyIDs;

        public HotKeyExample(Window window)
        {
            handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
                throw new ArgumentException(nameof(window), "窗口未初使化，未获取到窗体句柄 。");

            if(!User32.RegisterHotKey(handle, 0, RhkModifier.CONTROL, VirtualKeyCode.VK_T))
                Console.WriteLine("failed..0");
            if(!User32.RegisterHotKey(handle, 1, RhkModifier.CONTROL | RhkModifier.ALT, VirtualKeyCode.VK_F))
                Console.WriteLine("failed..1");

            window.Closing += (s, e) =>
            {
                User32.UnregisterHotKey(handle, 0);
                User32.UnregisterHotKey(handle, 1);
            };
        }

        protected IntPtr WindowProcHandler(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            MessageType msgType = (MessageType)msg;
            Console.WriteLine(msgType);

            if (msgType == MessageType.WM_HOTKEY)
            {
                
                handled = true;
            }

            return IntPtr.Zero;
        }
    }
}
//```