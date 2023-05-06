//## DeviceNotificationExample
//```C#
using System;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using SpaceCG.WindowsAPI.User32;

namespace Examples
{
    public class DeviceNotificationExample
    {
        private IntPtr handle;
        private HwndSource hwndSource;

        public DeviceNotificationExample(Window window)
        {
            handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
                throw new ArgumentException(nameof(window), "窗口未初使化，未获取到窗体句柄 。");

            hwndSource = HwndSource.FromHwnd(handle);
            hwndSource.AddHook(WindowProcHandler);
            //OR
            //hwndSource = PresentationSource.FromVisual(window) as HwndSource;
            //hwndSource.AddHook(new HwndSourceHook(WindowProcHandler));

            window.Closing += (s, e) => hwndSource.Dispose();
        }

        protected IntPtr WindowProcHandler(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            MessageType msgType = (MessageType)msg;
            Console.WriteLine(msgType);

            if (msg == (int)MessageType.WM_DEVICECHANGE)
            {
                DeviceBroadcastType dbt = (DeviceBroadcastType)wParam.ToInt32();
                Console.WriteLine(dbt);

                switch (dbt)
                {
                    case DeviceBroadcastType.DBT_DEVICEARRIVAL:
                    case DeviceBroadcastType.DBT_DEVICEREMOVECOMPLETE:
                        Console.WriteLine(dbt == DeviceBroadcastType.DBT_DEVICEARRIVAL ? "Device Arrival" : "Device Move Complete");

                        DEV_BROADCAST_HDR hdr = Marshal.PtrToStructure<DEV_BROADCAST_HDR>(lParam);
                        Console.WriteLine("{0}", hdr);

                        if (hdr.dbch_devicetype == DeviceType.DBT_DEVTYP_PORT)
                        {
                            DEV_BROADCAST_PORT port = Marshal.PtrToStructure<DEV_BROADCAST_PORT>(lParam);
                            Console.WriteLine(port);
                        }
                        if (hdr.dbch_devicetype == DeviceType.DBT_DEVTYP_VOLUME)
                        {
                            DEV_BROADCAST_VOLUME volume = Marshal.PtrToStructure<DEV_BROADCAST_VOLUME>(lParam);
                            Console.WriteLine(volume);
                        }
                        break;

                    default:
                        break;
                }

                handled = true;
            }

            return IntPtr.Zero;
        }
    }
}
//```