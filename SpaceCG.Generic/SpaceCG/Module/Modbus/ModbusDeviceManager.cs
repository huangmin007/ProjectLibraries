using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using HPSocket;
using SpaceCG.Extensions;

namespace SpaceCG.Module.Modbus
{
    /// <summary>
    /// Modbus Device Manager 
    /// <para>遵循自定义的 XML 模型</para>
    /// </summary>
    public class ModbusDeviceManager : IDisposable
    {
        protected static readonly log4net.ILog Log = log4net.LogManager.GetLogger(nameof(ModbusDeviceManager));

        /// <summary>
        /// Name
        /// </summary>
        public String Name { get; private set; } = null;

        /// <summary>
        /// Root Config Items 
        /// </summary>
        private XElement Configuration { get; set; } = null;
        private IEnumerable<XElement> ModbusElements { get; set; } = null;

        HPSocket.IServer HPTcpServer;
        HPSocket.IServer HPUdpServer;

        /// <summary>
        /// Transport Devices 列表
        /// </summary>
        private List<ModbusTransportDevice> TransportDevices = new List<ModbusTransportDevice>(8);

        /// <summary>
        /// 可通过反射技术访问的对象列表
        /// </summary>
        private ConcurrentDictionary<String, IDisposable> AccessObjects = new ConcurrentDictionary<String, IDisposable>();

        /// <summary>
        /// Modbus 设备管理对象
        /// </summary>
        /// <param name="accessName">当前对象可通过反射技术访问的名称</param>
        public ModbusDeviceManager(ushort localPort = 2023, String accessName = nameof(ModbusDeviceManager))
        {
            this.Name = accessName;
            if(localPort >= 1024)
            {
                HPTcpServer = HPSocketExtensions.CreateNetworkServer<HPSocket.Tcp.TcpServer>("0.0.0.0", localPort, OnServerReceiveEventHandler);
                HPUdpServer = HPSocketExtensions.CreateNetworkServer<HPSocket.Udp.UdpServer>("0.0.0.0", localPort, OnServerReceiveEventHandler);
            }
        }

        private HandleResult OnServerReceiveEventHandler(IServer sender, IntPtr connId, byte[] data)
        {
            String message = Encoding.UTF8.GetString(data);
            ReceiveNetworkMessageHandler(message);

            return HandleResult.Ok; 
        }

        /// <summary>
        /// 添加可访问对象
        /// </summary>
        /// <param name="name"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool AddAccessObject(string name, IDisposable obj)
        {
            if (String.IsNullOrWhiteSpace(name) || obj == null) return false;

            if (AccessObjects.ContainsKey(name))
            {
                Log.Warn($"添加可访问对象 {name}:{obj} 失败");
                return false;
            }
            return AccessObjects.TryAdd(name, obj);
        }

        /// <summary>
        /// 添加传输总线设备
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public bool AddTransportDevice(ModbusTransportDevice device)
        {
            foreach (ModbusTransportDevice td in TransportDevices)
                if (td.Name == device.Name) return false;

            TransportDevices.Add(device);
            return true;
        }
        /// <summary>
        /// 移除传输总线设备
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ModbusTransportDevice GetTransportDevice(String name)
        {
            foreach (ModbusTransportDevice td in TransportDevices)
                if (td.Name == name) return td;

            return null;
        }

        /// <summary>
        /// 加载设备配置文件
        /// </summary>
        /// <param name="configFile"></param>
        public void LoadDeviceConfig(String configFile)
        {
            if (!File.Exists(configFile))
            {
                Log.Error($"指定的配置文件不存在 {configFile}, 禁用 Modbus Device Manager 模块.");
                return;
            }

            ResetAndClear();

            Configuration = XElement.Load(configFile);
            ModbusElements = Configuration.Elements("Modbus"); 
            
            ParseConnectionsConfig(Configuration.Descendants("Connection"));            
            ParseModbusDevicesConfig(Configuration.Elements("Modbus"));

            if (!String.IsNullOrWhiteSpace(Name)) AddAccessObject(Name, this);

            //Initialize
            foreach (ModbusTransportDevice transport in TransportDevices)
            {
                CallEventName(transport.Name, "Initialize");
                Thread.Sleep(128);
            }
        }
        /// <summary>
        /// 解析 Connections 节点配置
        /// </summary>
        /// <param name="connectionsElement"></param>
        private void ParseConnectionsConfig(IEnumerable<XElement> connectionsElement)
        {
            if (connectionsElement?.Count() <= 0) return;
            foreach(XElement connection in connectionsElement)
            {
                String name = connection.Attribute("Name")?.Value;
                String type = connection.Attribute("Type")?.Value;
                String parameters = connection.Attribute("Parameters")?.Value;

                if (String.IsNullOrWhiteSpace(name) ||
                    String.IsNullOrWhiteSpace(type) ||
                    String.IsNullOrWhiteSpace(parameters)) continue;

                String[] args = parameters.Split(',');
                if (args.Length != 3 || !int.TryParse(args[2], out int port)) continue;

                switch(type.ToUpper())
                {
                    case "SERIAL":
                        AddAccessObject(name, new SerialPort(args[1], port));
                        break;

                    case "MODBUS":
                        AddAccessObject(name, NModbus4Extensions.CreateNModbus4Master(args[0], args[1], port));
                        break;

                    case "SERVER":
                        if (args[0].ToUpper() == "TCP")
                            AddAccessObject(name, HPSocketExtensions.CreateNetworkServer<HPSocket.Tcp.TcpServer>(args[1], (ushort)port, null));
                        else if (args[0].ToUpper() == "UDP")
                            AddAccessObject(name, HPSocketExtensions.CreateNetworkServer<HPSocket.Udp.UdpServer>(args[1], (ushort)port, null));
                        else
                            Log.Warn($"连接参数错误：{name},{type},{parameters}");
                        break;

                    case "CLIENT":
                        if (args[0].ToUpper() == "TCP")
                            AddAccessObject(name, HPSocketExtensions.CreateNetworkClient<HPSocket.Tcp.TcpClient>(args[1], (ushort)port, null));
                        else if (args[0].ToUpper() == "UDP")
                            AddAccessObject(name, HPSocketExtensions.CreateNetworkClient<HPSocket.Udp.UdpClient>(args[1], (ushort)port, null));
                        else
                            Log.Warn($"连接参数错误：{name},{type},{parameters}");
                        break;
                }
            }
        }
        /// <summary>
        /// 解析 Modbus 节点配置
        /// </summary>
        /// <param name="modbusElements"></param>
        private void ParseModbusDevicesConfig(IEnumerable<XElement> modbusElements)
        {
            if (modbusElements?.Count() <= 0) return;

            foreach(XElement modbusElement in modbusElements)
            {
                if (ModbusTransportDevice.TryParse(modbusElement, out ModbusTransportDevice transport) && AddTransportDevice(transport))
                {
                    transport.StartTransport();
                    transport.InputChangeEvent += Transport_InputChangeEvent;
                    transport.OutputChangeEvent += Transport_OutputChangeEvent;

                    AddAccessObject(transport.Name, transport);
                }
                else
                {
                    Log.Warn($"解析/添加 传输总线 设备失败");
                }
            }
        }
        /// <summary>
        /// 总线 Input 事件处理
        /// </summary>
        /// <param name="transportDevice"></param>
        /// <param name="ioDevice"></param>
        /// <param name="register"></param>
        private void Transport_OutputChangeEvent(ModbusTransportDevice transportDevice, ModbusIODevice ioDevice, Register register)
        {
            InputOutputEventHandler("OutputChange", transportDevice.Name, ioDevice.Address, register);
        }
        /// <summary>
        /// 总线 Output 事件处理
        /// </summary>
        /// <param name="transportDevice"></param>
        /// <param name="ioDevice"></param>
        /// <param name="register"></param>
        private void Transport_InputChangeEvent(ModbusTransportDevice transportDevice, ModbusIODevice ioDevice, Register register)
        {
            InputOutputEventHandler("InputChange", transportDevice.Name, ioDevice.Address, register);
        }

        /// <summary>
        /// 总线输入输出事件处理
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="transportName"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="register"></param>
        internal void InputOutputEventHandler(string eventType, String transportName, byte slaveAddress, Register register)
        {
            if (ModbusElements?.Count() <= 0) return;

            if(Log.IsDebugEnabled)
                Log.Debug($"{eventType} {transportName} > 0x{slaveAddress:X2} > #{register.Address:X4} > {register.Type} > {register.Value}");

            foreach (XElement modbus in ModbusElements)
            {
                if (modbus.Attribute("Name")?.Value != transportName) continue;

                IEnumerable<XElement> events = modbus.Descendants("Event");
                foreach (XElement evt in events)
                {
                    if (evt.Attribute("Type")?.Value != eventType) continue;
                    
                    if (!StringExtensions.TryParse(evt.Attribute("DeviceAddress")?.Value, out byte deviceAddress)) continue;
                    if (deviceAddress != slaveAddress) continue;
                    
                    if (!StringExtensions.TryParse(evt.Attribute($"{register.Type}Address")?.Value, out ushort regAddress)) continue;
                    if (regAddress != register.Address) continue;

                    if (StringExtensions.TryParse(evt.Attribute("Value")?.Value, out ulong regValue) && regValue == register.Value)
                    {
                        if (Log.IsInfoEnabled)
                            Log.Info($"{eventType} {transportName} > 0x{slaveAddress:X2} > #{register.Address:X4} > {register.Type} > {register.Value}");

                        IEnumerable<XElement> actions = evt.Elements("Action");
                        foreach (XElement action in actions) CallActionElement(action);
                        continue;
                    }
                    else if(StringExtensions.TryParse(evt.Attribute("MinValue")?.Value, out ulong minValue) && StringExtensions.TryParse(evt.Attribute("MaxValue")?.Value, out ulong maxValue))
                    {
                        if (maxValue > minValue && register.Value <= maxValue && register.Value >= minValue && (register.LastValue < minValue || register.LastValue > maxValue))
                        {
                            if (Log.IsInfoEnabled)
                                Log.Info($"{eventType} {transportName} > 0x{slaveAddress:X2} > #{register.Address:X4} > {register.Type} > {register.Value}");

                            IEnumerable<XElement> actions = evt.Elements("Action");
                            foreach (XElement action in actions) CallActionElement(action);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 调用配置事件，外部调用
        /// </summary>
        /// <param name="transportName"></param>
        /// <param name="eventName"></param>
        public void CallEventName(String transportName, String eventName)
        {
            foreach (XElement modbus in ModbusElements)
            {
                if (modbus.Attribute("Name")?.Value != transportName) continue;

                IEnumerable<XElement> events = modbus.Descendants("Event");
                foreach (XElement evt in events)
                {
                    if (evt.Attribute("Name")?.Value == eventName)
                    {
                        IEnumerable<XElement> actions = evt.Elements("Action");
                        foreach (XElement action in actions) CallActionElement(action);
                    }
                }
            }
        }

        /// <summary>
        /// 接收网络消息并处理
        /// </summary>
        /// <param name="message"></param>
        internal void ReceiveNetworkMessageHandler(String message)
        {
            if (String.IsNullOrWhiteSpace(message)) return;
            Log.Info($"Receive Network Message: {message}");

            XElement element = null;

            try
            {
                element = XElement.Parse(message);
            }
            catch (Exception ex)
            {
                Log.Error($"数据解析错误：{ex}");
                return;
            }

            if (element.Name?.LocalName != "Action") return;

            try
            {
                this.CallActionElement(element);
            }
            catch (Exception ex)
            {
                Log.Error($"执行网络消息错误：{ex}");
            }
        }
        /// <summary>
        /// 分析/调用 Action 配置节点 
        /// </summary>
        /// <param name="action"></param>
        internal void CallActionElement(XElement action)
        {
            if (action == null || action.Name != "Action") return;
            if (String.IsNullOrWhiteSpace(action.Attribute("TargetName")?.Value) ||
                String.IsNullOrWhiteSpace(action.Attribute("Method")?.Value)) return;

            String key = action.Attribute("TargetName").Value;
            String methodName = action.Attribute("Method").Value;

            if (!AccessObjects.TryGetValue(key, out IDisposable targetObj))
            {
                Log.Warn($"未找到时目标对象实例 {key} ");
                return;
            }

            Task.Run(() =>
            {
                InstanceExtensions.CallInstanceMethod(targetObj, methodName, StringExtensions.SplitParameters(action.Attribute("Params")?.Value));
            });
        }

        /// <summary>
        /// Reset And Clear
        /// </summary>
        private void ResetAndClear()
        {
            foreach (ModbusTransportDevice transport in TransportDevices)
            {
                CallEventName(transport.Name, "Dispose");
                Thread.Sleep(128);

                transport.StopTransport();
            }
            foreach (KeyValuePair<String, IDisposable> obj in AccessObjects)
            {
                if (obj.Key == this.Name || obj.Value == this) continue;
                try
                {
                    obj.Value.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Warn(ex);
                    continue;
                }
            }

            AccessObjects.Clear();
            TransportDevices.Clear();

            Configuration = null;
            ModbusElements = null;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            ResetAndClear();

            AccessObjects = null;
            TransportDevices = null;

            if(HPTcpServer != null)
            {
                List<IntPtr> clients = HPTcpServer.GetAllConnectionIds();
                foreach (IntPtr client in clients)
                    HPTcpServer.Disconnect(client, true);
                HPTcpServer.Dispose();
                HPTcpServer = null;
            }
            if (HPUdpServer != null)
            {
                List<IntPtr> clients = HPUdpServer.GetAllConnectionIds();
                foreach (IntPtr client in clients)
                    HPUdpServer.Disconnect(client, true);
                HPUdpServer.Dispose();
                HPUdpServer = null;
            }
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(ModbusDeviceManager)}] Name:{Name}";
        }
    }
}
