using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using SpaceCG.Generic;
using SpaceCG.Module;

namespace SpaceCG.ModbusExtension
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

        private IEnumerable<XElement> EventElements { get; set; } = null;

        /// <summary>
        /// Transport Devices 列表
        /// </summary>
        internal List<ModbusTransportDevice> TransportDevices { get; private set; } = new List<ModbusTransportDevice>(8);

        /// <summary>
        /// 可通过反射技术访问的对象列表
        /// </summary>
        public ConcurrentDictionary<String, IDisposable> AccessObjects { get; private set; } = new ConcurrentDictionary<String, IDisposable>();

        /// <summary>
        /// Modbus 设备管理对象
        /// </summary>
        /// <param name="accessName">当前对象可通过反射技术访问的名称</param>
        public ModbusDeviceManager(String accessName = nameof(ModbusDeviceManager))
        {
            this.Name = accessName;

            if (!String.IsNullOrWhiteSpace(Name))
                AddAccessObject(Name, this);
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
#if false
            //Reset Clear Handler
            foreach (var modbus in TransportDevices)
            {
                if (AccessObjects.ContainsKey(modbus.Name))
                    AccessObjects[modbus.Name] = modbus.Master;
                else
                    AccessObjects.TryAdd(modbus.Name, modbus.Master);

                modbus.StopTransport();
            }
            TransportDevices.Clear();
#endif
            Configuration = XElement.Load(configFile);

            IEnumerable<XElement> ConnectElements = Configuration.Element("Connections")?.Elements("Connection");

            ParseModbusDevicesConfig(Configuration.Elements("Modbus"));

        }

        private void ParseConnectionsConfig(IEnumerable<XElement> connectionsElement)
        {

        }

        private void ParseModbusDevicesConfig(IEnumerable<XElement> modbusElements)
        {
            if (modbusElements?.Count() == null) return;

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

        private void Transport_OutputChangeEvent(ModbusTransportDevice transportDevice, byte slaveAddress, Register register)
        {
            Console.WriteLine(register);
            if (register.Type == RegisterType.CoilsStatus || register.Type == RegisterType.DiscreteInput)
                Console.WriteLine($"OutputChange {transportDevice} slaveAddress:{slaveAddress}, registerAddress:{register.Address}, newValue:{Convert.ToString((int)register.Value, 2)}, oldValue:{register.LastValue}");
        }

        private void Transport_InputChangeEvent(ModbusTransportDevice transportDevice, byte slaveAddress, Register register)
        {
            Console.WriteLine(register);
            Console.WriteLine($"InputChange {transportDevice} slaveAddress:{slaveAddress}, registerAddress:{register.Address}, newValue:{Convert.ToString((int)register.Value, 2)}, oldValue:{register.LastValue}");
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

        internal void InputOutputChange(String transportName, string eventName, byte slaveAddress, ushort registerAddress, ushort value)
        {
            if (EventElements?.Count() <= 0) return;

            var events = from evt in EventElements
                         where evt.Attribute("Name")?.Value == eventName && evt.Attribute("SlaveAddress")?.Value == slaveAddress.ToString()
                         select evt;
        }

        /// <summary>
        /// 接收网络消息并处理
        /// </summary>
        /// <param name="message"></param>
        internal void ReceiveNetworkMessageHandler(String message)
        {
            if (String.IsNullOrWhiteSpace(message)) return;
            Log.Info($"Receive Network Message: {message}");

#if false
            String[] args = message.Split(',');
            if (args.Length != 5) return;
            InputOutputChange(args[0], args[1], Convert.ToByte(args[2]), Convert.ToUInt16(args[3]), Convert.ToUInt16(args[4]));
#endif

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
                Task.Run(() =>
                {
                    this.CallActionElement(element);
                });
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
            if (action?.Attribute("TargetKey") == null || action?.Attribute("Method") == null) return;

            if (action.Attribute("TargetKey").Value == "Thread" && action.Attribute("Method").Value == "Sleep")
            {
                if (int.TryParse(action.Attribute("Params").Value, out int millisecondsTimeout))
                {
                    Thread.Sleep(millisecondsTimeout);
                    return;
                }
            }

            String objKey = action.Attribute("TargetKey")?.Value;
            if (!AccessObjects.TryGetValue(objKey, out IDisposable targetObj))
            {
                Log.Warn($"未找到时目标对象实例 {objKey} ");
                return;
            }

            //Method
            String methodName = action.Attribute("Method")?.Value;
            if (String.IsNullOrWhiteSpace(methodName))
            {
                if (Log.IsDebugEnabled) Log.Debug($"目标对象实例 {objKey}, 的方法名不能为空");
                return;
            }

            Task.Run(() =>
            {
                //object[] objs = StringExtension.ConvertParameters(action.Attribute("Params").Value);
                //foreach (object obj in objs) Console.WriteLine($"{obj.GetType()},{obj}");
                //StringExtension.ConvertParameters(action.Attribute("Params").Value);

                if (!String.IsNullOrWhiteSpace(action.Attribute("Params")?.Value))
                    InstanceExtension.CallInstanceMethod(targetObj, methodName, StringExtension.ConvertParameters(action.Attribute("Params").Value));
                else
                    InstanceExtension.CallInstanceMethod(targetObj, methodName);
            });
        }

        /// <inheritdoc/>
        public void Dispose()
        {

        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(ModbusDeviceManager)}] Name:{Name}";
        }
    }
}
