using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Modbus.Device;

namespace SpaceCG.Extensions.Modbus
{
    /// <summary>
    /// Modbus Write Function Info
    /// </summary>
    internal class ModbusMethod
    {
        public String Name;
        //public byte FuncCode;
        public byte SlaveAddress;
        public ushort StartAddress;
        public object Value;
    }

    /// <summary>
    /// Modbus 传输总线
    /// </summary>
    public partial class ModbusTransport
    {
        /// <summary>
        /// 传输对象协议封装对象
        /// </summary>
        public IModbusMaster Master { get; private set; }
        /// <summary>
        /// Read Timeout
        /// </summary>
        public int ReadTimeout
        {
            get { return Master != null && Master.Transport != null ? Master.Transport.ReadTimeout : 0; }
            set { if (Master != null && Master.Transport != null) Master.Transport.ReadTimeout = value; }
        }
        /// <summary>
        /// Write Timeout
        /// </summary>
        public int WriteTimeout
        {
            get { return Master != null && Master.Transport != null ? Master.Transport.WriteTimeout : 0; }
            set { if (Master != null && Master.Transport != null) Master.Transport.WriteTimeout = value; }
        }

        /// <summary>
        /// Modbus Write 方法参数队列
        /// </summary>
        private ConcurrentQueue<ModbusMethod> MethodQueues = new ConcurrentQueue<ModbusMethod>();


        /// <summary>
        /// 翻转单个线圈
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <param name="coilAddress"></param>
        public void TurnSingleCoil(byte slaveAddress, ushort coilAddress)
        {
            ModbusIODevice device = GetDevice(slaveAddress);
            if (device == null) return;
            if (!device.RawCoilsStatus.ContainsKey(coilAddress)) return;

            bool value = device.RawCoilsStatus[coilAddress];
            WriteSingleCoil(device.Address, coilAddress, !value);
        }
        /// <summary>
        /// 翻转多个线圈。
        /// <para>需要从 startAddress 开始有 count 个连续的存储记录</para>
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="count"></param>
        public void TurnMultipleCoils(byte slaveAddress, ushort startAddress, byte count)
        {
            ModbusIODevice device = GetDevice(slaveAddress);
            if (device == null) return;

            ushort address = startAddress;
            bool[] values = new bool[count];

            for(int i = 0; i < count; i ++)
            {
                if (!device.RawCoilsStatus.ContainsKey(address)) return;
                values[i] = !device.RawCoilsStatus[startAddress];

                address++;
            }

            WriteMultipleCoils(device.Address, startAddress, values);
        }
        /// <summary>
        /// 翻转多个线圈。
        /// <para>在 addresses 中，最小地址 到 最大地址 有连续的存储记录</para>
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <param name="coilsAddresses"></param>
        public void TurnMultipleCoils(byte slaveAddress, ushort[] coilsAddresses)
        {
            ModbusIODevice device = GetDevice(slaveAddress);
            if (device == null) return;

            ushort minAddress = coilsAddresses.Min();
            ushort maxAddress = coilsAddresses.Max();

            int i = 0;
            bool[] values = new bool[maxAddress - minAddress];

            for(ushort address = minAddress; address <= maxAddress; address ++, i ++)
            {
                if (!device.RawCoilsStatus.ContainsKey(address)) return;
                values[i] = Array.IndexOf(coilsAddresses, address) >= 0 ? !device.RawCoilsStatus[address] : device.RawCoilsStatus[address];
            }

            WriteMultipleCoils(device.Address, minAddress, values);
        }

        /// <summary>
        /// 写单个线圈
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <param name="coilAddress"></param>
        /// <param name="value"></param>
        public void WriteSingleCoil(byte slaveAddress, ushort coilAddress, bool value)
        {
            ModbusMethod method = new ModbusMethod
            {
                Name = "WriteSingleCoil",
                SlaveAddress = slaveAddress,
                StartAddress = coilAddress,
                Value = value,
            };

            MethodQueues.Enqueue(method);
        }
        /// <summary>
        /// 写多个线圈
        /// <para>需要从 startAddress 开始有 data.Length 个连续的存储记录</para>
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="data"></param>
        public void WriteMultipleCoils(byte slaveAddress, ushort startAddress, bool[] data)
        {
            ModbusMethod method = new ModbusMethod
            {
                Name = "WriteMultipleCoils",
                SlaveAddress = slaveAddress,
                StartAddress = startAddress,
                Value = data,
            };

            MethodQueues.Enqueue(method);
        }
        /// <summary>
        /// 写多个线圈
        /// <para>在 addresses 中，最小地址 到 最大地址 有连续的存储记录</para>
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <param name="addresses"></param>
        /// <param name="data"></param>
        public void WriteMultipleCoils(byte slaveAddress, ushort[] addresses, bool[] data)
        {
            ModbusIODevice device = GetDevice(slaveAddress);
            if (device == null) return;
            if (addresses.Length != data.Length) return;

            ushort minAddress = addresses.Min();
            ushort maxAddress = addresses.Max();

            int i = 0;
            bool[] values = new bool[maxAddress - minAddress];

            for (ushort address = minAddress; address <= maxAddress; address++, i++)
            {
                if (!device.RawCoilsStatus.ContainsKey(address)) return;
                values[i] = Array.IndexOf(addresses, address) >= 0 ? !device.RawCoilsStatus[address] : device.RawCoilsStatus[address];
            }

            WriteMultipleCoils(device.Address, minAddress, values);
        }

        /// <summary>
        /// 写单个寄存器
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <param name="registerAddress"></param>
        /// <param name="value"></param>
        public void WriteSingleRegister(byte slaveAddress, ushort registerAddress, ushort value)
        {
            ModbusMethod method = new ModbusMethod
            {
                Name = "WriteSingleRegister",
                SlaveAddress = slaveAddress,
                StartAddress = registerAddress,
                Value = value,
            };

            MethodQueues.Enqueue(method);
        }
        /// <summary>
        /// 写多个寄存器
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <param name="startAddress"></param>
        /// <param name="data"></param>
        public void WriteMultipleRegisters(byte slaveAddress, ushort startAddress, ushort[] data)
        {
            ModbusMethod method = new ModbusMethod
            {
                Name = "WriteMultipleRegisters",
                SlaveAddress = slaveAddress,
                StartAddress = startAddress,
                Value = data,
            };

            MethodQueues.Enqueue(method);
        }

        /// <summary>
        /// 总线 IO 中断阻塞时间
        /// </summary>
        /// <param name="millisecondsTimeout"></param>
        public void Sleep(int millisecondsTimeout)
        {
            ModbusMethod method = new ModbusMethod
            {
                Name = "Sleep",
                SlaveAddress = 0,
                StartAddress = 0,
                Value = millisecondsTimeout,
            };
            MethodQueues.Enqueue(method);
        }

        /// <summary>
        /// 禁用指定的的寄存器 IO 事件同步
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <param name="registerAddress">寄存器地址，-1 表示当前 IO 设备的所有寄存器 </param>
        /// <param name="type"></param>
        /// <param name="timeout">超时恢复，大于 0 时，表示会自动恢复启用 IO 事件同步；-1 表示一直禁用</param>
        public void DisableIOEventSync(byte slaveAddress, int registerAddress = -1, RegisterType type = RegisterType.DiscreteInput, int timeout = -1)
        {
            ModbusIODevice device = GetDevice(slaveAddress);
            if (device == null) return;

            if (registerAddress <= -1)
            {
                foreach (var register in device.Registers)
                    register.EnabledChangeEvent = false;
            }
            else
            {
                foreach (var register in device.Registers)
                {
                    if (register.Address == registerAddress && register.Type == type)
                    {
                        register.EnabledChangeEvent = false;
                        break;
                    }
                }
            }

            //自动复位
            if (timeout > 0)
            {
                Task.Run(() =>
                {
                    Thread.Sleep(timeout);
                    EnabledIOEventSync(slaveAddress, registerAddress, type);
                });
            }
        }
        /// <summary>
        /// 允许指定的寄存器 IO 事件同步
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <param name="registerAddress"></param>
        /// <param name="type"></param>
        public void EnabledIOEventSync(byte slaveAddress, int registerAddress = -1, RegisterType type = RegisterType.DiscreteInput)
        {
            ModbusIODevice device = GetDevice(slaveAddress);
            if (device == null) return;

            if (registerAddress <= -1)
            {
                foreach (var register in device.Registers)
                    register.EnabledChangeEvent = true;
            }
            else
            {
                foreach (var register in device.Registers)
                {
                    if (register.Address == registerAddress && register.Type == type)
                    {
                        register.EnabledChangeEvent = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 同步执行输出方法队列
        /// </summary>
        private void SyncOutputMethodQueues()
        {
            if (MethodQueues.Count() <= 0 || !IOThreadRunning || Master?.Transport == null) return;
            if (!MethodQueues.TryDequeue(out ModbusMethod method)) return;

            if (method.Name == "Sleep")
            {
                if ((int)method.Value > 0) Thread.Sleep((int)method.Value);
                return;
            }

            ModbusIODevice device = GetDevice(method.SlaveAddress);
            if (device == null) return;

            try
            {
                switch(method.Name)
                {
                    case "WriteSingleCoil":
                        bool bool_value = (bool)method.Value;
                        Master.WriteSingleCoil(method.SlaveAddress, method.StartAddress, bool_value);
                        device.UpdateRawRegisterValues(method.StartAddress, RegisterType.CoilsStatus, new bool[] { bool_value });
                        break;

                    case "WriteMultipleCoils":
                        bool[] bool_values = (bool[])method.Value;
                        Master.WriteMultipleCoils(method.SlaveAddress, method.StartAddress, bool_values);
                        device.UpdateRawRegisterValues(method.StartAddress, RegisterType.CoilsStatus, bool_values);
                        break;

                    case "WriteSingleRegister":
                        ushort ushort_value = (ushort)method.Value;
                        Master.WriteSingleRegister(method.SlaveAddress, method.StartAddress, ushort_value);
                        device.UpdateRawRegisterValues(method.StartAddress, RegisterType.HoldingRegister, new ushort[] { ushort_value });
                        break;

                    case "WriteMultipleRegisters":
                        ushort[] ushort_values = (ushort[])method.Value;
                        Master.WriteMultipleRegisters(method.SlaveAddress, method.StartAddress, ushort_values);
                        device.UpdateRawRegisterValues(method.StartAddress, RegisterType.HoldingRegister, ushort_values);
                        break;
                }
            }
            catch(Exception ex)
            {
                Logger.Error(this);
                Logger.Error(ex);
                Thread.Sleep(300);
            }

        }

        /// <summary>
        /// TestEcho
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool TestEcho(String message)
        {
            if (String.IsNullOrWhiteSpace(message))
            {
                Logger.Info("Test...Echo....");
                return false;
            }
            else
            {
                Logger.Info($"Test...Echo...{message}");
                return true;
            }
        }
    }
}
