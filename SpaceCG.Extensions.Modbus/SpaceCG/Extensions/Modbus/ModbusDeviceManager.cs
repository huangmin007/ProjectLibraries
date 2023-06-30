using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Modbus.Device;
using SpaceCG.Generic;
using SpaceCG.Net;

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
        private ControlInterface ControlInterface;
        private IEnumerable<XElement> ModbusElements;

        /// <summary>
        /// Modbus 设备管理对象
        /// </summary>
        /// <param name="controlInterface"></param>
        /// <param name="name"></param>
        public ModbusDeviceManager(ControlInterface controlInterface, string name)
        {
            if (controlInterface == null || string.IsNullOrWhiteSpace(name))  
                throw new ArgumentNullException("参数错误，不能为空");

            this.Name = name;
            this.ControlInterface = controlInterface;
            this.ControlInterface.AccessObjects.Add(name, this);
        }
       
        /// <summary>
        /// 解析 Modbus 节点元素
        /// </summary>
        /// <param name="modbusElements"></param>
        public void TryParseModbusElements(IEnumerable<XElement> modbusElements)
        {
            if (modbusElements?.Count() <= 0) return;

            RemoveAll();
            this.ModbusElements = modbusElements;

            foreach (XElement modbusElement in modbusElements)
            {
                String name = modbusElement.Attribute(ControlInterface.XName)?.Value;
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (this.ControlInterface.AccessObjects.ContainsKey(name))
                {
                    Logger.Warn($"对象名称 {name} 已存在, {modbusElement}");
                    continue;
                }

                if (ModbusTransport.TryParse(modbusElement, out ModbusTransport transport))
                {
                    transport.InputChangeEvent += Transport_InputChangeEvent;
                    transport.OutputChangeEvent += Transport_OutputChangeEvent;
                    string connectionName = modbusElement.Attribute("ConnectionName")?.Value;

                    if (!string.IsNullOrWhiteSpace(connectionName))
                    {
                        var master = this.ControlInterface.AccessObjects[connectionName];
                        if (typeof(IModbusMaster).IsAssignableFrom(master.GetType()))
                        {
                            transport.StartTransport(master as IModbusMaster);
                            ControlInterface.AccessObjects.Add(transport.Name, transport);
                        }
                        else
                        {
                            transport.Dispose();
                            Logger.Error($"错误：连接对象 {master} 未实现 IModbusMaster 接口");
                        }
                    }
                    else
                    {
                        transport.Dispose();
                        Logger.Warn($"Modbus 对象未指定连接 ConnectionName 对象");
                    }
                }
                else
                {
                    Logger.Warn($"解析/添加 传输总线 设备失败: {modbusElement}");
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
            if (ModbusElements?.Count() <= 0 || string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(transportName)) return;

            IEnumerable<XElement> events = from modbus in ModbusElements
                                           where modbus.Attribute(ControlInterface.XName)?.Value == transportName
                                           from evt in modbus.Descendants(ControlInterface.XEvent)
                                           where evt.Attribute(ControlInterface.XType)?.Value == eventType
                                           select evt;

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
            if (ModbusElements?.Count() <= 0 || string.IsNullOrWhiteSpace(eventName) || string.IsNullOrWhiteSpace(transportName)) return;

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
            if (ModbusElements?.Count() <= 0 || string.IsNullOrWhiteSpace(eventName)) return;

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
        /// Clear Devices
        /// </summary>
        public void RemoveAll()
        {
            CallEventType(XDisposed, null);
            Thread.Sleep(100);
            if (ModbusElements == null || ModbusElements.Count() <= 0) return;

            Type DisposableType = typeof(IDisposable);
            foreach (XElement modbus in ModbusElements)
            {
                string name = modbus.Attribute(ControlInterface.XName)?.Value;

                if (string.IsNullOrWhiteSpace(name) || name == this.Name) continue;
                if (!ControlInterface.AccessObjects.ContainsKey(name)) continue;

                ModbusTransport transport = ControlInterface.AccessObjects[name] as ModbusTransport;
                if (transport == null)
                {
                    ControlInterface.AccessObjects.Remove(name);
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
                    ControlInterface.AccessObjects.Remove(name);
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
