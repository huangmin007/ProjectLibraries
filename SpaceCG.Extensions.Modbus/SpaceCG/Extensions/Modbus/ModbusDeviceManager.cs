using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using SpaceCG.Generic;
using SpaceCG.Net;

namespace SpaceCG.Extensions.Modbus
{
    /// <summary>
    /// Modbus Device Manager CallEventName
    /// <para>遵循自定义的 XML 模型，参考 ModbusDevices.Config</para>
    /// </summary>
    public class ModbusDeviceManager : IDisposable
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(ModbusDeviceManager));

        /// <summary> <see cref="XDisposed"/> Name </summary>
        public const string XDisposed = "Disposed";
        /// <summary> <see cref="XInitialized"/> Name </summary>
        public const string XInitialized = "Initialized";
        /// <summary> <see cref="XInputChange"/> Name </summary>
        public const string XInputChange = "InputChange";
        /// <summary> <see cref="XOutputChange"/> Name </summary>
        public const string XOutputChange = "OutputChange";

        /// <summary> <see cref="XMinValue"/> Name </summary>
        public const string XMinValue = "MinValue";
        /// <summary> <see cref="XMaxValue"/> Name </summary>
        public const string XMaxValue = "MaxValue";
        /// <summary> <see cref="XDeviceAddress"/> Name </summary>
        public const string XDeviceAddress = "DeviceAddress";

        /// <summary>
        /// Name
        /// </summary>
        public String Name { get; private set; } = null;

        /// <summary>
        /// Root Config Items 
        /// </summary>
        private XElement Configuration { get; set; } = null;
        private IEnumerable<XElement> ModbusElements { get; set; } = null;

        /// <summary>
        /// 控制接口对象
        /// </summary>
        public ControlInterface ControlInterface { get; private set; } = new ControlInterface(0);

        /// <summary>
        /// Transport Devices 列表
        /// </summary>
        private List<ModbusTransport> TransportDevices { get; set; } = new List<ModbusTransport>(8);

        /// <summary>
        /// Modbus 设备管理对象
        /// </summary>
        public ModbusDeviceManager()
        {
        }

#if true
        /// <summary>
        /// 添加传输总线设备
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public bool AddTransportDevice(ModbusTransport device)
        {
            foreach (ModbusTransport td in TransportDevices)
                if (td.Name == device.Name) return false;

            TransportDevices.Add(device);
            return true;
        }
        /// <summary>
        /// 移除传输总线设备
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ModbusTransport GetTransportDevice(String name)
        {
            foreach (ModbusTransport td in TransportDevices)
                if (td.Name == name) return td;

            return null;
        }
#endif
        /// <summary>
        /// 加载设备配置文件，配置文件参考 ModbusDevices.Config
        /// </summary>
        /// <param name="configFile"></param>
        public void LoadDeviceConfig(String configFile)
        {
            if (!File.Exists(configFile))
            {
                Logger.Error($"指定的配置文件不存在 {configFile}, 禁用 Modbus Device Manager 模块.");
                return;
            }

            ResetAndClear();

            Configuration = XElement.Load(configFile);
            ModbusElements = Configuration.Elements("Modbus");

            ParseRootAttributes();
            ParseConnectionsConfig(Configuration.Descendants("Connection"));            
            ParseModbusDevicesConfig(Configuration.Elements("Modbus"));

            //XInitialized
            CallEventType(XInitialized, null);
            Thread.Sleep(100);
        }

        /// <summary>
        /// 解析配置的 Root 节点的属性值
        /// </summary>
        private void ParseRootAttributes()
        {
            if (String.IsNullOrWhiteSpace(Configuration?.Attribute("Name")?.Value) || 
                String.IsNullOrWhiteSpace(Configuration?.Attribute("LocalPort")?.Value)) return;

            if (ushort.TryParse(Configuration?.Attribute("LocalPort")?.Value, out ushort localPort) && localPort >= 1024)
            {
                ControlInterface.InstallNetworkService(localPort, "0.0.0.0");

                this.Name = Configuration.Attribute("Name").Value;
                if (!String.IsNullOrWhiteSpace(Name)) ControlInterface.AccessObjects.Add(Name, this);
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
                String name = connection.Attribute(ControlInterface.XName)?.Value;
                String type = connection.Attribute(ControlInterface.XType)?.Value;
                String parameters = connection.Attribute("Parameters")?.Value;

                if (String.IsNullOrWhiteSpace(name) ||
                    String.IsNullOrWhiteSpace(type) ||
                    String.IsNullOrWhiteSpace(parameters)) continue;

                String[] args = parameters.Split(',');
                if (args.Length != 3 || !int.TryParse(args[2], out int port)) continue;

                switch(type.ToUpper().Replace(" ", ""))
                {
                    case "SERIAL":
                        ControlInterface.AccessObjects.Add(name, new SerialPort(args[1], port));
                        break;

                    case "MODBUS":
                        ControlInterface.AccessObjects.Add(name, NModbus4Extensions.CreateNModbus4Master(args[0], args[1], port));
                        break;

                    case "SERVER":
                        try
                        {
                            IAsyncServer Server = null;
                            if (args[0].ToUpper() == "TCP")
                                Server = new AsyncTcpServer((ushort)port);
                            else if (args[0].ToUpper() == "UDP")
                                Server = new AsyncUdpServer((ushort)port);
                            else
                                Logger.Warn($"连接参数错误：{name},{type},{parameters}");

                            if (Server != null && Server.Start())
                                ControlInterface.AccessObjects.Add(name, Server);
                        }
                        catch(Exception ex)
                        {
                            Logger.Error($"创建服务端 {args} 错误：{ex}");
                        }
                        break;

                    case "CLIENT":
                        try
                        {
                            IAsyncClient Client = null;
                            if (args[0].ToUpper() == "TCP")
                            {
                                Client = new AsyncTcpClient();
                                Client.Disconnected += (s, e) => Client.Connect(args[1], (ushort)port);
                            }
                            else if (args[0].ToUpper() == "UDP")
                                Client = new AsyncUdpClient();
                            else
                                Logger.Warn($"连接参数错误：{name},{type},{parameters}");

                            if (Client != null && Client.Connect(args[1], (ushort)port))
                                ControlInterface.AccessObjects.Add(name, Client);
                        }
                        catch(Exception ex)
                        {
                            Logger.Error($"创建客户端 {args} 错误：{ex}");
                        }
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
                if (ModbusTransport.TryParse(modbusElement, out ModbusTransport transport) && AddTransportDevice(transport))
                {
                    transport.StartTransport();
                    transport.InputChangeEvent += Transport_InputChangeEvent;
                    transport.OutputChangeEvent += Transport_OutputChangeEvent;

                    ControlInterface.AccessObjects.Add(transport.Name, transport);
                }
                else
                {
                    Logger.Warn($"解析/添加 传输总线 设备失败");
                }
            }
        }

        /// <summary>
        /// 总线 Input 事件处理
        /// </summary>
        /// <param name="transportDevice"></param>
        /// <param name="ioDevice"></param>
        /// <param name="register"></param>
        private void Transport_OutputChangeEvent(ModbusTransport transportDevice, ModbusIODevice ioDevice, Register register)
        {
            CallEventType(XOutputChange, transportDevice.Name, ioDevice.Address, register);
        }
        /// <summary>
        /// 总线 Output 事件处理
        /// </summary>
        /// <param name="transportDevice"></param>
        /// <param name="ioDevice"></param>
        /// <param name="register"></param>
        private void Transport_InputChangeEvent(ModbusTransport transportDevice, ModbusIODevice ioDevice, Register register)
        {
            CallEventType(XInputChange, transportDevice.Name, ioDevice.Address, register);
        }

        /// <summary>
        /// 总线输入输出事件处理
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="transportName"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="register"></param>
        protected void CallEventType(string eventType, string transportName, byte slaveAddress, Register register)
        {
            if(Logger.IsDebugEnabled)
                Logger.Debug($"{eventType} {transportName} > 0x{slaveAddress:X2} > #{register.Address:X4} > {register.Type} > {register.Value}");

            foreach (XElement modbus in ModbusElements)
            {
                if (modbus.Attribute(ControlInterface.XName)?.Value != transportName) continue;

                IEnumerable<XElement> events = modbus.Descendants(ControlInterface.XEvent);
                foreach (XElement evt in events)
                {
                    if (evt.Attribute(ControlInterface.XType)?.Value != eventType) continue;
                    
                    if (!StringExtensions.TryParse(evt.Attribute(XDeviceAddress)?.Value, out byte deviceAddress)) continue;
                    if (deviceAddress != slaveAddress) continue;
                    
                    if (!StringExtensions.TryParse(evt.Attribute($"{register.Type}Address")?.Value, out ushort regAddress)) continue;
                    if (regAddress != register.Address) continue;

                    if (StringExtensions.TryParse(evt.Attribute(ControlInterface.XValue)?.Value, out long regValue) && regValue == register.Value)
                    {
                        if (Logger.IsInfoEnabled)
                            Logger.Info($"{eventType} {transportName} > 0x{slaveAddress:X2} > #{register.Address:X4} > {register.Type} > {register.Value}");

                        IEnumerable<XElement> actions = evt.Elements(ControlInterface.XAction);
                        foreach (XElement action in actions) ControlInterface.TryParseCallMethod(action, out object result);
                        continue;
                    }
                    else if(StringExtensions.TryParse(evt.Attribute(XMinValue)?.Value, out long minValue) && StringExtensions.TryParse(evt.Attribute(XMaxValue)?.Value, out long maxValue))
                    {
                        if (maxValue > minValue && register.Value <= maxValue && register.Value >= minValue && (register.LastValue < minValue || register.LastValue > maxValue))
                        {
                            if (Logger.IsInfoEnabled)
                                Logger.Info($"{eventType} {transportName} > 0x{slaveAddress:X2} > #{register.Address:X4} > {register.Type} > {register.Value}");

                            IEnumerable<XElement> actions = evt.Elements(ControlInterface.XAction);
                            foreach (XElement action in actions) ControlInterface.TryParseCallMethod(action, out object result);
                        }
                    }
                }//End for                
            }//End for
        }

        /// <summary>
        /// Call Event Type
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="transportName"></param>
        public void CallEventType(string eventType, string transportName)
        {
            IEnumerable<XElement> events;
            if (string.IsNullOrWhiteSpace(transportName))
            {
                events = from evt in ModbusElements.Descendants(ControlInterface.XEvent)
                         where evt.Attribute(ControlInterface.XType)?.Value == eventType
                         select evt;
            }
            else
            {
                events = from modbus in ModbusElements
                         where modbus.Attribute(ControlInterface.XName)?.Value == transportName
                         from evt in modbus.Descendants(ControlInterface.XEvent)
                         where evt.Attribute(ControlInterface.XType)?.Value == eventType
                         select evt;
            }

            if (events?.Count() <= 0) return;
            foreach (XElement evt in events)
            {
                ControlInterface.TryParseControlMessage(evt, out object returnResult);                
            }
        }

        /// <summary>
        /// 跟据事件名称，modbus名称，调用配置事件
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="transportName"></param>
        public void CallEventName(string eventName, string transportName)
        {
            IEnumerable<XElement> events = from modbus in ModbusElements
                                           where modbus.Attribute(ControlInterface.XName)?.Value == transportName
                                           from evt in modbus.Descendants(ControlInterface.XEvent)
                                           where evt.Attribute(ControlInterface.XName)?.Value == eventName
                                           select evt;

            foreach (XElement element in events.Elements())
            {
                if (element.Name.LocalName == ControlInterface.XAction)
                    ControlInterface.TryParseControlMessage(element, out object result);
            }
        }
        /// <summary>
        /// 跟据事件名称，调用事件
        /// </summary>
        /// <param name="eventName"></param>
        public void CallEventName(string eventName)
        {
            IEnumerable<XElement> events = from evt in ModbusElements.Descendants(ControlInterface.XEvent)
                                           where evt.Attribute(ControlInterface.XName)?.Value == eventName
                                           select evt;

            foreach (XElement element in events.Elements())
            {
                if(element.Name.LocalName == ControlInterface.XAction)
                    ControlInterface.TryParseControlMessage(element, out object result);
            }
        }

        /// <summary>
        /// Reset And Clear
        /// </summary>
        private void ResetAndClear()
        {
            CallEventType(XDisposed, null);
            Thread.Sleep(100);

            foreach (ModbusTransport transport in TransportDevices)
            {
                transport.StopTransport();
            }

            Type DisposableType = typeof(IDisposable);
            foreach (KeyValuePair<String, Object> obj in ControlInterface.AccessObjects)
            {
                if (obj.Key == this.Name || obj.Value == this) continue;

                try
                {
                    if (obj.Value != null && DisposableType.IsAssignableFrom(obj.Value.GetType()))
                    {
                        ((IDisposable)(obj.Value))?.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                    continue;
                }
            }

            TransportDevices.Clear();
            ControlInterface.AccessObjects.Clear();
            ControlInterface.UninstallNetworkServices();

            Configuration = null;
            ModbusElements = null;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            ResetAndClear();

            ControlInterface?.Dispose();
            ControlInterface = null;
            TransportDevices = null;
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(ModbusDeviceManager)}] {nameof(Name)}:{Name}";
        }
    }
}
