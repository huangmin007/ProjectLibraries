using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        public ModbusDeviceManager(String accessName = null)
        {
            this.Name = accessName;

            if (!String.IsNullOrWhiteSpace(Name))
                TryAddAccessObject(Name, this);
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

            IEnumerable<XElement> ModbusElements = Configuration.Elements("Modbus");
        }

        private void ParseConnectionConfig(XElement element)
        {

        }

        private void ParseModbusConfig(XElement element)
        {
            IEnumerable<XElement> devicesElements = element.Elements("Device");
            foreach(XElement deviceElement in devicesElements)
            {

            }
        }

        /// <summary>
        /// 添加传输设备
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
        public ModbusTransportDevice GetTransportDevice(String name)
        {
            foreach (ModbusTransportDevice td in TransportDevices)
                if (td.Name == name) return td;

            return null;
        }

        /// <summary>
        /// 添加可访问对象
        /// </summary>
        /// <param name="name"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool TryAddAccessObject(string name, IDisposable obj)
        {
            if (String.IsNullOrWhiteSpace(name) || obj == null) return false;

            if (AccessObjects.ContainsKey(name))
            {
                Log.Warn($"添加可访问对象 {name}:{obj} 失败");
                return false;
            }
            return AccessObjects.TryAdd(name, obj);
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
