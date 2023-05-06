using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace SpaceCG.Generic
{
    public static partial class InstanceExtension
    {
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
            SetInstancePropertyValue(serialPort, nameSpace);

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
            RemoveInstanceEvents(serialPort);

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

