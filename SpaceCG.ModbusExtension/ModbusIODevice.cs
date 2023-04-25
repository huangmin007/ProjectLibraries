using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(nameof(Register));

        private string _name;
        private byte _count = 1;

        /// <summary>
        /// 寄存器地址
        /// </summary>
        public ushort Address { get; internal set; } = 0x0000;
        
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
            get
            { 
                if (String.IsNullOrWhiteSpace(_name)) _name = $"Register#0x{Address:X4}";
                return _name;
            }
            internal set { _name = String.IsNullOrWhiteSpace(value) ? $"Register#0x{Address:X4}" : value; }
        }

        /// <summary>
        /// 寄存器单位描述，暂时保留
        /// </summary>
        public String Units { get; set; } = null;

        /// <summary>
        /// 字节顺序(Endianness)，低地址数据在后，高地址数据在前
        /// </summary>
        public bool IsLittleEndian { get; set; } = true;

        /// <summary>
        /// 允许 IO Change 事件
        /// </summary>
        public bool EnabledChangeEvent { get; set; } = true;

        /// <summary>
        /// 64位 寄存器的最新数据
        /// </summary>
        public ulong Value { get; internal set; } = ulong.MaxValue;

        /// <summary>
        /// 64位 寄存器的上一次的数据
        /// </summary>
        public ulong LastValue { get; internal set; } = ulong.MaxValue;

#if false
        /// <summary>
        /// 寄存器的原始数据存储块，待我思考一会儿，用于未来出现 大于64位数据时 在升级吧~~~
        /// </summary>
        internal byte[] RawBytes;
#endif

        /// <summary>
        /// 寄存器描述对象
        /// </summary>
        /// <param name="address"></param>
        /// <param name="type"></param>
        /// <param name="count"></param>
        /// <param name="name"></param>
        public Register(ushort address, RegisterType type, byte count = 1, string name = null)
        {
            this.Type = type;
            this.Count = count;
            this.Address = address;
            this.Name = name;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(Register)}]  Name:{Name}  Address:0x{Address:X4}  Type:{Type}  Count:{Count}  Value:{Value}";
        }

        /// <summary>
        /// 创建 <see cref="Register"/> 对象
        /// </summary>
        /// <param name="address"></param>
        /// <param name="type"></param>
        /// <param name="count"></param>
        /// <param name="isLittleEndian"></param>
        /// <param name="offset"></param>
        /// <param name="name"></param>
        /// <param name="units"></param>
        /// <returns></returns>
        public static Register Create(ushort address, RegisterType type, byte count = 1, bool isLittleEndian = true, byte offset = 0, string name = null, string units = null, bool enabledChangeEvent = true)
        {
            Register register = new Register(address, type, count);
            register.Name = name;
            register.Units = units;
            register.Offset = offset;
            register.IsLittleEndian = isLittleEndian;
            register.EnabledChangeEvent = enabledChangeEvent;

            return register;
        }

        /// <summary>
        /// 解析 XML 格式
        /// </summary>
        /// <param name="element"></param>
        /// <param name="register"></param>
        /// <returns></returns>
        public static bool TryParse(XElement element, out Register register)
        {
            register = null;
            if (element == null) return false;
            if (element.Name != nameof(Register) || !element.HasAttributes)
            {
                Log.Warn($"({nameof(Register)}) 配置格式存在错误, {element}");
                return false;
            }

            if (StringExtension.TryParse<ushort>(element.Attribute("Address")?.Value, out ushort address) &&
                Enum.TryParse<RegisterType>(element.Attribute("Type")?.Value, true, out RegisterType type) && type != RegisterType.Unknown)
            {
                bool result = byte.TryParse(element.Attribute("Count")?.Value, out byte count);
                register = new Register(address, type, (byte)(result ? count : 0x01));
            }
            else
            {
                Log.Warn($"({nameof(Register)}) 配置格式存在错误, {element} 节点属性 Address, Type 值错误");
                return false;
            }

            register.Name = element.Attribute("Name")?.Value;
            register.Units = element.Attribute("Units")?.Value;
            register.Offset = byte.TryParse(element.Attribute("Offset")?.Value, out byte offset) ? offset : (byte)0x00;
            register.IsLittleEndian = bool.TryParse(element.Attribute("IsLittleEndian")?.Value, out bool isLittleEndian) ? isLittleEndian : true;
            register.EnabledChangeEvent = bool.TryParse(element.Attribute("EnabledChangeEvent")?.Value, out bool enabledChangeEvent) ? enabledChangeEvent : true;

            return true;
        }
    }


    /// <summary>
    /// Modbus Input/Output Device 对象
    /// </summary>
    public class ModbusIODevice:IDisposable
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(nameof(ModbusIODevice));

        /// <summary>
        /// 读取线圈状态的代理函数
        /// <para>(byte slaveAddress, ushort startAddress, ushort numberOfPoints)</para>
        /// </summary>
        internal Func<byte, ushort, ushort, bool[]> ReadCoilsStatus;
        /// <summary>
        /// 读取保持寄存器的代理函数
        /// <para>(byte slaveAddress, ushort startAddress, ushort numberOfPoints)</para>
        /// </summary>
        internal Func<byte, ushort, ushort, ushort[]> ReadHoldingRegisters;
        /// <summary>
        /// 读取离散输入的代理函数
        /// <para>(byte slaveAddress, ushort startAddress, ushort numberOfPoints)</para>
        /// </summary>
        internal Func<byte, ushort, ushort, bool[]> ReadDiscreteInputs;
        /// <summary>
        /// 读取输入寄存器的代理函数
        /// <para>(byte slaveAddress, ushort startAddress, ushort numberOfPoints)</para>
        /// </summary>
        internal Func<byte, ushort, ushort, ushort[]> ReadInputRegisters;

        /// <summary>
        /// Read Only 寄存器数据 Change 处理
        /// <para>(byte slaveAddress, Register register)</para>
        /// </summary>
        internal event Action<byte, Register> InputChangeHandler;
        /// <summary>
        /// Read Write 寄存器数据 Change 处理
        /// <para>(byte slaveAddress, Register register)</para>
        /// </summary>
        internal event Action<byte, Register> OutputChangeHandler;

        /// <summary>
        /// 设备名称
        /// </summary>
        public String Name { get; internal set; } = null;
        /// <summary>
        /// 设备地址
        /// </summary>
        public byte Address { get; internal set; } = 0x01;

        /// <summary>
        /// 线圈状态，原始数据存储对象，数据类型为 bool 类型，RW-ReadWrite
        /// </summary>
        internal ConcurrentDictionary<ushort, bool> CoilsStatus { get; private set; } = new ConcurrentDictionary<ushort, bool>(2, 16);
        /// <summary>
        /// 离散输入，原始数据存储对象，数据类型为 bool 类型，RO-ReadOnly
        /// </summary>
        private ConcurrentDictionary<ushort, bool> DiscreteInputs { get; set; } = new ConcurrentDictionary<ushort, bool>(2, 16);
        /// <summary>
        /// 保持寄存器，原始数据存储对象，数据类型为 ushort 类型，RW-ReadyWrite
        /// </summary>
        private ConcurrentDictionary<ushort, ushort> HoldingRegisters { get; set; } = new ConcurrentDictionary<ushort, ushort>(2, 16);
        /// <summary>
        /// 输入寄存器，原始数据存储对象，数据类型为 ushort 类型，RO-ReadOnly
        /// </summary>
        private ConcurrentDictionary<ushort, ushort> InputRegisters { get; set; } = new ConcurrentDictionary<ushort, ushort>(2, 16);

        //startAddress,numberOfPoint
        private ConcurrentDictionary<ushort, ushort> CoilsStatusAddresses = new ConcurrentDictionary<ushort, ushort>();
        private ConcurrentDictionary<ushort, ushort> DiscreteInputsAddresses = new ConcurrentDictionary<ushort, ushort>();
        private ConcurrentDictionary<ushort, ushort> HoldingRegistersAddresses = new ConcurrentDictionary<ushort, ushort>();
        private ConcurrentDictionary<ushort, ushort> InputRegistersAddresses = new ConcurrentDictionary<ushort, ushort>();

        /// <summary>
        /// 寄存器描述对象，用于描述寄存器数据或及组合
        /// </summary>
        private List<Register> RegistersDescription = new List<Register>();

        /// <summary>
        /// Modbus Input/Output Device 构造函数
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
        /// <para>注意：只有添加的寄存器 address 才会产生 Input/Output Change 事件</para>
        /// </summary>
        /// <param name="register"></param>
        public bool AddRegister(Register register)
        {
            var registers = from reg in RegistersDescription
                            where reg.Address == register.Address && reg.Type == register.Type
                            select reg;

            int fCount = registers != null ? registers.Count() : 0;

            if (fCount == 0) RegistersDescription.Add(register);
            else if (fCount == 1 && registers.First().Count != register.Count) registers.First().Count = register.Count;
            else return false;

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
        /// <para>注意：只有添加的寄存器 address 才会产生 Input/Output Change 事件</para>
        /// </summary>
        /// <param name="registerAddress"></param>
        /// <param name="count"></param>
        /// <param name="type"></param>
        public bool AddRegister(ushort registerAddress, RegisterType type, byte count = 1)
        {
            var registers = from reg in RegistersDescription
                            where reg.Address == registerAddress && reg.Type == type
                            select reg;

            int fCount = registers != null ? registers.Count() : 0;

            if (fCount == 0)
                return AddRegister(new Register(registerAddress, type, count));

            if (fCount == 1 && registers.First().Count != count)
                return AddRegister(registers.First());

            return false;
        }
        
        /// <summary>
        /// 移除寄存器对象
        /// </summary>
        /// <param name="register"></param>
        /// <returns></returns>
        public bool RemoveRegister(Register register)
        {
            var registers = from reg in RegistersDescription
                            where reg.Address == register.Address && reg.Type == register.Type // && reg.Count == register.Count
                            select reg;

            if (registers?.Count() != 1) return false;

            register = registers.First();
            if (!RegistersDescription.Remove(register)) return false;

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
        /// <param name="registerAddress"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool RemoveRegister(ushort registerAddress, RegisterType type)
        {
            var registers = from reg in RegistersDescription
                            where reg.Address == registerAddress && reg.Type == type
                            select reg;

            if (registers?.Count() != 1) return false;
            return RemoveRegister(registers.First());
        }

        /// <summary>
        /// 更新寄存器数据
        /// </summary>
        /// <param name="startAddress"></param>
        /// <param name="type"></param>
        /// <param name="values"></param>
        internal void UpdateRegisterValues(ushort startAddress, RegisterType type, bool[] values)
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
        internal void UpdateRegisterValues(ushort startAddress, RegisterType type, ushort[] values)
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
        /// 获取寄存器对象
        /// </summary>
        /// <param name="registerAddress"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Register GetRegister(ushort registerAddress, RegisterType type)
        {
            var regs = from reg in RegistersDescription
                       where reg.Address == registerAddress && reg.Type == type
                       select reg;

            return regs?.Count() == 1 ? regs.First() : null;
        }
        /// <summary>
        /// 获取所有寄存器对象
        /// </summary>
        public Register[] GetRegisters() => RegistersDescription.ToArray();

        /// <summary>
        /// 获取寄存器的值
        /// </summary>
        /// <param name="register"></param>
        /// <returns></returns>
        public ulong GetRegisterValue(Register register)
        {
            ulong value = 0;

            if (register.Type == RegisterType.CoilsStatus)
            {
                value = GetRegisterValue(CoilsStatus, register);
            }
            else if (register.Type == RegisterType.DiscreteInput)
            {
                value = GetRegisterValue(DiscreteInputs, register);
            }
            else if (register.Type == RegisterType.HoldingRegister)
            {
                value = GetRegisterValue(HoldingRegisters, register);
            }
            else if (register.Type == RegisterType.InputRegister)
            {
                value = GetRegisterValue(InputRegisters, register);
            }

            return value;
        }
        /// <summary>
        /// 获取寄存器的值
        /// </summary>
        /// <param name="registerAddress"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public ulong GetRegisterValue(ushort registerAddress, RegisterType type)
        {
            Register register = GetRegister(registerAddress, type);
            return register != null ? GetRegisterValue(register) : 0;
        }

        /// <summary>
        /// 清除所有寄存器
        /// </summary>
        public void ClearRegisters()
        {
            CoilsStatus.Clear();
            DiscreteInputs.Clear();
            HoldingRegisters.Clear();
            InputRegisters.Clear();

            RegistersDescription.Clear();

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
        internal void InitializeDevice(Modbus.Device.IModbusMaster master)
        {
            if (master?.Transport != null)
            {
                this.ReadCoilsStatus = master.ReadCoils;
                this.ReadDiscreteInputs = master.ReadInputs;

                this.ReadInputRegisters = master.ReadInputRegisters;
                this.ReadHoldingRegisters = master.ReadHoldingRegisters;
            }
            
            foreach (var kv in CoilsStatus) CoilsStatus[kv.Key] = default;
            foreach (var kv in DiscreteInputs) DiscreteInputs[kv.Key] = default;
            foreach (var kv in HoldingRegisters) HoldingRegisters[kv.Key] = default;
            foreach (var kv in InputRegisters) InputRegisters[kv.Key] = default;

            foreach (var reg in RegistersDescription)
            {
                reg.Value = ulong.MaxValue;
                reg.LastValue = ulong.MaxValue;
            }

            CoilsStatusAddresses = SpliteAddresses(CoilsStatus.Keys.ToArray());
            DiscreteInputsAddresses = SpliteAddresses(DiscreteInputs.Keys.ToArray());
            HoldingRegistersAddresses = SpliteAddresses(HoldingRegisters.Keys.ToArray());
            InputRegistersAddresses = SpliteAddresses(InputRegisters.Keys.ToArray());

            Log.Info($"{this} Initialize Device.");
        }

        /// <summary>
        /// 同步传输输出寄存器
        /// <para>需要在线程中实时或间隔时间调用，一般只用于初使化，或长时间间隔更新对比</para>
        /// </summary>
        internal void SyncOutputRegisters()
        {
            if (CoilsStatus.Count > 0 && ReadCoilsStatus != null)
            {
                try
                {
                    foreach (var kv in CoilsStatusAddresses)
                    {
                        bool[] result = ReadCoilsStatus(Address, kv.Key, kv.Value);
                        UpdateRegisterValues(kv.Key, RegisterType.CoilsStatus, result);
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
                        UpdateRegisterValues(kv.Key, RegisterType.HoldingRegister, result);
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
        /// 同步传输输入寄存器
        /// <para>需要在线程中实时或间隔时间调用</para>
        /// </summary>
        internal void SyncInputRegisters()
        {
            if (DiscreteInputs.Count > 0 && ReadDiscreteInputs != null)
            {
                try
                {
                    foreach (var kv in DiscreteInputsAddresses)
                    {
                        bool[] result = ReadDiscreteInputs(Address, kv.Key, kv.Value);
                        UpdateRegisterValues(kv.Key, RegisterType.DiscreteInput, result);
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
                        UpdateRegisterValues(kv.Key, RegisterType.InputRegister, result);
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
        /// 初使化寄存器的 Value, LastValue 为事件处理做准备
        /// </summary>
        internal void InitializeIORegisters()
        {
            for (int i = 0; i < RegistersDescription.Count; i++)
            {
                Register register = RegistersDescription[i];
                //CoilsStatus
                if (register.Type == RegisterType.CoilsStatus && CoilsStatus.Count > 0)
                {
                    ulong value = GetRegisterValue(CoilsStatus, register);
                    if (register.Value == ulong.MaxValue && register.LastValue == ulong.MaxValue)
                    {
                        register.Value = value;
                        register.LastValue = value;
                        continue;
                    }
                }
                //DiscreteInput
                else if (register.Type == RegisterType.DiscreteInput && DiscreteInputs.Count > 0)
                {
                    ulong value = GetRegisterValue(DiscreteInputs, register);
                    if (register.Value == ulong.MaxValue && register.LastValue == ulong.MaxValue)
                    {
                        register.Value = value;
                        register.LastValue = value;
                        continue;
                    }
                }
                //HoldingRegisters
                else if (register.Type == RegisterType.HoldingRegister && HoldingRegisters.Count > 0)
                {
                    ulong value = GetRegisterValue(HoldingRegisters, register);
                    if (register.Value == ulong.MaxValue && register.LastValue == ulong.MaxValue)
                    {
                        register.Value = value;
                        register.LastValue = value;
                        continue;
                    }
                }
                //InputRegisters
                else if (register.Type == RegisterType.InputRegister && InputRegisters.Count > 0)
                {
                    ulong value = GetRegisterValue(InputRegisters, register);
                    if (register.Value == ulong.MaxValue && register.LastValue == ulong.MaxValue)
                    {
                        register.Value = value;
                        register.LastValue = value;
                        continue;
                    }
                }
            }
        }
        /// <summary>
        /// 同步寄存器描述数据，会产生 Input/Output Change 事件
        /// <para>需要在线程中实时或间隔时间调用</para>
        /// </summary>
        internal void SyncRegisterChangeEvents()
        {
            for(int i = 0; i < RegistersDescription.Count; i ++)
            {
                Register register = RegistersDescription[i];
                if (!register.EnabledChangeEvent) continue;

                //CoilsStatus
                if (register.Type == RegisterType.CoilsStatus && CoilsStatus.Count > 0)
                {
                    ulong value = GetRegisterValue(CoilsStatus, register);
                    if (register.Value != value)
                    {
                        register.LastValue = register.Value;
                        register.Value = value;
                        OutputChangeHandler?.Invoke(Address, register);
                    }
                    continue;
                }
                //DiscreteInput
                else if(register.Type == RegisterType.DiscreteInput && DiscreteInputs.Count > 0)
                {
                    ulong value = GetRegisterValue(DiscreteInputs, register);
                    if (register.Value != value)
                    {
                        register.LastValue = register.Value;
                        register.Value = value;
                        InputChangeHandler?.Invoke(Address, register);
                    }
                    continue;
                }
                //HoldingRegisters
                else if (register.Type == RegisterType.HoldingRegister && HoldingRegisters.Count > 0)
                {
                    ulong value = GetRegisterValue(HoldingRegisters, register);
                    if (register.Value != value)
                    {
                        register.LastValue = register.Value;
                        register.Value = value;
                        OutputChangeHandler?.Invoke(Address, register);
                    }
                    continue;
                }
                //InputRegisters
                else if (register.Type == RegisterType.InputRegister && InputRegisters.Count > 0)
                {
                    ulong value = GetRegisterValue(InputRegisters, register);
                    if (register.Value != value)
                    {
                        register.LastValue = register.Value;
                        register.Value = value;
                        InputChangeHandler?.Invoke(Address, register);
                    }
                    continue;
                }                
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            ClearRegisters();

            ReadCoilsStatus = null;
            ReadHoldingRegisters = null;
            ReadDiscreteInputs = null;
            ReadInputRegisters = null;

            InputChangeHandler = null;
            OutputChangeHandler = null;

            CoilsStatus = null;
            DiscreteInputs = null;
            HoldingRegisters = null;
            InputRegisters = null;

            RegistersDescription = null;

            CoilsStatusAddresses = null;
            DiscreteInputsAddresses = null;
            HoldingRegistersAddresses = null;
            InputRegistersAddresses = null;
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(ModbusIODevice)}] Name:{Name} Address:0x{Address:X2}";
        }

        /// <summary>
        /// 解析 XML 格式
        /// </summary>
        /// <param name="element"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public static bool TryParse(XElement element, out ModbusIODevice device)
        {
            device = null;
            if (element == null) return false;
            if (element.Name != "Device" || String.IsNullOrWhiteSpace(element.Attribute("Address").Value))
            {
                Log.Warn($"({nameof(ModbusIODevice)}) 配置格式存在错误, {element}");
                return false;
            }
            if (!StringExtension.TryParse<byte>(element.Attribute("Address").Value, out byte address))
            {
                Log.Warn($"({nameof(ModbusIODevice)}) 配置格式存在错误, {element} 节点属性 Address 值错误");
                return false;
            }

            device = new ModbusIODevice(address, element.Attribute("Name")?.Value);
            String[] attributes = new String[] { "Unknown", "CoilsStatusCount", "DiscreteInputCount", "HoldingRegisterCount", "InputRegisterCount" };
            for (int i = 1; i < attributes.Length; i++)
            {
                if (!String.IsNullOrWhiteSpace(element.Attribute(attributes[i])?.Value))
                {
                    ushort[] args = null; // = new ushort[2] { 0, 0 }; //count|startAddress
                    StringExtension.TryParse<ushort>(element.Attribute(attributes[i]).Value, ref args, ',');

                    if (args?.Length == 0) continue;

                    ushort count = 0;
                    ushort startAddress = 0x0000;

                    if(args.Length == 1)
                    {
                        count = args[0];
                    }
                    else if(args.Length == 2)
                    {
                        count = args[1];
                        startAddress = args[0];
                    }
                    else
                    {
                        Log.Warn($"解析寄存器错误, {element} 节点属性 {attributes[i]} 值格式错误");
                        continue;
                    }
                    
                    for (ushort j = 0; j < count; j++, startAddress++)
                    {
                        if(!device.AddRegister(startAddress, (RegisterType)Enum.Parse(typeof(RegisterType), i.ToString(), true)))
                            Log.Warn($"{device} 添加寄存器失败, {element} 节点属性 {attributes[i]} 值格式错误");
                    }
                } 
            }

            if (!element.HasElements) return true;

            IEnumerable<XElement> regElements = element.Elements(nameof(Register));
            foreach(var regElement in regElements)
            {
                if (!(Register.TryParse(regElement, out Register register) && device.AddRegister(register)))
                    Log.Warn($"{device} 解析/添加寄存器失败");
            }

            return true;
        }


        /// <summary>
        /// 分割非连续的寄存器地址
        /// <para>返回 startAddress,numberOfPoints 的值键对 </para>
        /// </summary>
        /// <param name="addresses"></param>
        /// <returns>返回 startAddress,numberOfPoints 的值键对</returns>
        internal static ConcurrentDictionary<ushort, ushort> SpliteAddresses(ushort[] addresses)
        {
            //startAddress,numberOfPoints
            ConcurrentDictionary<ushort, ushort> result = new ConcurrentDictionary<ushort, ushort>();
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
                    result.TryAdd(startAddress, numberOfPoints);

                    numberOfPoints = 1;
                    startAddress = addresses[i];
                    nextAddress = (ushort)(startAddress + 1);
                }

                if (i == addresses.Length - 1)
                {
                    result.TryAdd(startAddress, numberOfPoints);
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// 跟据寄存器描述对象，从寄存器原始存储数据集中获取数据
        /// </summary>
        /// <param name="registers"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static ulong GetRegisterValue(IReadOnlyDictionary<ushort, ushort> registers, Register description)
        {            
            ulong longValue = (ulong)0;
            if (description.Count <= 0 || description.Count > 4) return longValue;

            int i = 0;

            for (ushort address = description.Address; address < description.Address + description.Count; address++, i ++)
            {
                ulong value = registers.ContainsKey(address) ? registers[address] : (ushort)0x0000;

                //BitConverter
                int bitOffset = description.IsLittleEndian ? i * 16 : (description.Count - 1 - i) * 16;
                longValue |= (ulong)(value << bitOffset);
            }

            return longValue;
        }
        /// <summary>
        /// 跟据寄存器描述对象，从寄存器原始存储数据集中获取数据
        /// </summary>
        /// <param name="registers"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static ulong GetRegisterValue(IReadOnlyDictionary<ushort, bool> registers, Register description)
        {
            ulong longValue = (ulong)0;
            if (description.Count <= 0 || description.Count > 4) return longValue;

            int i = 0;
            for (ushort address = description.Address; address < description.Address + description.Count; address++, i++)
            {
                ulong value = registers.ContainsKey(address) && registers[address] ? (byte)0x01 : (byte)0x00;

                //BitConverter
                int bitOffset = description.IsLittleEndian ? i * 1 : (description.Count - 1 - i) * 1;
                longValue |= (ulong)(value << bitOffset);
            }

            return longValue;
        }
    }

}
