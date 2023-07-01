using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Xml.Linq;
using Modbus.Device;
using SpaceCG.Generic;

namespace SpaceCG.Extensions.Modbus
{
    /// <summary>
    /// Modbus Transport 总线对象
    /// </summary>
    public partial class ModbusTransport : IDisposable
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(ModbusTransport));

        /// <summary> <see cref="XModbus"/> Name </summary>
        public const string XModbus = "Modbus";
        
        /// <summary>
        /// 传输对象名称
        /// </summary>
        public String Name { get; private set; }

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
        /// Read Only 寄存器数据 Change 处理
        /// <para>(ModbusTransportDevice transport, ModbusIODevice ioDevice, Register register)</para>
        /// </summary>
        public event Action<ModbusTransport, ModbusIODevice, Register> InputChangeEvent;
        /// <summary>
        /// Read Write 寄存器数据 Change 处理
        /// <para>(ModbusTransportDevice transport, ModbusIODevice ioDevice, Register register)</para>
        /// </summary>
        public event Action<ModbusTransport, ModbusIODevice, Register> OutputChangeEvent;

        /// <summary>
        /// 当前总线上的 Modbus IO 设备集合
        /// </summary>
        public List<ModbusIODevice> ModbusDevices { get; private set; } = new List<ModbusIODevice>(8);

        /// <summary>
        /// Modbus Transport 总线对象
        /// </summary>
        /// <param name="name"></param>
        public ModbusTransport(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name), "参数不能为空");

            this.Name = name;
            this.EventTimer = new Timer(SyncRegisterChangeEvents, this, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// 从当前总线上获取指定地址的设备
        /// </summary>
        /// <param name="deviceAddress"></param>
        /// <returns></returns>
        public ModbusIODevice GetDevice(byte deviceAddress)
        {
            foreach(var device in ModbusDevices) 
            {
                if(device.Address == deviceAddress) return device;
            }

            return null;
        }

        /// <summary>
        /// 启动同步传输
        /// </summary>
        /// <param name="master"></param>
        public void StartTransport(IModbusMaster master)
        {
            if (master == null || master.Transport == null)
                throw new ArgumentNullException(nameof(master), "参数不能为空");

            if (IOThreadRunning) return;
            if(CancelToken != null)
            {
                CancelToken.Dispose();
                CancelToken = null;
            }

            Master = master;
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
        public void StopTransport()
        {
            if (!IOThreadRunning) return;

            DeviceCount = 0;
            CancelToken.Cancel();
            IOThreadRunning = false;
            ElapsedMilliseconds = 0;
            EventTimer.Change(Timeout.Infinite, Timeout.Infinite);

            while (MethodQueues.TryDequeue(out ModbusMethod result))
            {
                ;
            }
            
            Thread.Sleep(32);
            Logger.Info($"传输总线 ({this}) 停止同步传输");
        }

        private void ModbusDevice_InputChangeHandler(ModbusIODevice device, Register register) => InputChangeEvent?.Invoke(this, device, register);
        private void ModbusDevice_OutputChangeHandler(ModbusIODevice device, Register register) => OutputChangeEvent?.Invoke(this, device, register);
            
        /// <summary>
        /// 同步寄存器描述状态
        /// </summary>
        private void SyncRegisterChangeEvents(object modbusTransport)
        {
            foreach(ModbusIODevice device in ModbusDevices)
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

                if (Master?.Transport == null) break;

                //全部重新初使化一次
                if(DeviceCount !=  ModbusDevices.Count) 
                {
                    while (MethodQueues.TryDequeue(out ModbusMethod result))
                    {
                        ;
                    }

                    foreach (var device in ModbusDevices)
                    {
                        device.InputChangeHandler -= ModbusDevice_InputChangeHandler;
                        device.OutputChangeHandler -= ModbusDevice_OutputChangeHandler;

                        device.InitializeDevice(Master);
                        device.InputChangeHandler += ModbusDevice_InputChangeHandler;
                        device.OutputChangeHandler += ModbusDevice_OutputChangeHandler;
                    }

                    DeviceCount = ModbusDevices.Count;
                }

                stopwatch.Restart();
                foreach (ModbusIODevice device in ModbusDevices)
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

        /// <summary>
        /// 解析 XML 协议格式
        /// </summary>
        /// <param name="element"></param>
        /// <param name="transport"></param>
        /// <returns></returns>
        public static bool TryParse(XElement element, out ModbusTransport transport)
        {
            transport = null;
            if (element?.Name.LocalName != XModbus || !element.HasElements) return false;

            string name = element.Attribute(nameof(Name))?.Value;
            if (String.IsNullOrWhiteSpace(name))
            {
                Logger.Warn($"({nameof(ModbusTransport)}) 配置格式存在错误, 属性 {nameof(Name)} 不能为空, {element}");
                return false;
            }

            transport = new ModbusTransport(name);
            foreach (XElement deviceElement in element.Descendants(ModbusIODevice.XDevice))
            {
                if(ModbusIODevice.TryParse(deviceElement, out ModbusIODevice device))
                {
                    transport.ModbusDevices.Add(device);
                }
            }

            return transport.ModbusDevices.Count > 0;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            StopTransport();
            EventTimer.Dispose();

            if (ModbusDevices != null)
            {
                foreach (ModbusIODevice device in ModbusDevices)
                    device.Dispose();

                ModbusDevices.Clear();
                ModbusDevices = null;
            }

            InputChangeEvent = null;
            OutputChangeEvent = null;

            Master?.Dispose();
            Master = null;

        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(ModbusTransport)}] {nameof(Name)}:{Name} {nameof(ModbusDevices)}:{ModbusDevices.Count}";
        }


    }
}
