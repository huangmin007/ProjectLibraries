using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using SpaceCG.Generic;

namespace SpaceCG.ModbusExtension
{
    /// <summary>
    /// 寄存器类型
    /// </summary>
    public enum RegisterType : byte
    {
        /// <summary>
        /// 未知类型
        /// </summary>
        Unknown,

        /// <summary>
        /// 线圈状态 (Read Write)
        /// </summary>
        CoilsStatus,

        /// <summary>
        /// 离散输入 (Read Only)
        /// </summary>
        DiscreteInput,

        /// <summary>
        /// 保持寄存器 (Read Write)
        /// </summary>
        HoldingRegister,

        /// <summary>
        /// 输入寄存器 (Read Only)
        /// </summary>
        InputRegister,
    }

    /// <summary>
    /// 寄存器描述对象
    /// </summary>
    public class Register
    {
        private string _name;
        private byte _count = 1;
        private ushort _address = 0x0000;

        /// <summary>
        /// 寄存器地址
        /// </summary>
        public ushort Address
        {
            get { return _address; }
            internal set
            {
                _address = value;
                if (String.IsNullOrWhiteSpace(Name)) _name = String.Format("Register#0x{0:X4}", _address);
            }
        }

        /// <summary>
        /// 寄存器类型
        /// </summary>
        public RegisterType Type { get; internal set; } = RegisterType.Unknown;

        /// <summary>
        /// 关联的连续寄存器数量，1~4 bytes
        /// </summary>
        public byte Count
        {
            get { return _count; }
            internal set { _count = (value <= 0x00) ? (byte)0x01 : value >= 0x04 ? (byte)0x04 : value; }
        }

        /// <summary>
        /// 位偏移量，暂时保留
        /// </summary>
        public byte Offset { get; set; } = 0;

        /// <summary>
        /// 寄存器名称
        /// </summary>
        public String Name
        {
            get { return _name; }
            set { _name = String.IsNullOrWhiteSpace(value) ? String.Format("Register#0x{0:X4}", Address) : value; }
        }

        /// <summary>
        /// 寄存器单位描述
        /// </summary>
        public String Units { get; set; } = null;

        /// <summary>
        /// 字节顺序(Endianness)，低地址数据在后，高地址数据在前
        /// </summary>
        public bool IsLittleEndian { get; set; } = false;

        /// <summary>
        /// 寄存器的值
        /// </summary>
        public ulong Value { get; internal set; } = ulong.MaxValue;

        /// <summary>
        /// 寄存器描述对象
        /// </summary>
        /// <param name="address"></param>
        /// <param name="count"></param>
        /// <param name="type"></param>
        internal Register(ushort address, RegisterType type, byte count = 1)
        {
            this.Type = type;
            this.Count = count;
            this.Address = address;
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(Register)}] Name:{Name} Address:{String.Format("Register#0x{0:X4}", Address)} Type:{Type}";
        }
        /// <summary>
        /// 创建 <see cref="Register"/> 对象
        /// </summary>
        /// <param name="address"></param>
        /// <param name="count"></param>
        /// <param name="type"></param>
        /// <param name="isLittleEndian"></param>
        /// <param name="offset"></param>
        /// <param name="name"></param>
        /// <param name="units"></param>
        /// <returns></returns>
        public static Register Create(ushort address, RegisterType type, byte count = 1, bool isLittleEndian = false, byte offset = 0, string name = null, string units = null)
        {
            Register description = new Register(address, type, count);
            description.IsLittleEndian = isLittleEndian;
            description.Offset = offset;
            description.Name = name;
            description.Units = units;

            return description;
        }

        /// <summary>
        /// 解析 XML 格式
        /// </summary>
        /// <param name="descriptionElement"></param>
        /// <param name="register"></param>
        /// <returns></returns>
        public static bool TryParse(XElement descriptionElement, out Register register)
        {
            register = null;
            if (descriptionElement == null || descriptionElement.Name != "Register" || !descriptionElement.HasAttributes) return false;
            if (String.IsNullOrWhiteSpace(descriptionElement.Attribute("Address")?.Value) || 
                String.IsNullOrWhiteSpace(descriptionElement.Attribute("Type")?.Value)) return false;

            ushort address = 0;
            RegisterType type = RegisterType.Unknown;
            if (StringExtension.ToNumber<ushort>(descriptionElement.Attribute("Address").Value, ref address) &&
                Enum.TryParse<RegisterType>(descriptionElement.Attribute("Address").Value, true, out type))
            {
                byte count = 1;
                bool result = byte.TryParse(descriptionElement.Attribute("Count")?.Value, out count);
                register = new Register(address, type, (byte)(result ? count : 0x01));
            }
            else
            {
                return false;
            }

            if (bool.TryParse(descriptionElement.Attribute("IsLittleEndian")?.Value, out bool isLittleEndian))
            {
                register.IsLittleEndian = isLittleEndian;
            }

            register.Name = descriptionElement.Attribute("Name")?.Value;
            register.Units = descriptionElement.Attribute("Units")?.Value;

            return true;
        }
    }
 
    /// <summary>
    /// Modbus Device 对象
    /// </summary>
    public class ModbusIODevice:IDisposable
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(nameof(ModbusIODevice));

        /// <summary>
        /// 读取线圈状态的代理函数
        /// <para>(byte slaveAddress, ushort startAddress, ushort numberOfPoints)</para>
        /// </summary>
        public Func<byte, ushort, ushort, bool[]> ReadCoilsStatus;
        /// <summary>
        /// 读取保持寄存器的代理函数
        /// <para>(byte slaveAddress, ushort startAddress, ushort numberOfPoints)</para>
        /// </summary>
        public Func<byte, ushort, ushort, ushort[]> ReadHoldingRegisters;
        /// <summary>
        /// 读取离散输入的代理函数
        /// <para>(byte slaveAddress, ushort startAddress, ushort numberOfPoints)</para>
        /// </summary>
        public Func<byte, ushort, ushort, bool[]> ReadDiscreteInputs;
        /// <summary>
        /// 读取输入寄存器的代理函数
        /// <para>(byte slaveAddress, ushort startAddress, ushort numberOfPoints)</para>
        /// </summary>
        public Func<byte, ushort, ushort, ushort[]> ReadInputRegisters;

        /// <summary>
        /// Read Only 寄存器数据 Change 处理
        /// </summary>
        public event Action<byte, ushort, ulong, ulong> InputChangeHandler;
        /// <summary>
        /// Read Write 寄存器数据 Change 处理
        /// </summary>
        public event Action<byte, ushort, ulong, ulong> OutputChangeHandler;

        /// <summary>
        /// 设备名称
        /// </summary>
        public String Name { get; internal set; } = null;
        /// <summary>
        /// 设备地址
        /// </summary>
        public byte Address { get; internal set; } = 0x01;

        /// <summary>
        /// 线圈状态，数据类型为 bool 类型，RW-ReadWrite
        /// </summary>
        internal ConcurrentDictionary<ushort, bool> CoilsStatus { get; private set; } = new ConcurrentDictionary<ushort, bool>(2, 16);
        /// <summary>
        /// 离散输入，数据类型为 bool 类型，RO-ReadOnly
        /// </summary>
        private ConcurrentDictionary<ushort, bool> DiscreteInputs { get; set; } = new ConcurrentDictionary<ushort, bool>(2, 16);
        /// <summary>
        /// 保持寄存器，数据类型为 ushort 类型，RW-ReadyWrite
        /// </summary>
        private ConcurrentDictionary<ushort, ushort> HoldingRegisters { get; set; } = new ConcurrentDictionary<ushort, ushort>(2, 16);
        /// <summary>
        /// 输入寄存器，数据类型为 ushort 类型，RO-ReadOnly
        /// </summary>
        private ConcurrentDictionary<ushort, ushort> InputRegisters { get; set; } = new ConcurrentDictionary<ushort, ushort>(2, 16);

        private Dictionary<ushort, ushort> CoilsStatusAddresses = new Dictionary<ushort, ushort>();
        private Dictionary<ushort, ushort> DiscreteInputsAddresses = new Dictionary<ushort, ushort>();
        private Dictionary<ushort, ushort> HoldingRegistersAddresses = new Dictionary<ushort, ushort>();
        private Dictionary<ushort, ushort> InputRegistersAddresses = new Dictionary<ushort, ushort>();

        /// <summary>
        /// 寄存器描述对象，用于描述寄存器数据或及组合
        /// </summary>
        private List<Register> RegisterDescriptions = new List<Register>();

        /// <summary>
        /// Modbus IO Device 构造函数
        /// </summary>
        /// <param name="address"></param>
        /// <param name="name"></param>
        public ModbusIODevice(byte address, String name = null)
        {
            this.Name = name;
            this.Address = address;

            if (String.IsNullOrWhiteSpace(Name))
                this.Name = String.Format("Device#{0:00}", this.Address);
        }

        #region Add,Remove,Update
        /// <summary>
        /// 添加寄存器对象
        /// </summary>
        /// <param name="register"></param>
        public bool AddRegisters(Register register)
        {
            var registers = from reg in RegisterDescriptions
                            where reg.Address == register.Address && reg.Type == register.Type
                            select reg;

            if (registers?.Count() != 0) return false;

            RegisterDescriptions.Add(register);

            byte count = register.Count;
            RegisterType type = register.Type;
            ushort startAddress = register.Address;

            if (type == RegisterType.CoilsStatus)
            {
                for (ushort address = startAddress; address < startAddress + count; address++)
                {
                    if (!CoilsStatus.ContainsKey(address)) CoilsStatus.TryAdd(address, default);
                }
            }
            else if (type == RegisterType.DiscreteInput)
            {
                for (ushort address = startAddress; address < startAddress + count; address++)
                {
                    if (!DiscreteInputs.ContainsKey(address)) DiscreteInputs.TryAdd(address, default);
                }
            }
            else if (type == RegisterType.HoldingRegister)
            {
                for (ushort address = startAddress; address < startAddress + count; address++)
                {
                    if (!HoldingRegisters.ContainsKey(address)) HoldingRegisters.TryAdd(address, default);
                }
            }
            else if (type == RegisterType.InputRegister)
            {
                for (ushort address = startAddress; address < startAddress + count; address++)
                {
                    if (!InputRegisters.ContainsKey(address)) InputRegisters.TryAdd(address, default);
                }
            }

            return true;
        }
        /// <summary>
        /// 添加寄存器对象
        /// </summary>
        /// <param name="address"></param>
        /// <param name="count"></param>
        /// <param name="type"></param>
        public bool AddRegisters(ushort address, RegisterType type, byte count = 1)
        {
            var registers = from des in RegisterDescriptions
                            where des.Address == address && des.Type == type
                            select des;
            if (registers?.Count() != 0) return false;

            return AddRegisters(new Register(address, type, count));
        }
        
        /// <summary>
        /// 移除寄存器对象
        /// </summary>
        /// <param name="register"></param>
        /// <returns></returns>
        public bool RemoveRegisters(Register register)
        {
            var registers = from reg in RegisterDescriptions
                            where reg.Address == register.Address && reg.Type == register.Type // && reg.Count == register.Count
                            select reg;

            if (registers?.Count() != 1) return false;

            register = registers.First();
            if (!RegisterDescriptions.Remove(register)) return false;

            if (register.Type == RegisterType.CoilsStatus)
            {
                for(ushort address = register.Address; address < register.Address + register.Count; address ++)
                {
                    if (CoilsStatus.ContainsKey(address)) CoilsStatus.TryRemove(address, out bool value);
                }
            }
            else if (register.Type == RegisterType.DiscreteInput)
            {
                for (ushort address = register.Address; address < register.Address + register.Count; address++)
                {
                    if (DiscreteInputs.ContainsKey(address)) DiscreteInputs.TryRemove(address, out bool value);
                }
            }
            else if (register.Type == RegisterType.HoldingRegister)
            {
                for (ushort address = register.Address; address < register.Address + register.Count; address++)
                {
                    if (HoldingRegisters.ContainsKey(address)) HoldingRegisters.TryRemove(address, out ushort value);
                }
            }
            else if (register.Type == RegisterType.InputRegister)
            {
                for (ushort address = register.Address; address < register.Address + register.Count; address++)
                {
                    if (InputRegisters.ContainsKey(address)) InputRegisters.TryRemove(address, out ushort value);
                }
            }

            return true;
        }
        /// <summary>
        /// 移除寄存器对象
        /// </summary>
        /// <param name="address"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool RemoveRegisters(ushort address, RegisterType type)
        {
            var registers = from des in RegisterDescriptions
                            where des.Address == address && des.Type == type
                            select des;
            if (registers?.Count() != 1) return false;

            return RemoveRegisters(registers.First());
        }

        /// <summary>
        /// 更新寄存器数据
        /// </summary>
        /// <param name="startAddress"></param>
        /// <param name="type"></param>
        /// <param name="values"></param>
        public void UpdateRegisters(ushort startAddress, RegisterType type, bool[] values)
        {
            ushort address, i = 0;
            if (type == RegisterType.CoilsStatus)
            {
                for (address = startAddress; address < startAddress + values.Length; address++, i++)
                {
                    if (CoilsStatus.ContainsKey(address)) CoilsStatus[address] = values[i];
#if false
                    else
                    {
                        if (CoilsStatus.TryAdd(address, values[i]))
                        {
                            AddRegisters(new Register(address, type, 1));
                            CoilsStatusAddresses = SpliteAddresses(CoilsStatus.Keys.ToArray());
                        }
                    }
#endif
                }
            }
            else if (type == RegisterType.DiscreteInput)
            {
                for (address = startAddress; address < startAddress + values.Length; address++, i++)
                {
                    if (DiscreteInputs.ContainsKey(address)) DiscreteInputs[address] = values[i];
#if fasle
                    else
                    {
                        if (DiscreteInputs.TryAdd(address, values[i]))
                        {
                            AddRegisters(new Register(address, type, 1));
                            DiscreteInputsAddresses = SpliteAddresses(DiscreteInputs.Keys.ToArray());
                        }
                    }
#endif
                }
            }
        }
        /// <summary>
        /// 更新寄存器数据
        /// </summary>
        /// <param name="startAddress"></param>
        /// <param name="type"></param>
        /// <param name="values"></param>
        public void UpdateRegisters(ushort startAddress, RegisterType type, ushort[] values)
        {
            ushort address, i = 0;
            if (type == RegisterType.HoldingRegister)
            {
                for (address = startAddress; address < startAddress + values.Length; address++, i++)
                {
                    if (HoldingRegisters.ContainsKey(address)) HoldingRegisters[address] = values[i];
#if false
                    else
                    {
                        if (HoldingRegisters.TryAdd(address, values[i]))
                        {
                            AddRegisters(new Register(address, type, 1));
                            HoldingRegistersAddresses = SpliteAddresses(HoldingRegisters.Keys.ToArray());
                        }
                    }
#endif
                }
            }
            else if (type == RegisterType.InputRegister)
            {
                for (address = startAddress; address < startAddress + values.Length; address++, i++)
                {
                    if (InputRegisters.ContainsKey(address)) InputRegisters[address] = values[i];
#if false
                    else
                    {
                        if (InputRegisters.TryAdd(address, values[i]))
                        {
                            AddRegisters(new Register(address, type, 1));
                            InputRegistersAddresses = SpliteAddresses(InputRegisters.Keys.ToArray());
                        }
                    }
#endif
                }
            }
        }

        /// <summary>
        /// 获取寄存器的值
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public ulong GetRegistersValues(Register description)
        {
            ulong value = 0;

            if (description.Type == RegisterType.CoilsStatus)
            {
                if (CoilsStatus.ContainsKey(description.Address)) return (byte)(CoilsStatus[description.Address] ? 0x01 : 0x00);
            }
            else if (description.Type == RegisterType.DiscreteInput)
            {
                if (DiscreteInputs.ContainsKey(description.Address)) return (byte)(DiscreteInputs[description.Address] ? 0x01 : 0x00);
            }
            else if (description.Type == RegisterType.HoldingRegister)
            {
                value = GetRegisterValue(HoldingRegisters, description);
            }
            else if (description.Type == RegisterType.InputRegister)
            {
                value = GetRegisterValue(InputRegisters, description);
            }

            return value;
        }
        /// <summary>
        /// 获取寄存器的值
        /// </summary>
        /// <param name="address"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public ulong GetRegistersValues(ushort address, RegisterType type)
        {
            ulong value = 0;
            var regDes = from des in RegisterDescriptions
                         where des.Address == address && des.Type == type
                         select des;

            if (regDes?.Count() == 1)
                value = GetRegistersValues(regDes.First());

            return value;
        }

        /// <summary>
        /// 清除所有寄存器
        /// </summary>
        public void ClearAllRegisters()
        {
            CoilsStatus.Clear();
            DiscreteInputs.Clear();
            HoldingRegisters.Clear();
            InputRegisters.Clear();

            RegisterDescriptions.Clear();

            CoilsStatusAddresses.Clear();
            DiscreteInputsAddresses.Clear();
            HoldingRegistersAddresses.Clear();
            InputRegistersAddresses.Clear();
        }
        #endregion

        /// <summary>
        /// 初使化设备，将寄存器数据归零，寄存器描述归零
        /// <para>对寄存器 添加、移除操作 全部完成后初使化设备</para>
        /// </summary>
        public void InitializeDevice()
        {
            foreach (var kv in CoilsStatus) CoilsStatus[kv.Key] = default;
            foreach (var kv in DiscreteInputs) DiscreteInputs[kv.Key] = default;
            foreach (var kv in HoldingRegisters) HoldingRegisters[kv.Key] = default;
            foreach (var kv in InputRegisters) InputRegisters[kv.Key] = default;

            foreach (var des in RegisterDescriptions) des.Value = ulong.MaxValue;

            CoilsStatusAddresses = SpliteAddresses(CoilsStatus.Keys.ToArray());
            DiscreteInputsAddresses = SpliteAddresses(DiscreteInputs.Keys.ToArray());
            HoldingRegistersAddresses = SpliteAddresses(HoldingRegisters.Keys.ToArray());
            InputRegistersAddresses = SpliteAddresses(InputRegisters.Keys.ToArray());
        }

        /// <summary>
        /// 同步输出寄存器
        /// <para>需要在线程中实时或间隔时间调用，一般只用于初使化，或长时间间隔更新对比</para>
        /// </summary>
        public void SyncOutputRegisters()
        {
            if (CoilsStatus.Count > 0 && ReadCoilsStatus != null)
            {
                try
                {
                    foreach (var kv in CoilsStatusAddresses)
                    {
                        bool[] result = ReadCoilsStatus(Address, kv.Key, kv.Value);
                        UpdateRegisters(kv.Key, RegisterType.CoilsStatus, result);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"{this}");
                    Log.Error(ex);
                    Thread.Sleep(300);
                }
            }
            if (HoldingRegisters.Count > 0 && ReadHoldingRegisters != null)
            {
                try
                {
                    foreach (var kv in HoldingRegistersAddresses)
                    {
                        ushort[] result = ReadHoldingRegisters(Address, kv.Key, kv.Value);
                        UpdateRegisters(kv.Key, RegisterType.HoldingRegister, result);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"{this}");
                    Log.Error(ex);
                    Thread.Sleep(300);
                }
            }
        }

        /// <summary>
        /// 同步输入寄存器
        /// <para>需要在线程中实时或间隔时间调用</para>
        /// </summary>
        public void SyncInputRegisters()
        {
            if (DiscreteInputs.Count > 0 && ReadDiscreteInputs != null)
            {
                try
                {
                    foreach (var kv in DiscreteInputsAddresses)
                    {
                        bool[] result = ReadDiscreteInputs(Address, kv.Key, kv.Value);
                        UpdateRegisters(kv.Key, RegisterType.DiscreteInput, result);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"{this}");
                    Log.Error(ex);
                    Thread.Sleep(300);
                }
            }
            if (InputRegisters.Count > 0 && ReadInputRegisters != null)
            {
                try
                {
                    foreach (var kv in InputRegistersAddresses)
                    {
                        ushort[] result = ReadInputRegisters(Address, kv.Key, kv.Value);
                        UpdateRegisters(kv.Key, RegisterType.InputRegister, result);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"{this}");
                    Log.Error(ex);
                    Thread.Sleep(300);
                }
            }
        }

        /// <summary>
        /// 同步寄存器描述数据，会产生 Input/Output Change 事件
        /// <para>需要在线程中实时或间隔时间调用</para>
        /// </summary>
        public void SyncRegisterDescriptionStatus()
        {
            for(int i = 0; i < RegisterDescriptions.Count; i ++)
            {
                Register register = RegisterDescriptions[i];
                if(register.Type == RegisterType.CoilsStatus && CoilsStatus.Count > 0)
                {
                    ulong value = (ulong)(CoilsStatus[register.Address] ? 1 : 0);
                    if(register.Value == ulong.MaxValue)
                    {
                        register.Value = value;
                        continue;
                    }

                    if(register.Value != value)
                    {
                        ulong lastValue = register.Value;
                        register.Value = value;
                        OutputChangeHandler?.Invoke(Address, register.Address, value, lastValue);
                    }
                }
                else if(register.Type == RegisterType.DiscreteInput && DiscreteInputs.Count > 0)
                {
                    ulong value = (ulong)(DiscreteInputs[register.Address] ? 1 : 0);
                    if (register.Value == ulong.MaxValue)
                    {
                        register.Value = value;
                        continue;
                    }

                    if (register.Value != value)
                    {
                        ulong lastValue = register.Value;
                        register.Value = value;
                        InputChangeHandler?.Invoke(Address, register.Address, value, lastValue);
                    }
                }
                else if(register.Type == RegisterType.HoldingRegister && HoldingRegisters.Count > 0)
                {
                    ulong value = GetRegisterValue(HoldingRegisters, register);
                    if (register.Value == ulong.MaxValue)
                    {
                        register.Value = value;
                        continue;
                    }

                    if (register.Value != value)
                    {
                        ulong lastValue = register.Value;
                        register.Value = value;
                        OutputChangeHandler?.Invoke(Address, register.Address, value, lastValue);
                    }
                }
                else if(register.Type == RegisterType.InputRegister && InputRegisters.Count > 0)
                {
                    ulong value = GetRegisterValue(InputRegisters, register);
                    if (register.Value == ulong.MaxValue)
                    {
                        register.Value = value;
                        continue;
                    }

                    if (register.Value != value)
                    {
                        ulong lastValue = register.Value;
                        register.Value = value;
                        InputChangeHandler?.Invoke(Address, register.Address, value, lastValue);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            ClearAllRegisters();

            ReadCoilsStatus = null;
            ReadHoldingRegisters = null;
            ReadDiscreteInputs = null;
            ReadInputRegisters = null;

            InputChangeHandler = null;
            OutputChangeHandler = null;
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(ModbusIODevice)}] Name:{Name} Address:{Address}";
        }

        /// <summary>
        /// 解析 XML 格式
        /// </summary>
        /// <param name="deviceElement"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public static bool TryParse(XElement deviceElement, out ModbusIODevice device)
        {
            device = null;

            if (deviceElement == null || deviceElement.Name != "Device" || deviceElement.Attribute("Address") == null) return false;

            byte address = 0x00;
            if (!StringExtension.ToNumber<byte>(deviceElement.Attribute("Address")?.Value, ref address)) return false;

            device = new ModbusIODevice(address, deviceElement.Attribute("Name")?.Value);
            String[] attributes = new String[] { "Unknown", "CoilsStatusCount", "DiscreteInputCount", "HoldingRegisterCount", "InputRegisterCount" };

            for (int i = 1; i < attributes.Length; i++)
            {
                if (String.IsNullOrWhiteSpace(deviceElement.Attribute(attributes[i])?.Value))
                {
                    ushort[] args = new ushort[2] { 0, 0 }; //count, startAddress
                    StringExtension.ToNumberArray<ushort>(deviceElement.Attribute("CoilsStatusCount").Value, ref args, ',');
                    device.AddRegisters(args[1], (RegisterType)Enum.Parse(typeof(RegisterType), i.ToString(), true), (byte)args[0]);
                }
            }

            if (!deviceElement.HasElements) return true;




            return true;
        }


        /// <summary>
        /// 分割非连续的寄存器地址
        /// <para>返回 startAddress,numberOfPoints 的值键对 </para>
        /// </summary>
        /// <param name="addresses"></param>
        /// <returns>返回 startAddress,numberOfPoints 的值键对</returns>
        internal static Dictionary<ushort, ushort> SpliteAddresses(ushort[] addresses)
        {
            //startAddress,numberOfPoints
            Dictionary<ushort, ushort> result = new Dictionary<ushort, ushort>();
            if (addresses?.Length <= 0) return result;

            ushort i = 0;
            Array.Sort(addresses);

            ushort numberOfPoints = 1;
            ushort startAddress = addresses[i];
            ushort nextAddress = (ushort)(startAddress + 1);

            while (i < addresses.Length - 1)
            {
                i++;

                if (nextAddress == addresses[i])
                {
                    nextAddress++;
                    numberOfPoints++;
                }
                else
                {
                    result.Add(startAddress, numberOfPoints);

                    numberOfPoints = 1;
                    startAddress = addresses[i];
                    nextAddress = (ushort)(startAddress + 1);
                }

                if (i == addresses.Length - 1)
                {
                    result.Add(startAddress, numberOfPoints);
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// 获取 ushort 类型寄存器数据
        /// </summary>
        /// <param name="registers"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        internal static ulong GetRegisterValue(IReadOnlyDictionary<ushort, ushort> registers, Register description)
        {            
            ulong longValue = (ulong)0;
            if (description.Count <= 0 || description.Count > 4) return longValue;

            int i = 0;
            for(ushort address = description.Address; address < description.Address + description.Count; address++, i ++)
            {
                ulong value = registers.ContainsKey(address) ? registers[address] : (ushort)0x0000;

                //BitConverter
                int bitOffset = description.IsLittleEndian ? i * 16 : (description.Count - 1 - i) * 16;
                longValue |= (ulong)(value << bitOffset);
            }

            return longValue;
        }
    }

}
