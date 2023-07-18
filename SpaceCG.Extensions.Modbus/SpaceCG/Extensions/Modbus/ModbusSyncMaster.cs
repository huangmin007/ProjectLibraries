using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Xml.Linq;
using Modbus.Device;
using SpaceCG.Generic;
using SpaceCG.Net;

namespace SpaceCG.Extensions.Modbus
{
    /// <summary>
    /// Modbus Synchronize Master
    /// </summary>
    public partial class ModbusSyncMaster : IDisposable
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(ModbusSyncMaster));

        /// <summary> <see cref="XConnectin"/> Name </summary>
        public const string XConnectin = "Connection";
        
        /// <summary>
        /// 传输对象名称
        /// </summary>
        public String Name { get; set; }

        /// <summary>
        /// 获取当前传输总线实时 获取输入寄存器 测量得出的总运行时间（一个轮寻周期，以毫秒为单位）。
        /// </summary>
        public long ElapsedMilliseconds { get; private set; }

        private Timer EventTimer;
        private int DeviceCount = 0;
        private bool IOThreadRunning = false;
        private CancellationTokenSource CancelToken;

        /// <summary>
        /// 是否在正运行同步
        /// </summary>
        public bool Running => IOThreadRunning;

        /// <summary>
        /// Input/Output Change Delegate
        /// </summary>
        /// <param name="modbusSyncMaster"></param>
        /// <param name="modbusDevice"></param>
        /// <param name="register"></param>
        /// <returns></returns>
        public delegate void IOChangeDelegate(ModbusSyncMaster modbusSyncMaster, ModbusDevice modbusDevice, Register register);

        /// <summary>
        /// Read Only 寄存器数据 Change 处理
        /// <para>(ModbusTransportDevice transport, ModbusIODevice ioDevice, Register register)</para>
        /// </summary>
        public event IOChangeDelegate InputChangeEvent;
        //public event Action<ModbusSyncMaster, ModbusDevice, Register> InputChangeEvent;
        /// <summary>
        /// Read Write 寄存器数据 Change 处理
        /// <para>(ModbusTransportDevice transport, ModbusIODevice ioDevice, Register register)</para>
        /// </summary>
        public event IOChangeDelegate OutputChangeEvent;
        //public event Action<ModbusSyncMaster, ModbusDevice, Register> OutputChangeEvent;

        /// <summary>
        /// 当前总线上的 Modbus IO 设备集合
        /// </summary>
        public List<ModbusDevice> ModbusDevices { get; private set; } = new List<ModbusDevice>(8);

        /// <summary>
        /// Modbus 同步主机对象
        /// </summary>
        /// <param name="master"></param>
        public ModbusSyncMaster(IModbusMaster master)
        {
            this.ModbusMaster = master;
            this.Name = this.Name = $"{nameof(ModbusMaster)}#{master}";
            this.EventTimer = new Timer(SyncRegisterChangeEvents, this, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// 启动同步传输
        /// </summary>
        public void Start()
        {
            if (ModbusMaster == null) 
                throw new InvalidOperationException($"{nameof(ModbusMaster)} 不能为空");

            if (IOThreadRunning) return;
            if(CancelToken != null)
            {
                CancelToken.Dispose();
                CancelToken = null;
            }
            MethodQueues.Clear();
            //while (MethodQueues.TryDequeue(out ModbusMethod result))
            //{;}

            DeviceCount = 0;
            IOThreadRunning = true;
            ElapsedMilliseconds = 0;
            CancelToken = new CancellationTokenSource();

            var sc_result = EventTimer.Change(100, 4);
            //var tp_result = ThreadPool.QueueUserWorkItem(new WaitCallback(SyncModbusDevicesStatus), this);
            var tp_result = ThreadPool.QueueUserWorkItem(o => SyncModbusDevicesStatus(CancelToken.Token));
            Logger.Info($"传输总线 ({this}) 同步数据线程入池状态： {tp_result}, 事件线程状态：{sc_result}");
        }

        /// <summary>
        /// 停止同步传输
        /// </summary>
        public void Stop()
        {
            if (!IOThreadRunning) return;

            DeviceCount = 0;
            CancelToken.Cancel();
            IOThreadRunning = false;
            ElapsedMilliseconds = 0;
            EventTimer.Change(Timeout.Infinite, Timeout.Infinite);
            
            Thread.Sleep(32);
            Logger.Info($"传输总线 ({this}) 停止同步传输");
        }

        private void ModbusDevice_InputChangeHandler(ModbusDevice device, Register register) => InputChangeEvent?.Invoke(this, device, register);
        private void ModbusDevice_OutputChangeHandler(ModbusDevice device, Register register) => OutputChangeEvent?.Invoke(this, device, register);
            
        /// <summary>
        /// 同步寄存器描述状态
        /// </summary>
        private void SyncRegisterChangeEvents(object modbusTransport)
        {
            foreach(ModbusDevice device in ModbusDevices)
            {
                device.SyncRegisterChangeEvents();
            }
        }
        /// <summary>
        /// 同步寄存器数据
        /// </summary>
        /// <param name="token"></param>
        private void SyncModbusDevicesStatus(CancellationToken token)
        {
            Stopwatch stopwatch = new Stopwatch();
            while(IOThreadRunning)
            {
                if (token.IsCancellationRequested)
                {
                    Logger.Info($"Cancellation Requested ... {Thread.CurrentThread.ManagedThreadId}");
                    break;
                }

                if (ModbusMaster?.Transport == null) break;

                //全部重新初使化一次
                if(DeviceCount !=  ModbusDevices.Count) 
                {
                    MethodQueues.Clear();
                    //while (MethodQueues.TryDequeue(out ModbusMethod result))
                    //{; }
                    foreach (var device in ModbusDevices)
                    {
                        device.InputChangeHandler -= ModbusDevice_InputChangeHandler;
                        device.OutputChangeHandler -= ModbusDevice_OutputChangeHandler;

                        device.InitializeDevice(ModbusMaster);
                        device.InputChangeHandler += ModbusDevice_InputChangeHandler;
                        device.OutputChangeHandler += ModbusDevice_OutputChangeHandler;
                    }

                    DeviceCount = ModbusDevices.Count;
                }

                stopwatch.Restart();
                foreach (ModbusDevice device in ModbusDevices)
                {
                    if (!IOThreadRunning) break;

                    device.SyncInputRegisters();
                    this.SyncOutputMethodQueues();
                }

                this.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            }

            this.DeviceCount = 0;
            this.IOThreadRunning = false;
            this.ElapsedMilliseconds = 0;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Stop();
            EventTimer?.Dispose();

            if (ModbusDevices != null)
            {
                foreach (ModbusDevice device in ModbusDevices)
                    device.Dispose();

                ModbusDevices.Clear();
                ModbusDevices = null;
            }

            InputChangeEvent = null;
            OutputChangeEvent = null;

            ModbusMaster?.Dispose();
            ModbusMaster = null;
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(ModbusSyncMaster)}] {nameof(Name)}:{Name} {nameof(ModbusDevices)}:{ModbusDevices.Count}";
        }


    }
}
