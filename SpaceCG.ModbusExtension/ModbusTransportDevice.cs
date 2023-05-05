using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using SpaceCG.Generic;

namespace SpaceCG.ModbusExtension
{
    
    /// <summary>
    /// Modbus Transport 总线对象
    /// </summary>
    public partial class ModbusTransportDevice : IDisposable
    {
        protected static readonly log4net.ILog Log = log4net.LogManager.GetLogger(nameof(ModbusTransportDevice));

        /// <summary>
        /// 传输对象名称
        /// </summary>
        public String Name { get; private set; }

        /// <summary>
        /// 获取当前传输总线实时 获取输入寄存器 测量得出的总运行时间（一个轮寻周期，以毫秒为单位）。
        /// </summary>
        public long ElapsedMilliseconds { get; private set; }

        private Timer EventTimer;
        private bool IOThreadRunning = false;

        /// <summary>
        /// Read Only 寄存器数据 Change 处理
        /// <para>(ModbusTransportDevice transport, ModbusIODevice ioDevice, Register register)</para>
        /// </summary>
        public event Action<ModbusTransportDevice, ModbusIODevice, Register> InputChangeEvent;
        /// <summary>
        /// Read Write 寄存器数据 Change 处理
        /// <para>(ModbusTransportDevice transport, ModbusIODevice ioDevice, Register register)</para>
        /// </summary>
        public event Action<ModbusTransportDevice, ModbusIODevice, Register> OutputChangeEvent;

        /// <summary>
        /// Modbus Transport 总线对象
        /// </summary>
        /// <param name="master"></param>
        /// <param name="name"></param>
        public ModbusTransportDevice(Modbus.Device.IModbusMaster master, String name = null)
        {
            if (master?.Transport == null)
                throw new ArgumentNullException(nameof(master), "参数不能为空");

            this.Name = name;
            this.Master = master;

            if (String.IsNullOrWhiteSpace(Name))
                this.Name = $"Transport#{this.Master}";

            EventTimer = new Timer(SyncRegisterDescriptionStatus, this, Timeout.Infinite, Timeout.Infinite);
        }

        #region ModbusDevices
        /// <summary>
        /// 当前总线上的 Modbus Devices 集合
        /// <para>Address, ModbusDevice</para>
        /// </summary>
        private ConcurrentDictionary<byte, ModbusIODevice> ModbusDevices { get; set; } = new ConcurrentDictionary<byte, ModbusIODevice>(2, 8);

        /// <summary>
        /// 在当前总线上添加 Modbus 设备
        /// </summary>
        /// <param name="device"></param>
        public bool AddIODevice(ModbusIODevice device)
        {
            if (IOThreadRunning) return false;
            if (device == null)
                throw new ArgumentNullException(nameof(device), "参数不能为空");

            if (ModbusDevices.ContainsKey(device.Address))
            {
                Log.Warn($"传输总线 ({Name})，已经存在相同的 IO 设备地址 0x{device.Address:X2}");
                return false;
            }

            bool result = ModbusDevices.TryAdd(device.Address, device);
            Log.Info($"传输总线 ({Name})，添加 IO 设备 0x{device.Address:X2} 状态 {result}");

            if(result && Master != null)
            {
                device.InputChangeHandler += (ioDevice, register) => InputChangeEvent?.Invoke(this, ioDevice, register);
                device.OutputChangeHandler += (ioDevice, register) => OutputChangeEvent?.Invoke(this, ioDevice, register);
            }

            return result;
        }
        /// <summary>
        /// 从当前总线上移除 Modbus 设备
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <returns></returns>
        public bool RemoveIODevice(byte slaveAddress)
        {
            if (IOThreadRunning) return false;
            if (ModbusDevices.ContainsKey(slaveAddress))
            {
                if (ModbusDevices.TryRemove(slaveAddress, out ModbusIODevice device))
                {
                    Log.Info($"传输总线 ({Name}) 成功移除 IO 设备 0x{slaveAddress:X2}");
                }
                else
                {
                    Log.Warn($"传输总线 ({Name}) 移除 IO 设备 0x{slaveAddress:X2} 失败");
                    return false;
                }
            }
            else
            {
                Log.Warn($"传输总线 {Name} 不存在 IO 设备 0x{slaveAddress:X2}");
            }

            return true;
        }
        /// <summary>
        /// 从当前总线上移除 Modbus 设备
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public bool RemoveIODevice(ModbusIODevice device)
        {
            if (IOThreadRunning) return false;
            if (device == null) throw new ArgumentNullException(nameof(device), "参数不能为空");

            return RemoveIODevice(device.Address);
        }
        /// <summary>
        /// 从当前总线上获取指定地址的设备
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <returns></returns>
        public ModbusIODevice GetIODevice(byte slaveAddress)
        {
            return ModbusDevices.ContainsKey(slaveAddress) ? ModbusDevices[slaveAddress] : null;
        }
        #endregion


        /// <summary>
        /// 启动同步传输
        /// </summary>
        public void StartTransport()
        {
            if (IOThreadRunning) return;

            while (MethodQueues.TryDequeue(out ModbusMethod result))
            {
                ;
            }

            foreach (var device in ModbusDevices)
            {
                ModbusDevices[device.Key].InitializeDevice(Master);

                //ModbusDevices[device.Key].SyncInputRegisters();
                //ModbusDevices[device.Key].SyncOutputRegisters();
                //ModbusDevices[device.Key].InitializeIORegisters();
            }

            IOThreadRunning = true;
            var sc_result = EventTimer.Change(100, 5);
            var tp_result = ThreadPool.QueueUserWorkItem(new WaitCallback(SyncModbusDevicesStatus), this);
            Log.Info($"传输总线 ({this}) 同步数据线程入池状态： {tp_result}, 事件线程状态：{sc_result}");
        }

        /// <summary>
        /// 停止同步传输
        /// </summary>
        public void StopTransport()
        {
            if (!IOThreadRunning) return;

            IOThreadRunning = false;
            ElapsedMilliseconds = 0;
            EventTimer.Change(Timeout.Infinite, Timeout.Infinite);

            while (MethodQueues.TryDequeue(out ModbusMethod result))
            {
                ;
            }

            Thread.Sleep(32);
            Log.Info($"传输总线 ({this}) 停止同步传输");
        }

        /// <summary>
        /// 同步寄存器描述状态
        /// </summary>
        private static void SyncRegisterDescriptionStatus(object modbusTransport)
        {
            ModbusTransportDevice transport = (ModbusTransportDevice)modbusTransport;
            
            ICollection<ModbusIODevice> devices = transport.ModbusDevices.Values;
            foreach(ModbusIODevice device in devices)
            {
                device.SyncRegisterChangeEvents();
            }
        }
        /// <summary>
        /// 同步寄存器数据
        /// </summary>
        /// <param name="modbusTransport"></param>
        private static void SyncModbusDevicesStatus(object modbusTransport)
        {
            ModbusTransportDevice transport = (ModbusTransportDevice)modbusTransport;

            Stopwatch stopwatch = new Stopwatch();
            ICollection<ModbusIODevice> devices = transport.ModbusDevices.Values;

            while(transport.IOThreadRunning)
            {
                stopwatch.Restart();

                foreach (ModbusIODevice device in devices)
                {
                    if (!transport.IOThreadRunning) break;

                    device.SyncInputRegisters();
                    transport.SyncOutputMethodQueues();
                }                

                transport.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }

            transport.ElapsedMilliseconds = 0;
        }

        /// <summary>
        /// 解析 XML 协议格式
        /// </summary>
        /// <param name="element"></param>
        /// <param name="transport"></param>
        /// <returns></returns>
        public static bool TryParse(XElement element, out ModbusTransportDevice transport)
        {
            transport = null;
            if (element == null) return false;

            if (element.Name != "Modbus" || !element.HasElements ||
                String.IsNullOrWhiteSpace(element.Attribute("Name").Value) ||
                String.IsNullOrWhiteSpace(element.Attribute("Parameters")?.Value))
            {
                Log.Warn($"({nameof(ModbusTransportDevice)}) 配置格式存在错误, {element}");
                return false;
            }

            int portORbaudRate = 0;
            Modbus.Device.IModbusMaster master;
            String[] connectArgs = element.Attribute("Parameters").Value.Split(',');
            
            if (connectArgs.Length == 3 && int.TryParse(connectArgs[2], out portORbaudRate))
            {
                master = InstanceExtension.CreateNModbus4Master(connectArgs[0], connectArgs[1], portORbaudRate);
            }
            else
            {
                Log.Warn($"({nameof(ModbusTransportDevice)}) 配置格式存在错误, {element} 节点属性 Parameters 值错误");
                return false;
            }

            if (int.TryParse(element.Attribute("ReadTimeout")?.Value, out int readTimeout))
                master.Transport.ReadTimeout = readTimeout;
            if (int.TryParse(element.Attribute("WriteTimeout")?.Value, out int writeTimeout))
                master.Transport.WriteTimeout = writeTimeout;

            transport = new ModbusTransportDevice(master, element.Attribute("Name").Value);

            //Devices
            IEnumerable<XElement> deviceElements = element.Element("Devices") != null ?
                element.Element("Devices").Elements("Device") : element.Elements("Device");

            foreach (XElement deviceElement in deviceElements)
            {
                if(!(ModbusIODevice.TryParse(deviceElement, out ModbusIODevice device) && transport.AddIODevice(device)))
                {
                    Log.Warn($"{transport} 解析/添加 IO 设备失败");
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (KeyValuePair<byte, ModbusIODevice> kv in ModbusDevices)
                kv.Value.Dispose();

            StopTransport();
            EventTimer.Dispose();
            ModbusDevices.Clear();

            ModbusDevices = null;
            InputChangeEvent = null;
            OutputChangeEvent = null;

            if (Master != null) Master.Dispose();
            Master = null;
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(ModbusTransportDevice)}] Name:{Name}";
        }


    }
}
