using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Modbus.Device;
using SpaceCG.Generic;

namespace SpaceCG.Extensions.Modbus
{
    /// <summary>
    /// Modbus Device Manager CallEventName
    /// <para>遵循自定义的 XML 模型，参考 ModbusDevices.Config</para>
    /// </summary>
    public class ModbusDeviceManagement
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(ModbusDeviceManagement));

        /// <summary> <see cref="XModbus"/> Name </summary>
        public const string XModbus = "Modbus";
        /// <summary> <see cref="XDevices"/> Name </summary>
        public const string XDevices = "Devices";

        /// <summary> <see cref="XConnectionName"/> Name </summary>
        public const string XConnectionName = "ConnectionName";

        /// <summary> <see cref="XDisposed"/> Name </summary>
        public const string XDisposed = "Disposed";
        /// <summary> <see cref="XInitialized"/> Name </summary>
        public const string XInitialized = "Initialized";
        /// <summary> <see cref="XInputChanged"/> Name </summary>
        public const string XInputChanged = "InputChanged";
        /// <summary> <see cref="XOutputChanged"/> Name </summary>
        public const string XOutputChanged = "OutputChanged";

        /// <summary> <see cref="XMinValue"/> Name </summary>
        public const string XMinValue = "MinValue";
        /// <summary> <see cref="XMaxValue"/> Name </summary>
        public const string XMaxValue = "MaxValue";
        /// <summary> <see cref="XDeviceAddress"/> Name </summary>
        public const string XDeviceAddress = "DeviceAddress";

        /// <summary>
        /// Connection Management Name
        /// </summary>
        public String Name { get; private set; } = null;
        /// <summary>
        /// Connection Management Reflection Controller
        /// </summary>
        public ReflectionController Controller { get; private set; } = null;

        private IEnumerable<XElement> ModbusElements;
        private static ModbusDeviceManagement instance;
        /// <summary>
        /// 连接管理实例
        /// </summary>
        public static ModbusDeviceManagement Instance
        {
            get
            {
                if (instance == null)
                    instance = new ModbusDeviceManagement();
                return instance;
            }
        }
        /// <summary>
        /// 清理管理对象
        /// </summary>
        public static void Dispose()
        {
            if (instance != null)
            {
                if (!string.IsNullOrWhiteSpace(instance.Name) && instance.Controller != null)
                    instance.Controller.AccessObjects.Remove(instance.Name);

                instance.RemoveAll();
                instance.Name = null;
                instance.Controller = null;

                instance.ModbusElements = null;
                instance = null;
            }
        }
        
        private ModbusDeviceManagement()
        {
            ModbusElements = null;
        }

        /// <summary>
        /// 配置管理对象
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="name"></param>
        public void Configuration(ReflectionController controller, string name)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller), "参数不能为空");

            if (!string.IsNullOrWhiteSpace(name))
            {
                if (controller.AccessObjects.ContainsKey(name))
                    throw new ArgumentException($"ReflectionInterface.AccessObject 对象集合中已包含名称 {name}");

                this.Name = name;
            }
            else
            {
                this.Name = nameof(ModbusDeviceManagement);
                Logger.Info($"{nameof(ModbusDeviceManagement)} Name: {this.Name}");
            }

            if (this.Controller == null)
            {
                this.Controller = controller;
                this.Controller.AccessObjects.Add(this.Name, this);
            }
        }

        /// <summary>
        /// 解析 Modbus 节点元素
        /// <code>//配置示例：
        /// &lt;Modbus Name="Bus.#01" ConnectionName="modbusConnection"&gt; ... 
        /// //OR 建议采用以下方式
        /// &lt;Modbus Name="Bus.#01" Type="ModbusRtu" Parameters="COM7,115200" WriteTimeout="30" &gt; ...
        /// </code>
        /// </summary>
        /// <param name="modbusElements"></param>
        public void TryParseElements(IEnumerable<XElement> modbusElements)
        {
            if (string.IsNullOrWhiteSpace(Name) || Controller == null)
                throw new InvalidOperationException($"未配置管理对象，不可操作");

            if (modbusElements?.Count() <= 0) return;

            RemoveAll();
            this.ModbusElements = modbusElements;

            foreach (XElement modbusElement in modbusElements)
            {
                if (modbusElement.Name.LocalName != XModbus) continue;

                String name = modbusElement.Attribute(ReflectionController.XName)?.Value;
                if (string.IsNullOrWhiteSpace(name))
                {
                    Logger.Warn($"配置格式错误, 属性 {ReflectionController.XName} 不能为空, {modbusElement}");
                    continue;
                }

                if (this.Controller.AccessObjects.ContainsKey(name))
                {
                    Logger.Warn($"配置格式错误, 访问对象名称 {name} 已存在, {modbusElement}");
                    continue;
                }

                ModbusSyncMaster transport = new ModbusSyncMaster(null);
                transport.Name = name;
                if (!ConnectionManagement.CreateConnection(modbusElement, out object master))
                {
                    transport.Dispose();
                    Logger.Warn($"创建 {XModbus} 的连接对象失败 {modbusElement}");
                    continue;
                }

                if (master != null && typeof(IModbusMaster).IsAssignableFrom(master.GetType()))
                {
                    transport.ModbusMaster = master as IModbusMaster;
                    transport.InputChangeEvent += Transport_InputChangeEvent;
                    transport.OutputChangeEvent += Transport_OutputChangeEvent;

                    transport.Start();
                    Controller.AccessObjects.Add(transport.Name, transport);
                }
                else
                {
                    transport.Dispose();
                    Logger.Error($"错误：连接对象 {master} 未实现 {nameof(IModbusMaster)} 接口");
                }
            }

            //XInitialized
            CallEventType(XInitialized, null);
            Thread.Sleep(100);
        }

        /// <summary>
        /// 总线 Input 事件处理
        /// </summary>
        /// <param name="transportDevice"></param>
        /// <param name="ioDevice"></param>
        /// <param name="register"></param>
        private void Transport_OutputChangeEvent(ModbusSyncMaster transportDevice, ModbusDevice ioDevice, Register register)
        {
            CallEventType(XOutputChanged, transportDevice.Name, ioDevice.Address, register);
        }
        /// <summary>
        /// 总线 Output 事件处理
        /// </summary>
        /// <param name="transportDevice"></param>
        /// <param name="ioDevice"></param>
        /// <param name="register"></param>
        private void Transport_InputChangeEvent(ModbusSyncMaster transportDevice, ModbusDevice ioDevice, Register register)
        {
            CallEventType(XInputChanged, transportDevice.Name, ioDevice.Address, register);
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
            
            if (ModbusElements?.Count() <= 0) return;
            foreach (XElement modbus in ModbusElements)
            {
                if (modbus.Attribute(ReflectionController.XName)?.Value != transportName) continue;

                IEnumerable<XElement> events = modbus.Descendants(ReflectionController.XEvent);
                foreach (XElement evt in events)
                {
                    if (evt.Attribute(ReflectionController.XType)?.Value != eventType) continue;
                    
                    if (!StringExtensions.ToNumber(evt.Attribute(XDeviceAddress)?.Value, out byte deviceAddress)) continue;
                    if (deviceAddress != slaveAddress) continue;
                    
                    if (!StringExtensions.ToNumber(evt.Attribute($"{register.Type}Address")?.Value, out ushort regAddress)) continue;
                    if (regAddress != register.Address) continue;

                    if (StringExtensions.ToNumber(evt.Attribute(ReflectionController.XValue)?.Value, out long regValue) && regValue == register.Value)
                    {
                        if (Logger.IsInfoEnabled)
                            Logger.Info($"{eventType} {transportName} > 0x{slaveAddress:X2} > #{register.Address:X4} > {register.Type} > {register.Value}");

                        Controller.TryParseControlMessage(evt.Elements());
                        //ReflectionInterface.TryParseControlMessage(evt.Elements(ReflectionInterface.XAction));
                        continue;
                    }
                    else if(StringExtensions.ToNumber(evt.Attribute(XMinValue)?.Value, out long minValue) && StringExtensions.ToNumber(evt.Attribute(XMaxValue)?.Value, out long maxValue))
                    {
                        if (maxValue > minValue && register.Value <= maxValue && register.Value >= minValue && (register.LastValue < minValue || register.LastValue > maxValue))
                        {
                            if (Logger.IsInfoEnabled)
                                Logger.Info($"{eventType} {transportName} > 0x{slaveAddress:X2} > #{register.Address:X4} > {register.Type} > {register.Value}");

                            Controller.TryParseControlMessage(evt.Elements());
                            //ReflectionInterface.TryParseControlMessage(evt.Elements(ReflectionInterface.XAction));
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
        protected void CallEventType(string eventType, string transportName)
        {
            if (ModbusElements?.Count() <= 0 || string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(transportName)) return;

            IEnumerable<XElement> events = from modbus in ModbusElements
                                           where modbus.Attribute(ReflectionController.XName)?.Value == transportName
                                           from evt in modbus.Descendants(ReflectionController.XEvent)
                                           where evt.Attribute(ReflectionController.XType)?.Value == eventType
                                           select evt;

            Controller.TryParseControlMessage(events.Elements());
        }
        /// <summary>
        /// 在管理对象的配置集合中，跟据事件名称，调用事件
        /// </summary>
        /// <param name="eventName"></param>
        public void CallEventName(string eventName)
        {
            if (ModbusElements?.Count() <= 0 || string.IsNullOrWhiteSpace(eventName)) return;

            IEnumerable<XElement> events = from evt in ModbusElements.Descendants(ReflectionController.XEvent)
                                           where evt.Attribute(ReflectionController.XName)?.Value == eventName
                                           select evt;

            Controller.TryParseControlMessage(events.Elements());
        }
        /// <summary>
        /// 在管理对象的配置集合中，跟据事件名称，调用事件
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="transportName"></param>
        public void CallEventName(string eventName, string transportName)
        {
            if (ModbusElements?.Count() <= 0 || string.IsNullOrWhiteSpace(eventName) || string.IsNullOrWhiteSpace(transportName)) return;

            IEnumerable<XElement> events = from modbus in ModbusElements
                                           where modbus.Attribute(ReflectionController.XName)?.Value == transportName
                                           from evt in modbus.Descendants(ReflectionController.XEvent)
                                           where evt.Attribute(ReflectionController.XName)?.Value == eventName
                                           select evt;

            Controller.TryParseControlMessage(events.Elements());
        }
        

        /// <summary>
        /// Clear Devices
        /// </summary>
        public void RemoveAll()
        {
            if (ModbusElements == null || ModbusElements.Count() <= 0) return;

            CallEventType(XDisposed, null);
            Thread.Sleep(100);

            Type DisposableType = typeof(IDisposable);
            foreach (XElement modbus in ModbusElements)
            {
                string name = modbus.Attribute(ReflectionController.XName)?.Value;

                if (string.IsNullOrWhiteSpace(name) || name == this.Name) continue;
                if (!Controller.AccessObjects.ContainsKey(name)) continue;

                ModbusSyncMaster transport = Controller.AccessObjects[name] as ModbusSyncMaster;
                if (transport == null)
                {
                    Controller.AccessObjects.Remove(name);
                    continue;
                }

                try
                {
                    transport.Stop();
                    transport.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                }
                finally
                {
                    Controller.AccessObjects.Remove(name);
                }
            }

            ModbusElements = null;
        }
        
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(ModbusDeviceManagement)}] {nameof(Name)}:{Name}";
        }
    }
}
