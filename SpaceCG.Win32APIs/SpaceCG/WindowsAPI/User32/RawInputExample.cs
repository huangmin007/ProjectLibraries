//## RawInputExample
//```C#
using System;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using SpaceCG.WindowsAPI.User32;
using System.Collections.Generic;
using System.Linq;

#if false
using HidSharp;
using HidSharp.Reports;
using HidSharp.Reports.Input;
using HidSharp.Reports.Encodings;
#endif

namespace Examples
{
    public class RawInputExample
    {
        private IntPtr handle;
        private HwndSource hwndSource;

        static readonly IReadOnlyDictionary<IntPtr, String> DevicesName;
        static readonly IReadOnlyDictionary<IntPtr, RID_DEVICE_INFO> DevicesInfo;

        static RawInputExample()
        {
            DevicesName = User32Extension.GetRawInputDevicesName();
            DevicesInfo = User32Extension.GetRawIntputDevicesInfo();
        }

        public RawInputExample(Window window)
        {
            handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero) 
                throw new ArgumentException(nameof(window), "窗口未初使化，未获取到窗体句柄 。");

            hwndSource = HwndSource.FromHwnd(handle);
            hwndSource.AddHook(WindowProcHandler);
            //OR
            //hwndSource = PresentationSource.FromVisual(window) as HwndSource;
            //hwndSource.AddHook(new HwndSourceHook(WindowProcHandler));

            RegisterInput();
            window.Closing += (s, e) => hwndSource?.Dispose();
        }

        private void RegisterInput()
        {
            //0x01 0x06 键盘
            //0x01 0x02 鼠标
            //13, 4 SiS HID Touch
            //65280, 1 SiS HID Touch
            RAWINPUTDEVICE mouseDevice = new RAWINPUTDEVICE()
            {
                usUsagePage = 0x01,
                usUsage = 0x02,
                dwFlags = RawInputFlags.RIDEV_INPUTSINK | RawInputFlags.RIDEV_NOLEGACY,
                hwndTarget = handle,
            };
            RAWINPUTDEVICE keyboardDevice = new RAWINPUTDEVICE()
            {
                usUsagePage = 0x01,
                usUsage = 0x06,
                dwFlags = RawInputFlags.RIDEV_INPUTSINK,
                hwndTarget = handle,
            };
            RAWINPUTDEVICE[] rawInputDevices = new RAWINPUTDEVICE[] { mouseDevice, keyboardDevice };

            if (User32.RegisterRawInputDevices(rawInputDevices, (uint)rawInputDevices.Length, RAWINPUTDEVICE.Size))
            {
                Console.WriteLine("注册原始输入数据的设备成功");
            }
            else
            {
                Console.WriteLine("注册原始输入数据的设备失败");
                Exception ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                Console.WriteLine("LastWin32Error Code:{0},{1}  Message:{2}", Marshal.GetLastWin32Error(), Marshal.GetHRForLastWin32Error(), ex.Message);
            }
        }

        protected IntPtr WindowProcHandler(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            MessageType msgType = (MessageType)msg;
            if (msgType != MessageType.WM_INPUT) return IntPtr.Zero;

            handled = true;
            uint pcbSize = 0;

            int result = User32.GetRawInputData(lParam, RIDFlag.RID_INPUT, IntPtr.Zero, ref pcbSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));
            if (result != 0)
            {
                Exception ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                Console.WriteLine("LastWin32Error Exception Message:{0}", ex.Message);
                return IntPtr.Zero;
            }
            Console.WriteLine("pcbSize:{0} {1} {2} {3}", result, RAWINPUTHEADER.Size, Marshal.SizeOf(typeof(RAWKEYBOARD)), pcbSize);
#if false
            IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
            result = User32.GetRawInputData(lParam, RIDFlag.RID_INPUT, pData, ref pcbSize, RAWINPUTHEADER.Size);
            if (pcbSize != result)
            {
                Marshal.FreeHGlobal(pData);
                //Console.WriteLine(Kernel32Utils.GetSysErrroMessage("GetRawInputData"));

                Exception ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                Console.WriteLine("LastWin32Error Exception Message:{0}", ex.Message);
                return IntPtr.Zero;
            }

            RAWINPUT raw = (RAWINPUT)Marshal.PtrToStructure(pData, typeof(RAWINPUT));
            Marshal.FreeHGlobal(pData);
#else
            RAWINPUT raw = new RAWINPUT();
            result = User32.GetRawInputData(lParam, RIDFlag.RID_INPUT, ref raw, ref pcbSize, RAWINPUTHEADER.Size);
            if (pcbSize != result)
            {
                Exception ex = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                Console.WriteLine("LastWin32Error Code:{0},{1}  Message:{2}", Marshal.GetLastWin32Error(), Marshal.GetHRForLastWin32Error(), ex.Message);
                return IntPtr.Zero;
            }
#endif
            Console.WriteLine(raw);
            string name = null;
            if (!DevicesName.TryGetValue(raw.header.hDevice, out name))
            {
                Console.WriteLine("不存在的设备句柄：{0}", raw.header.hDevice);
                //return IntPtr.Zero;
            }

            //Console.WriteLine(raw);
            Console.WriteLine("Current Input Device Name: {0} {1}", name, raw);

            switch (raw.header.dwType)
            {
                case RawInputType.RIM_TYPEMOUSE:
                    Console.WriteLine(raw.data.mouse);
                    break;

                case RawInputType.RIM_TYPEKEYBOARD:
                    Console.WriteLine(raw.data.keyboard);
                    break;

                case RawInputType.RIM_TYPEHID:
                    Console.WriteLine(raw.data.hid);
                    byte[] rawData = raw.data.hid.GetRawData();
                    Console.WriteLine("HID Raw Data Length: {0}", rawData.Length);
                    break;

                default:
                    return IntPtr.Zero;
            }

            return IntPtr.Zero;
        }

#if false
        DeviceItemInputParser InputParser;
        private void PrintfHidDeviceInfo()
        {
            HidDevice[] devices = DeviceList.Local.GetHidDevices(1111, 4755).ToArray();

            foreach (HidDevice dev in devices)
            {
                Console.WriteLine("-----------------------------");
                Console.WriteLine("{0}, {1}", dev, dev.DevicePath);

                try
                {
                    Console.WriteLine(string.Format("Max Lengths: Input {0}, Output {1}, Feature {2}", dev.GetMaxInputReportLength(), dev.GetMaxOutputReportLength(), dev.GetMaxFeatureReportLength()));
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e);
                    continue;
                }

                try
                {
                    var rawReportDescriptor = dev.GetRawReportDescriptor();
                    Console.WriteLine("Report Descriptor:");
                    Console.WriteLine("  {0} ({1} bytes)", string.Join(" ", rawReportDescriptor.Select(d => d.ToString("X2"))), rawReportDescriptor.Length);
                    int indent = 0;
                    foreach (EncodedItem element in EncodedItem.DecodeItems(rawReportDescriptor, 0, rawReportDescriptor.Length))
                    {
                        if (element.ItemType == ItemType.Main && element.TagForMain == MainItemTag.EndCollection) { indent -= 4; }
                        Console.WriteLine("  {0}{1}", new string(' ', indent), element);
                        if (element.ItemType == ItemType.Main && element.TagForMain == MainItemTag.Collection) { indent += 4; }
                    }

                    var reportDescriptor = dev.GetReportDescriptor();
                    foreach (DeviceItem deviceItem in reportDescriptor.DeviceItems)
                    {
                        if (InputParser == null)
                            InputParser = deviceItem.CreateDeviceItemInputParser();

                        foreach (var usage in deviceItem.Usages.GetAllValues())
                            Console.WriteLine(string.Format("Usage: {0:X4} {1}", usage, (Usage)usage));

                        foreach (var report in deviceItem.Reports)
                        {
                            Console.WriteLine(string.Format("{0}: ReportID={1}, Length={2}, Items={3}",
                                                report.ReportType, report.ReportID, report.Length, report.DataItems.Count));
                            foreach (DataItem dataItem in report.DataItems)
                            {
                                Console.WriteLine(string.Format("  {0} Elements x {1} Bits, Units: {2}, Expected Usage Type: {3}, Flags: {4}, Usages: {5}  TotalBits: {6}",
                                    dataItem.ElementCount, dataItem.ElementBits, dataItem.Unit.System, dataItem.ExpectedUsageType, dataItem.Flags,
                                    string.Join(", ", dataItem.Usages.GetAllValues().Select(usage => usage.ToString("X4") + " " + ((Usage)usage).ToString())), dataItem.TotalBits));
                            }
                        }

                        //tryOpen
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception:::{0}", ex);
                }
            }
        }
#endif
    }
}
//```