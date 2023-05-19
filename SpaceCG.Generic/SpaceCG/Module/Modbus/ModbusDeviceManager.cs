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
using SpaceCG.Extensions;
using SpaceCG.Generic;
using SpaceCG.Net;

namespace SpaceCG.Module.Modbus
{
    /// <summary>
    /// Modbus Device Manager 
    /// <para>遵循自定义的 XML 模型，参考 ModbusDevices.Config</para>
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

        /// <summary>
        /// 控制接口对象
        /// </summary>
        public ControllerInterface ControlInterface { get; private set; } = new ControllerInterface(0);

        /// <summary>
        /// Transport Devices 列表
        /// </summary>
        private List<ModbusTransportDevice> TransportDevices = new List<ModbusTransportDevice>(8);

        /// <summary>
        /// Modbus 设备管理对象
        /// </summary>
        public ModbusDeviceManager()
        {
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
        /// 加载设备配置文件，配置文件参考 ModbusDevices.Config
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

            ParseRootAttributes();
            ParseConnectionsConfig(Configuration.Descendants("Connection"));            
            ParseModbusDevicesConfig(Configuration.Elements("Modbus"));

            //Initialize
            foreach (ModbusTransportDevice transport in TransportDevices)
            {
                CallEventName(transport.Name, "Initialize");
                Thread.Sleep(128);
            }
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
                if (!String.IsNullOrWhiteSpace(Name)) ControlInterface.AddControlObject(Name, this);
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

                switch(type.ToUpper().Replace(" ", ""))
                {
                    case "SERIAL":
                        ControlInterface.AddControlObject(name, new SerialPort(args[1], port));
                        break;

                    case "MODBUS":
                        ControlInterface.AddControlObject(name, NModbus4Extensions.CreateNModbus4Master(args[0], args[1], port));
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
                                Log.Warn($"连接参数错误：{name},{type},{parameters}");

                            if (Server != null && Server.Start())
                                ControlInterface.AddControlObject(name, Server);
                        }
                        catch(Exception ex)
                        {
                            Log.Error($"创建服务端 {args} 错误：{ex}");
                        }
                        break;

                    case "CLIENT":
                        try
                        {
                            IAsyncClient Client = null;
                            if (args[0].ToUpper() == "TCP")
                                Client = new AsyncTcpClient();
                            else if (args[0].ToUpper() == "UDP")
                                Client = new AsyncUdpClient();
                            else
                                Log.Warn($"连接参数错误：{name},{type},{parameters}");

                            if (Client != null && Client.Connect(args[1], (ushort)port))
                                ControlInterface.AddControlObject(name, Client);
                        }
                        catch(Exception ex)
                        {
                            Log.Error($"创建客户端 {args} 错误：{ex}");
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
                if (ModbusTransportDevice.TryParse(modbusElement, out ModbusTransportDevice transport) && AddTransportDevice(transport))
                {
                    transport.StartTransport();
                    transport.InputChangeEvent += Transport_InputChangeEvent;
                    transport.OutputChangeEvent += Transport_OutputChangeEvent;

                    ControlInterface.AddControlObject(transport.Name, transport);
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
                        foreach (XElement action in actions) ControlInterface.TryParseCallMethod(action, out object result);
                        continue;
                    }
                    else if(StringExtensions.TryParse(evt.Attribute("MinValue")?.Value, out ulong minValue) && StringExtensions.TryParse(evt.Attribute("MaxValue")?.Value, out ulong maxValue))
                    {
                        if (maxValue > minValue && register.Value <= maxValue && register.Value >= minValue && (register.LastValue < minValue || register.LastValue > maxValue))
                        {
                            if (Log.IsInfoEnabled)
                                Log.Info($"{eventType} {transportName} > 0x{slaveAddress:X2} > #{register.Address:X4} > {register.Type} > {register.Value}");

                            IEnumerable<XElement> actions = evt.Elements("Action");
                            foreach (XElement action in actions) ControlInterface.TryParseCallMethod(action, out object result);
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
                        foreach (XElement action in actions) ControlInterface.TryParseCallMethod(action, out object result);
                    }
                }
            }
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

            Type DisposableType = typeof(IDisposable);
            foreach (KeyValuePair<String, Object> obj in ControlInterface.GetControlObjects())
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
                    Log.Warn(ex);
                    continue;
                }
            }

            TransportDevices.Clear();
            ControlInterface.RemoveControlObjects();
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
            return $"[{nameof(ModbusDeviceManager)}] Name:{Name}";
        }
    }
}
