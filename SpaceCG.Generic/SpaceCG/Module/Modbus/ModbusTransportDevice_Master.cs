using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Modbus.Device;

namespace SpaceCG.Module.Modbus
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


    public partial class ModbusTransportDevice
    {
        /// <summary>
        /// 传输对象协议封装对象
        /// </summary>
        public IModbusMaster Master { get; private set; }

        public int ReadTimeout
        {
            get { return Master != null && Master.Transport != null ? Master.Transport.ReadTimeout : 0; }
            set { if (Master != null && Master.Transport != null) Master.Transport.ReadTimeout = value; }
        }
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
        /// <param name="value"></param>
        public void TurnSingleCoil(byte slaveAddress, ushort coilAddress)
        {
            ModbusIODevice device = GetIODevice(slaveAddress);
            if (device == null) return;
            if (!device.CoilsStatus.ContainsKey(coilAddress)) return;

            bool value = device.CoilsStatus[coilAddress];
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
            ModbusIODevice device = GetIODevice(slaveAddress);
            if (device == null) return;

            ushort address = startAddress;
            bool[] values = new bool[count];

            for(int i = 0; i < count; i ++)
            {
                if (!device.CoilsStatus.ContainsKey(address)) return;
                values[i] = !device.CoilsStatus[startAddress];

                address++;
            }

            WriteMultipleCoils(device.Address, startAddress, values);
        }
        /// <summary>
        /// 翻转多个线圈。
        /// <para>在 addresses 中，最小地址 到 最大地址 有连续的存储记录</para>
        /// </summary>
        /// <param name="slaveAddress"></param>
        /// <param name="addresses"></param>
        public void TurnMultipleCoils(byte slaveAddress, ushort[] addresses)
        {
            ModbusIODevice device = GetIODevice(slaveAddress);
            if (device == null) return;

            ushort minAddress = addresses.Min();
            ushort maxAddress = addresses.Max();

            int i = 0;
            bool[] values = new bool[maxAddress - minAddress];

            for(ushort address = minAddress; address <= maxAddress; address ++, i ++)
            {
                if (!device.CoilsStatus.ContainsKey(address)) return;
                values[i] = Array.IndexOf(addresses, address) >= 0 ? !device.CoilsStatus[address] : device.CoilsStatus[address];
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
        /// <param name="addreses"></param>
        /// <param name="data"></param>
        public void WriteMultipleCoils(byte slaveAddress, ushort[] addresses, bool[] data)
        {
            ModbusIODevice device = GetIODevice(slaveAddress);
            if (device == null) return;
            if (addresses.Length != data.Length) return;

            ushort minAddress = addresses.Min();
            ushort maxAddress = addresses.Max();

            int i = 0;
            bool[] values = new bool[maxAddress - minAddress];

            for (ushort address = minAddress; address <= maxAddress; address++, i++)
            {
                if (!device.CoilsStatus.ContainsKey(address)) return;
                values[i] = Array.IndexOf(addresses, address) >= 0 ? !device.CoilsStatus[address] : device.CoilsStatus[address];
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
            ModbusIODevice device = GetIODevice(slaveAddress);
            if (device == null) return;

            if (registerAddress <= -1)
            {
                Register[] registers = device.GetRegisters();
                foreach (var register in registers)
                    register.EnabledChangeEvent = false;
            }
            else
            {
                Register register = device.GetRegister((ushort)registerAddress, type);
                if (register != null) register.EnabledChangeEvent = false;
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
            ModbusIODevice device = GetIODevice(slaveAddress);
            if (device == null) return;

            if (registerAddress <= -1)
            {
                Register[] registers = device.GetRegisters();
                foreach (var register in registers)
                    register.EnabledChangeEvent = true;
            }
            else
            {
                Register register = device.GetRegister((ushort)registerAddress, type);
                if (register != null) register.EnabledChangeEvent = true;
            }
        }

        /// <summary>
        /// 同步执行输出方法队列
        /// </summary>
        private void SyncOutputMethodQueues()
        {
            if (MethodQueues.Count() <= 0 || !IOThreadRunning || Master == null) return;
            if (!MethodQueues.TryDequeue(out ModbusMethod method)) return;

            if (method.Name == "Sleep")
            {
                if ((int)method.Value > 0) Thread.Sleep((int)method.Value);
                return;
            }

            ModbusIODevice device = GetIODevice(method.SlaveAddress);
            if (device == null) return;

            try
            {
                switch(method.Name)
                {
                    case "WriteSingleCoil":
                        bool bool_value = (bool)method.Value;
                        Master.WriteSingleCoil(method.SlaveAddress, method.StartAddress, bool_value);
                        device.UpdateRegisterValues(method.StartAddress, RegisterType.CoilsStatus, new bool[] { bool_value });
                        break;

                    case "WriteMultipleCoils":
                        bool[] bool_values = (bool[])method.Value;
                        Master.WriteMultipleCoils(method.SlaveAddress, method.StartAddress, bool_values);
                        device.UpdateRegisterValues(method.StartAddress, RegisterType.CoilsStatus, bool_values);
                        break;

                    case "WriteSingleRegister":
                        ushort ushort_value = (ushort)method.Value;
                        Master.WriteSingleRegister(method.SlaveAddress, method.StartAddress, ushort_value);
                        device.UpdateRegisterValues(method.StartAddress, RegisterType.HoldingRegister, new ushort[] { ushort_value });
                        break;

                    case "WriteMultipleRegisters":
                        ushort[] ushort_values = (ushort[])method.Value;
                        Master.WriteMultipleRegisters(method.SlaveAddress, method.StartAddress, ushort_values);
                        device.UpdateRegisterValues(method.StartAddress, RegisterType.HoldingRegister, ushort_values);
                        break;
                }
            }
            catch(Exception ex)
            {
                Log.Error(this);
                Log.Error(ex);
                Thread.Sleep(300);
            }

        }

        public bool TestEcho(String message)
        {
            if (String.IsNullOrWhiteSpace(message))
            {
                Log.Info("Test...Echo....");
                return false;
            }
            else
            {
                Log.Info($"Test...Echo...{message}");
                return true;
            }
        }
    }
}
