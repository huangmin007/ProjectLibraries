using System;
using System.Configuration;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Interop;
//using SpaceCG.WindowsAPI.User32;

namespace SpaceCG.Extensions
{
    public static partial class SerialPortExtensions
    {
        /// <summary>
        /// log4net.Logger 对象
        /// </summary>
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(nameof(SerialPortExtensions));

        #region 扩展的配置动态调用函数
        public static void SendBytes(this SerialPort serialPort, byte[] buffer)
        {
            if (!serialPort.IsOpen) return;
            serialPort.Write(buffer, 0, buffer.Length);
        }

        public static void SendMessage(this SerialPort serialPort, string message)
        {
            SendBytes(serialPort, Encoding.UTF8.GetBytes(message));
        }
        #endregion

        /// <summary>
        /// 快捷创建 SerialPort 对象
        /// <para>配置的键值格式：(命名空间.Serial属性)，属性 PortName 不可为空</para>
        /// </summary>
        /// <param name="portNameCfgKey"></param>
        /// <param name="onSerialDataReceivedEventHandler"></param>
        /// <returns></returns>
        public static SerialPort CreateSerialPort(string portNameCfgKey, SerialDataReceivedEventHandler onSerialDataReceivedEventHandler)
        {
            if (String.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[portNameCfgKey])) return null;

            String nameSpace = portNameCfgKey.Replace("PortName", "");
            SerialPort serialPort = new SerialPort();
            InstanceExtensions.SetInstancePropertyValues(serialPort, nameSpace);

            serialPort.ErrorReceived += (s, e) => Logger.Warn($"串行端口 ({serialPort.PortName},{serialPort.BaudRate}) 发生了错误({e.EventType})");
            serialPort.PinChanged += (s, e) => Logger.Warn($"串行端口 ({serialPort.PortName},{serialPort.BaudRate}) 发生了非数据信号事件({e.EventType})");
            if (Logger.IsDebugEnabled)
            {
                serialPort.DataReceived += (s, e) => Logger.Debug($"串行端口 ({serialPort.PortName},{serialPort.BaudRate}) 接收了数据({e.EventType})，接收缓冲区中数据: {serialPort.BytesToRead}(Bytes)");
            };
            if (onSerialDataReceivedEventHandler != null)
                serialPort.DataReceived += onSerialDataReceivedEventHandler;

            try
            {
                serialPort.Open();
                Logger.Info($"创建并打开新的串行端口 ({serialPort.PortName},{serialPort.BaudRate}) 完成");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return serialPort;
        }
        /// <summary>
        /// 关闭并释放 SerialPort 对象
        /// </summary>
        /// <param name="serialPort"></param>
        public static void DisposeSerialPort(ref SerialPort serialPort)
        {
            if (serialPort == null) return;
            InstanceExtensions.RemoveInstanceEvents(serialPort);

            try
            {
                if (serialPort.IsOpen) serialPort.Close();
                serialPort.Dispose();
                Logger.Info($"关闭并释放串行端口 ({serialPort.PortName},{serialPort.BaudRate}) 完成");
            }
            catch (Exception ex)
            {
                Logger.Error($"关闭并释放串行端口资源 {serialPort} 对象错误：{ex}");
            }
            finally
            {
                serialPort = null;
            }
        }

        /// <summary>
        /// 串口设备热插拔自动重新连接
        /// <para>使用 ManagementEventWatcher WQL 事件监听模式</para>
        /// </summary>
        /// <param name="serialPort"></param>
        public static void AutoReconnection(this SerialPort serialPort)
        {
            if (serialPort == null) throw new ArgumentException("参数不能为空");
            Logger.InfoFormat("ManagementEventWatcher WQL Event Listen SerialPort Name:{0}", serialPort.PortName);

            TimeSpan withinInterval = TimeSpan.FromSeconds(1);
            //string wql_condition = $"TargetInstance isa 'Win32_PnPEntity' AND TargetInstance.PNPClass='WPD'"; //移动U盘
            string wql_condition = $"TargetInstance isa 'Win32_PnPEntity' AND TargetInstance.Name LIKE '%({serialPort.PortName.ToUpper()})'";

            ManagementScope scope = new ManagementScope(@"\\.\Root\CIMV2")
            {
                Options = new ConnectionOptions() { EnablePrivileges = true },
            };

            ManagementEventWatcher CreationEvent = new ManagementEventWatcher(scope, new WqlEventQuery("__InstanceCreationEvent", withinInterval, wql_condition));
            CreationEvent.EventArrived += (s, e) =>
            {
                if (!serialPort.IsOpen) serialPort.Open();
                Logger.InfoFormat("Instance Creation Event SerialPort Name:{0}", serialPort.PortName);
            };
            ManagementEventWatcher DeletionEvent = new ManagementEventWatcher(scope, new WqlEventQuery("__InstanceDeletionEvent", withinInterval, wql_condition));
            DeletionEvent.EventArrived += (s, e) =>
            {
                if (serialPort.IsOpen) serialPort.Close();
                Logger.ErrorFormat("Instance Deletion Event SerialPort Name:{0}", serialPort.PortName);
            };

            CreationEvent.Start();
            DeletionEvent.Start();
        }

#if false //需要窗体相关的库，以及 Win32 API
        /// <summary>
        /// 串口设备热插拔自动重新连接
        /// <para>使用 HwndSource Hook Window Message #WM_DEVICECHANGE 事件监听模式</para>
        /// </summary>
        /// <param name="serialPort"></param>
        /// <param name="window">IsLoaded 为 True 的窗口对象</param>
        public static void AutoReconnection(this SerialPort serialPort, System.Windows.Window window)
        {
            if (serialPort == null || window == null) throw new ArgumentException("参数不能为空");
            if (!window.IsLoaded) throw new InvalidOperationException("Window 对象 IsLoaded 为 True 时才能获取窗口句柄");
            Logger.InfoFormat("HwndSource Hook Window Message #WM_DEVICECHANGE Event Listen SerialPort Name:{0}", serialPort.PortName);

            HwndSource hwndSource = HwndSource.FromVisual(window) as HwndSource;
            if (hwndSource != null) hwndSource.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                MessageType mt = (MessageType)msg;
                if (mt != MessageType.WM_DEVICECHANGE) return IntPtr.Zero;
                DeviceBroadcastType dbt = (DeviceBroadcastType)wParam.ToInt32();
                if (dbt == DeviceBroadcastType.DBT_DEVICEARRIVAL || dbt == DeviceBroadcastType.DBT_DEVICEREMOVECOMPLETE)
                {
                    DEV_BROADCAST_HDR hdr = (DEV_BROADCAST_HDR)Marshal.PtrToStructure(lParam, typeof(DEV_BROADCAST_HDR));
                    if (hdr.dbch_devicetype != DeviceType.DBT_DEVTYP_PORT) return IntPtr.Zero;
                    DEV_BROADCAST_PORT port = (DEV_BROADCAST_PORT)Marshal.PtrToStructure(lParam, typeof(DEV_BROADCAST_PORT));
                    if (port.dbcp_name.ToUpper() != serialPort.PortName.ToUpper()) return IntPtr.Zero;
                    if (dbt == DeviceBroadcastType.DBT_DEVICEARRIVAL)
                    {
                        if (!serialPort.IsOpen) serialPort.Open();
                        Logger.InfoFormat("Device Arrival SerialPort Name:{0}", port.dbcp_name);
                    }
                    if (dbt == DeviceBroadcastType.DBT_DEVICEREMOVECOMPLETE)
                    {
                        if (serialPort.IsOpen) serialPort.Close();
                        Logger.ErrorFormat("Device Remove Complete SerialPort Name:{0}", port.dbcp_name);
                    }
                    handled = true;
                }

                return IntPtr.Zero;
            });

            window.Closing += (s, e) =>
            {
                hwndSource?.Dispose();
                hwndSource = null;
            };
        }
#endif

        /// <summary>
        /// 获取当前计算机的 串行端口 完整名称 的数组
        /// <para>与 <see cref="System.IO.Ports.SerialPort.GetPortNames"/> 函数不同，<see cref="System.IO.Ports.SerialPort.GetPortNames"/> 只输出类似"COM3,COM4,COMn"；该函数输出串口对象的名称或是驱动名，类似："USB Serial Port (COM3)" ... </para>
        /// <para>这只是 WMI 示例应用函数，用于查询 串口名称 信息。更多应用参考 WMI。</para>
        /// </summary>
        /// <returns></returns>
        public static string[] GetPortNames()
        {
            String query = "SELECT Name FROM Win32_PnPEntity WHERE PNPClass='Ports' AND (Name LIKE '%(COM_)' OR Name LIKE '%(COM__)')";

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                var names = from ManagementObject obj in searcher.Get()
                            from PropertyData pd in obj.Properties
                            where !string.IsNullOrWhiteSpace(pd.Name) && pd.Value != null
                            select pd.Value.ToString();

                return names.ToArray();
            }
        }

        /// <summary>
        /// 获取当前计算机的 串行端口 完整名称 的数组
        /// <para><see cref="GetPortNames"/> 的异步方法</para>
        /// <para>这只是 WMI 示例应用函数，用于查询 串口名称 信息。更多应用参考 WMI。</para>
        /// </summary>
        /// <returns></returns>
        public static async Task<string[]> GetPortNamesAsync()
        {
            return await Task.Run<string[]>(() => GetPortNamesAsync());
        }
    }
}

