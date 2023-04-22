using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpaceCG.ModbusExtension
{
    
    /// <summary>
    /// Modbus Transport 对象
    /// </summary>
    public partial class ModbusTransportDevice : IDisposable
    {
        protected static readonly log4net.ILog Log = log4net.LogManager.GetLogger(nameof(ModbusTransportDevice));

        /// <summary>
        /// 传输对象名称
        /// </summary>
        public String Name { get; private set; }

        private Timer SyncTimer;
        private bool IOThreadRunning = false;

        public event Action<ModbusTransportDevice, byte, ushort, ulong, ulong> InputChangeEvent;
        public event Action<ModbusTransportDevice, byte, ushort, ulong, ulong> OutputChangeEvent;

        /// <summary>
        /// Modbus Transport 对象
        /// </summary>
        /// <param name="master"></param>
        /// <param name="name"></param>
        public ModbusTransportDevice(Modbus.Device.IModbusMaster master, String name = null)
        {
            if (master == null || master.Transport == null)
                throw new ArgumentNullException(nameof(master), "参数不能为空");

            this.Name = name;
            this.Master = master;

            if (String.IsNullOrWhiteSpace(Name))
                this.Name = $"Transport#{this.Master}";

            SyncTimer = new Timer(SyncRegisterDescriptionStatus, this, Timeout.Infinite, Timeout.Infinite);
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
        public bool AddDevice(ModbusIODevice device)
        {
            if (IOThreadRunning) return false;
            if (device == null)
                throw new ArgumentNullException(nameof(device), "参数不能为空");

            if (ModbusDevices.ContainsKey(device.Address))
            {
                Log.Warn($"当前总线 {Name} 上，已经存在相同的设备地址 {device.Address}");
                return false;
            }

            bool result = ModbusDevices.TryAdd(device.Address, device);
            Log.Info($"当前总线 {Name} 上，添加设备 {device.Address} 状态 {result}");

            if(result && Master != null)
            {
                device.ReadCoilsStatus = Master.ReadCoils;
                device.ReadDiscreteInputs = Master.ReadInputs;

                device.ReadHoldingRegisters = Master.ReadHoldingRegisters;
                device.ReadInputRegisters = Master.ReadInputRegisters;

                //device.InputChangeHandler += ModbusDevice_InputChangeHandler;
                //device.OutputChangeHandler += ModbusDevice_OutputChangeHandler;

                device.InputChangeHandler += (slaveAddress, registerAddress, newValue, oldValue) => InputChangeEvent?.Invoke(this, slaveAddress, registerAddress, newValue, oldValue);
                device.OutputChangeHandler += (slaveAddress, registerAddress, newValue, oldValue) => OutputChangeEvent?.Invoke(this, slaveAddress, registerAddress, newValue, oldValue);

                device.InitializeDevice();
            }

            return result;
        }
        /// <summary>
        /// 从当前总线上移除 Modbus 设备
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <returns></returns>
        public bool RemoveDevice(byte slaveAddress)
        {
            if (IOThreadRunning) return false;
            if (ModbusDevices.ContainsKey(slaveAddress))
            {
                if (ModbusDevices.TryRemove(slaveAddress, out ModbusIODevice device))
                {
                    Log.Info($"从总线 {Name} 上成功移除 Modbus 设备 {slaveAddress}");
                }
                else
                {
                    Log.Warn($"从总线 {Name} 上移除 Modbus 设备 {slaveAddress} 失败");
                    return false;
                }
            }
            else
            {
                Log.Warn($"总线 {Name} 不存在 Modbus 设备 {slaveAddress}");
            }

            return true;
        }
        /// <summary>
        /// 从当前总线上移除 Modbus 设备
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public bool RemoveDevice(ModbusIODevice device)
        {
            if (IOThreadRunning) return false;
            if (device == null) throw new ArgumentNullException(nameof(device), "参数不能为空");

            return RemoveDevice(device.Address);
        }
        /// <summary>
        /// 从当前总线上获取指定地址的设备
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <returns></returns>
        public ModbusIODevice GetDevice(byte slaveAddress)
        {
            return ModbusDevices.ContainsKey(slaveAddress) ? ModbusDevices[slaveAddress] : null;
        }
        #endregion

        /// <summary>
        /// 启动同步传输
        /// </summary>
        public void StartTransport()
        {
            foreach (var device in ModbusDevices)
            {
                ModbusDevices[device.Key].InitializeDevice();
                ModbusDevices[device.Key].SyncInputRegisters();
                ModbusDevices[device.Key].SyncOutputRegisters();
            }

            IOThreadRunning = true;
            var sc_result = SyncTimer.Change(100, 8);
            var tp_result = ThreadPool.QueueUserWorkItem(new WaitCallback(SyncModbusDevicesStatus), this);
            Log.InfoFormat($"设备总线 ({Name}) 同步数据线程入池状态： {tp_result}, 事件线程状态：{sc_result}");
        }
        /// <summary>
        /// 停止同步传输
        /// </summary>
        public void StopTransport()
        {
            IOThreadRunning = false;
            SyncTimer.Change(Timeout.Infinite, Timeout.Infinite);

            while (MethodQueues.TryDequeue(out ModbusMethod result))
            {
                ;
            }
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
                device.SyncRegisterDescriptionStatus();
            }
        }
        /// <summary>
        /// 同步寄存器数据
        /// </summary>
        /// <param name="modbusTransport"></param>
        private static void SyncModbusDevicesStatus(object modbusTransport)
        {
            ModbusTransportDevice transport = (ModbusTransportDevice)modbusTransport;

            ICollection<ModbusIODevice> devices = transport.ModbusDevices.Values;

            while(transport.IOThreadRunning)
            {
                foreach(ModbusIODevice device in devices)
                {
                    device.SyncInputRegisters();
                    transport.SyncOutputMethodQueues();
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (var kv in ModbusDevices)
                kv.Value.Dispose();

            StopTransport();

            SyncTimer.Dispose();
            ModbusDevices.Clear();
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(ModbusTransportDevice)}] Name:{Name}";
        }


    }
}
