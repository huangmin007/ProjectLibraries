using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Modbus.Device;
using SpaceCG.Generic;
using SpaceCG.Net;
using static System.Collections.Specialized.BitVector32;

namespace SpaceCG.Extensions.Modbus
{
    /// <summary>
    /// Modbus Device Manager CallEventName
    /// <para>遵循自定义的 XML 模型，参考 ModbusDevices.Config</para>
    /// </summary>
    public class ModbusDeviceManager
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(ModbusDeviceManager));

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
        /// Current Object Name
        /// </summary>
        public String Name { get; private set; } = null;
        private ReflectionInterface ReflectionInterface;
        private IEnumerable<XElement> ModbusElements;

        /// <summary>
        /// Modbus 设备管理对象
        /// </summary>
        /// <param name="reflectionInterface"></param>
        /// <param name="name"></param>
        public ModbusDeviceManager(ReflectionInterface reflectionInterface, string name)
        {
            if (reflectionInterface == null)  
                throw new ArgumentNullException(nameof(reflectionInterface), "参数错误，不能为空");
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name), "参数错误，不能为空");

            this.Name = name;
            this.ReflectionInterface = reflectionInterface;
            this.ReflectionInterface.AccessObjects.Add(name, this);
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
            if (modbusElements?.Count() <= 0) return;

            RemoveAll();
            this.ModbusElements = modbusElements;

            foreach (XElement modbusElement in modbusElements)
            {
                if (modbusElement.Name.LocalName != XModbus) continue;

                String name = modbusElement.Attribute(ReflectionInterface.XName)?.Value;
                if (string.IsNullOrWhiteSpace(name))
                {
                    Logger.Warn($"配置格式错误, 属性 {ReflectionInterface.XName} 不能为空, {modbusElement}");
                    continue;
                }
                
                if (this.ReflectionInterface.AccessObjects.ContainsKey(name))
                {
                    Logger.Warn($"配置格式错误, 访问对象名称 {name} 已存在, {modbusElement}");
                    continue;
                }

                if (!ModbusTransport.TryParse(modbusElement, out ModbusTransport transport))
                {
                    Logger.Warn($"解析/创建 传输总线 设备失败: {modbusElement}");
                    continue;
                }

                object master = null;
                string connectionName = modbusElement.Attribute(XConnectionName)?.Value;
                if (!string.IsNullOrWhiteSpace(connectionName))
                {
                    if (this.ReflectionInterface.AccessObjects.ContainsKey(connectionName))
                        master = this.ReflectionInterface.AccessObjects[connectionName];
                    else
                    {
                        transport.Dispose();
                        Logger.Warn($"{XModbus} 对象指定的连接 {XConnectionName} 对象 {name} 不存在, {modbusElement}");
                        continue;
                    }
                }
                else
                {
                    if (!ConnectionManager.CreateConnection(modbusElement, out master))
                    {
                        transport.Dispose();
                        Logger.Warn($"创建 {XModbus} 的连接对象失败 {modbusElement}");
                        continue;
                    }
                }

                if (master != null && typeof(IModbusMaster).IsAssignableFrom(master.GetType()))
                {
                    transport.InputChangeEvent += Transport_InputChangeEvent;
                    transport.OutputChangeEvent += Transport_OutputChangeEvent;

                    transport.StartTransport(master as IModbusMaster);
                    ReflectionInterface.AccessObjects.Add(transport.Name, transport);
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
            
            if (ModbusElements?.Count() <= 0) return;
            foreach (XElement modbus in ModbusElements)
            {
                if (modbus.Attribute(ReflectionInterface.XName)?.Value != transportName) continue;

                IEnumerable<XElement> events = modbus.Descendants(ReflectionInterface.XEvent);
                foreach (XElement evt in events)
                {
                    if (evt.Attribute(ReflectionInterface.XType)?.Value != eventType) continue;
                    
                    if (!StringExtensions.TryParse(evt.Attribute(XDeviceAddress)?.Value, out byte deviceAddress)) continue;
                    if (deviceAddress != slaveAddress) continue;
                    
                    if (!StringExtensions.TryParse(evt.Attribute($"{register.Type}Address")?.Value, out ushort regAddress)) continue;
                    if (regAddress != register.Address) continue;

                    if (StringExtensions.TryParse(evt.Attribute(ReflectionInterface.XValue)?.Value, out long regValue) && regValue == register.Value)
                    {
                        if (Logger.IsInfoEnabled)
                            Logger.Info($"{eventType} {transportName} > 0x{slaveAddress:X2} > #{register.Address:X4} > {register.Type} > {register.Value}");

                        ReflectionInterface.TryParseControlMessage(evt.Elements());
                        //ReflectionInterface.TryParseControlMessage(evt.Elements(ReflectionInterface.XAction));
                        continue;
                    }
                    else if(StringExtensions.TryParse(evt.Attribute(XMinValue)?.Value, out long minValue) && StringExtensions.TryParse(evt.Attribute(XMaxValue)?.Value, out long maxValue))
                    {
                        if (maxValue > minValue && register.Value <= maxValue && register.Value >= minValue && (register.LastValue < minValue || register.LastValue > maxValue))
                        {
                            if (Logger.IsInfoEnabled)
                                Logger.Info($"{eventType} {transportName} > 0x{slaveAddress:X2} > #{register.Address:X4} > {register.Type} > {register.Value}");

                            ReflectionInterface.TryParseControlMessage(evt.Elements());
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
        public void CallEventType(string eventType, string transportName)
        {
            if (ModbusElements?.Count() <= 0 || string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(transportName)) return;

            IEnumerable<XElement> events = from modbus in ModbusElements
                                           where modbus.Attribute(ReflectionInterface.XName)?.Value == transportName
                                           from evt in modbus.Descendants(ReflectionInterface.XEvent)
                                           where evt.Attribute(ReflectionInterface.XType)?.Value == eventType
                                           select evt;

            ReflectionInterface.TryParseControlMessage(events.Elements());
        }

        /// <summary>
        /// 跟据事件名称，modbus名称，调用配置事件
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="transportName"></param>
        public void CallEventName(string eventName, string transportName)
        {
            if (ModbusElements?.Count() <= 0 || string.IsNullOrWhiteSpace(eventName) || string.IsNullOrWhiteSpace(transportName)) return;

            IEnumerable<XElement> events = from modbus in ModbusElements
                                           where modbus.Attribute(ReflectionInterface.XName)?.Value == transportName
                                           from evt in modbus.Descendants(ReflectionInterface.XEvent)
                                           where evt.Attribute(ReflectionInterface.XName)?.Value == eventName
                                           select evt;

            ReflectionInterface.TryParseControlMessage(events.Elements());
        }
        /// <summary>
        /// 跟据事件名称，调用事件
        /// </summary>
        /// <param name="eventName"></param>
        public void CallEventName(string eventName)
        {
            if (ModbusElements?.Count() <= 0 || string.IsNullOrWhiteSpace(eventName)) return;

            IEnumerable<XElement> events = from evt in ModbusElements.Descendants(ReflectionInterface.XEvent)
                                           where evt.Attribute(ReflectionInterface.XName)?.Value == eventName
                                           select evt;

            ReflectionInterface.TryParseControlMessage(events.Elements());
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
                string name = modbus.Attribute(ReflectionInterface.XName)?.Value;

                if (string.IsNullOrWhiteSpace(name) || name == this.Name) continue;
                if (!ReflectionInterface.AccessObjects.ContainsKey(name)) continue;

                ModbusTransport transport = ReflectionInterface.AccessObjects[name] as ModbusTransport;
                if (transport == null)
                {
                    ReflectionInterface.AccessObjects.Remove(name);
                    continue;
                }

                try
                {
                    transport.StopTransport();
                    transport.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                }
                finally
                {
                    ReflectionInterface.AccessObjects.Remove(name);
                }
            }

            ModbusElements = null;
        }
        
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(ModbusDeviceManager)}] {nameof(Name)}:{Name}";
        }
    }
}
