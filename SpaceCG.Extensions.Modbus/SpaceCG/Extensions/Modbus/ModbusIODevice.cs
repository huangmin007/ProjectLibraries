using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Modbus.Device;
using SpaceCG.Generic;

namespace SpaceCG.Extensions.Modbus
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
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(Register));

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
        internal ushort[] RawBytes;
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
        /// <param name="enabledChangeEvent"></param>
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
        /// 解析 XML 协议格式
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
                Logger.Warn($"({nameof(Register)}) 配置格式存在错误, {element}");
                return false;
            }

            if (StringExtensions.TryParse<ushort>(element.Attribute("Address")?.Value, out ushort address) &&
                Enum.TryParse<RegisterType>(element.Attribute("Type")?.Value, true, out RegisterType type) && type != RegisterType.Unknown)
            {
                bool result = byte.TryParse(element.Attribute("Count")?.Value, out byte count);
                register = new Register(address, type, result ? count : (byte)0x01);
            }
            else
            {
                Logger.Warn($"({nameof(Register)}) 配置格式存在错误, {element} 节点属性 Address, Type 值错误");
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
    public class ModbusIODevice : IDisposable
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(ModbusIODevice));

        /// <summary>
        /// Read Only 寄存器数据 Change 处理
        /// <para>(ModbusIODevice ioDevice, Register register)</para>
        /// </summary>
        internal event Action<ModbusIODevice, Register> InputChangeHandler;
        /// <summary>
        /// Read Write 寄存器数据 Change 处理
        /// <para>(ModbusIODevice ioDevice, Register register)</para>
        /// </summary>
        internal event Action<ModbusIODevice, Register> OutputChangeHandler;

        /// <summary>
        /// 设备名称
        /// </summary>
        public String Name { get; private set; } = null;
        /// <summary>
        /// 设备地址
        /// </summary>
        public byte Address { get; private set; } = 0x01;
        /// <summary>
        /// 当前设备上的寄存器集合，用于描述寄存器及数据
        /// </summary>
        public List<Register> Registers { get; private set; } = new List<Register>(32);
        //public ConcurrentBag<Register> Registers { get; private set; } = new ConcurrentBag<Register>();

        /// <summary>
        /// 寄存器数量
        /// </summary>
        private int RegistersCount = 0;

        /// <summary> 线圈状态，原始数据存储对象，数据类型为 bool 类型，RW-ReadWrite </summary>
        internal Dictionary<ushort, bool> RawCoilsStatus { get; private set; } = new Dictionary<ushort, bool>(16);
        /// <summary> 离散输入，原始数据存储对象，数据类型为 bool 类型，RO-ReadOnly </summary>
        private Dictionary<ushort, bool> RawDiscreteInputs { get; set; } = new Dictionary<ushort, bool>(16);
        /// <summary>保持寄存器，原始数据存储对象，数据类型为 ushort 类型，RW-ReadyWrite </summary>
        private Dictionary<ushort, ushort> RawHoldingRegisters { get; set; } = new Dictionary<ushort, ushort>(16);
        /// <summary> 输入寄存器，原始数据存储对象，数据类型为 ushort 类型，RO-ReadOnly </summary>
        private Dictionary<ushort, ushort> RawInputRegisters { get; set; } = new Dictionary<ushort, ushort>(16);

        /// <summary> startAddress, numberOfPoint </summary>
        private Dictionary<ushort, ushort> RawCoilsStatusAddresses = new Dictionary<ushort, ushort>();
        /// <summary> startAddress, numberOfPoint </summary>
        private Dictionary<ushort, ushort> RawDiscreteInputsAddresses = new Dictionary<ushort, ushort>();
        /// <summary> startAddress, numberOfPoint </summary>
        private Dictionary<ushort, ushort> RawHoldingRegistersAddresses = new Dictionary<ushort, ushort>();
        /// <summary> startAddress, numberOfPoint </summary>
        private Dictionary<ushort, ushort> RawInputRegistersAddresses = new Dictionary<ushort, ushort>();

        private IModbusMaster Master;

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

        /// <summary>
        /// 清除所有寄存器
        /// </summary>
        private void ClearRawRegisters()
        {
            RawCoilsStatus.Clear();
            RawDiscreteInputs.Clear();
            RawHoldingRegisters.Clear();
            RawInputRegisters.Clear();

            RawCoilsStatusAddresses.Clear();
            RawDiscreteInputsAddresses.Clear();
            RawHoldingRegistersAddresses.Clear();
            RawInputRegistersAddresses.Clear();
        }

        /// <summary>
        /// 更新寄存器数据
        /// </summary>
        /// <param name="startAddress"></param>
        /// <param name="type"></param>
        /// <param name="values"></param>
        internal void UpdateRawRegisterValues(ushort startAddress, RegisterType type, bool[] values)
        {
            if (values == null || type == RegisterType.Unknown) return;

            ushort address, i = 0;
            if (type == RegisterType.CoilsStatus)
            {
                for (address = startAddress; address < startAddress + values.Length; address++, i++)
                {
                    if (RawCoilsStatus.ContainsKey(address)) RawCoilsStatus[address] = values[i];
                    else RawCoilsStatus.Add(address, values[i]);
                }
            }
            else if (type == RegisterType.DiscreteInput)
            {
                for (address = startAddress; address < startAddress + values.Length; address++, i++)
                {
                    if (RawDiscreteInputs.ContainsKey(address)) RawDiscreteInputs[address] = values[i];
                    else RawDiscreteInputs.Add(address, values[i]);
                }
            }
        }
        /// <summary>
        /// 更新寄存器数据
        /// </summary>
        /// <param name="startAddress"></param>
        /// <param name="type"></param>
        /// <param name="values"></param>
        internal void UpdateRawRegisterValues(ushort startAddress, RegisterType type, ushort[] values)
        {
            if (values == null || type == RegisterType.Unknown) return;

            ushort address, i = 0;
            if (type == RegisterType.HoldingRegister)
            {
                for (address = startAddress; address < startAddress + values.Length; address++, i++)
                {
                    if (RawHoldingRegisters.ContainsKey(address)) RawHoldingRegisters[address] = values[i];
                    else RawHoldingRegisters.Add(address, values[i]);
                }
            }
            else if (type == RegisterType.InputRegister)
            {
                for (address = startAddress; address < startAddress + values.Length; address++, i++)
                {
                    if (RawInputRegisters.ContainsKey(address)) RawInputRegisters[address] = values[i];
                    else RawInputRegisters.Add(address, values[i]);
                }
            }
        }

        /// <summary>
        /// 初使化设备，将寄存器数据归零，寄存器描述归零
        /// <para>对寄存器 添加、移除操作 全部完成后初使化设备</para>
        /// </summary>
        internal void InitializeDevice(IModbusMaster master)
        {
            this.Master = master;

            ClearRawRegisters();
            RegistersCount = Registers.Count;
            foreach (Register register in Registers)
            {
                byte count = register.Count;
                ushort startAddress = register.Address;

                switch (register.Type) 
                {
                    case RegisterType.CoilsStatus:
                        for (ushort address = startAddress; address < startAddress + count; address++)
                        {
                            if (!RawCoilsStatus.ContainsKey(address)) RawCoilsStatus.Add(address, default);
                        }
                        break;

                    case RegisterType.DiscreteInput:
                        for (ushort address = startAddress; address < startAddress + count; address++)
                        {
                            if (!RawDiscreteInputs.ContainsKey(address)) RawDiscreteInputs.Add(address, default);
                        }
                        break;

                    case RegisterType.HoldingRegister:
                        for (ushort address = startAddress; address < startAddress + count; address++)
                        {
                            if (!RawHoldingRegisters.ContainsKey(address)) RawHoldingRegisters.Add(address, default);
                        }
                        break;

                    case RegisterType.InputRegister:
                        for (ushort address = startAddress; address < startAddress + count; address++)
                        {
                            if (!RawInputRegisters.ContainsKey(address)) RawInputRegisters.Add(address, default);
                        }
                        break;
                }
            }
            
            RawCoilsStatusAddresses = SpliteAddresses(RawCoilsStatus.Keys.ToArray());
            RawDiscreteInputsAddresses = SpliteAddresses(RawDiscreteInputs.Keys.ToArray());
            RawHoldingRegistersAddresses = SpliteAddresses(RawHoldingRegisters.Keys.ToArray());
            RawInputRegistersAddresses = SpliteAddresses(RawInputRegisters.Keys.ToArray());

            foreach (var reg in Registers)
            {
                reg.Value = ulong.MaxValue;
                reg.LastValue = ulong.MaxValue;
            }

            SyncInputRegisters();
            SyncOutputRegisters();
            InitializeRegistersEvents();

            Logger.Info($"{this} Initialize Device.");
        }

        /// <summary>
        /// 同步传输输出寄存器
        /// <para>需要在线程中实时或间隔时间调用，一般只用于初使化，或长时间间隔更新对比</para>
        /// </summary>
        internal void SyncOutputRegisters()
        {
            if (RawCoilsStatus?.Count > 0 && Master?.Transport != null)
            {
                try
                {
                    foreach (var kv in RawCoilsStatusAddresses)
                    {
                        bool[] result = Master?.ReadCoils(Address, kv.Key, kv.Value);
                        if (result != null) UpdateRawRegisterValues(kv.Key, RegisterType.CoilsStatus, result);
                        else break;                        
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"{this}");
                    Logger.Error(ex);
                    Thread.Sleep(300);
                }
            }

            if (RawHoldingRegisters?.Count > 0 && Master?.Transport != null)
            {
                try
                {
                    foreach (var kv in RawHoldingRegistersAddresses)
                    {
                        ushort[] result = Master?.ReadHoldingRegisters(Address, kv.Key, kv.Value);
                        if (result != null) UpdateRawRegisterValues(kv.Key, RegisterType.HoldingRegister, result);
                        else break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"{this}");
                    Logger.Error(ex);
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
            if (RawDiscreteInputs?.Count > 0 && Master?.Transport != null)
            {
                try
                {
                    foreach (var kv in RawDiscreteInputsAddresses)
                    {
                        bool[] result = Master?.ReadInputs(Address, kv.Key, kv.Value);
                        if (result != null) UpdateRawRegisterValues(kv.Key, RegisterType.DiscreteInput, result);
                        else break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"{this}");
                    Logger.Error(ex);
                    Thread.Sleep(300);
                }
            }

            if (RawInputRegisters?.Count > 0 && Master?.Transport != null)
            {
                try
                {
                    foreach (var kv in RawInputRegistersAddresses)
                    {
                        ushort[] result = Master?.ReadInputRegisters(Address, kv.Key, kv.Value);
                        if (result != null) UpdateRawRegisterValues(kv.Key, RegisterType.InputRegister, result);
                        else break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"{this}");
                    Logger.Error(ex);
                    Thread.Sleep(300);
                }
            }
        }

        /// <summary>
        /// 初使化寄存器的 Value, LastValue 为事件处理做准备
        /// </summary>
        private void InitializeRegistersEvents()
        {
            foreach(Register register in Registers)
            {
                //CoilsStatus
                if (register.Type == RegisterType.CoilsStatus && RawCoilsStatus.Count > 0)
                {
                    ulong value = GetRegisterValue(RawCoilsStatus, register);
                    if (register.Value == ulong.MaxValue && register.LastValue == ulong.MaxValue)
                    {
                        register.Value = value;
                        register.LastValue = value;
                        continue;
                    }
                }
                //DiscreteInput
                else if (register.Type == RegisterType.DiscreteInput && RawDiscreteInputs.Count > 0)
                {
                    ulong value = GetRegisterValue(RawDiscreteInputs, register);
                    if (register.Value == ulong.MaxValue && register.LastValue == ulong.MaxValue)
                    {
                        register.Value = value;
                        register.LastValue = value;
                        continue;
                    }
                }
                //HoldingRegisters
                else if (register.Type == RegisterType.HoldingRegister && RawHoldingRegisters.Count > 0)
                {
                    ulong value = GetRegisterValue(RawHoldingRegisters, register);
                    if (register.Value == ulong.MaxValue && register.LastValue == ulong.MaxValue)
                    {
                        register.Value = value;
                        register.LastValue = value;
                        continue;
                    }
                }
                //InputRegisters
                else if (register.Type == RegisterType.InputRegister && RawInputRegisters.Count > 0)
                {
                    ulong value = GetRegisterValue(RawInputRegisters, register);
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
            foreach(Register register in Registers)
            {
                //CoilsStatus
                if (register.Type == RegisterType.CoilsStatus && RawCoilsStatus.Count > 0)
                {
                    ulong value = GetRegisterValue(RawCoilsStatus, register);
                    if (register.Value != value)
                    {
                        register.LastValue = register.Value;
                        register.Value = value;
                        if(register.EnabledChangeEvent) OutputChangeHandler?.Invoke(this, register);
                    }
                    continue;
                }
                //DiscreteInput
                else if(register.Type == RegisterType.DiscreteInput && RawDiscreteInputs.Count > 0)
                {
                    ulong value = GetRegisterValue(RawDiscreteInputs, register);
                    if (register.Value != value)
                    {
                        register.LastValue = register.Value;
                        register.Value = value;
                        if (register.EnabledChangeEvent) InputChangeHandler?.Invoke(this, register);
                    }
                    continue;
                }
                //HoldingRegisters
                else if (register.Type == RegisterType.HoldingRegister && RawHoldingRegisters.Count > 0)
                {
                    ulong value = GetRegisterValue(RawHoldingRegisters, register);
                    if (register.Value != value)
                    {
                        register.LastValue = register.Value;
                        register.Value = value;
                        if (register.EnabledChangeEvent) OutputChangeHandler?.Invoke(this, register);
                    }
                    continue;
                }
                //InputRegisters
                else if (register.Type == RegisterType.InputRegister && RawInputRegisters.Count > 0)
                {
                    ulong value = GetRegisterValue(RawInputRegisters, register);

                    if (register.Value != value)
                    {
                        register.LastValue = register.Value;
                        register.Value = value;
                        if (register.EnabledChangeEvent) InputChangeHandler?.Invoke(this, register);
                    }
                    continue;
                }                
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Master = null;

            ClearRawRegisters();
            Registers.Clear();
            Registers = null;
            
            InputChangeHandler = null;
            OutputChangeHandler = null;

            RawCoilsStatus = null;
            RawDiscreteInputs = null;
            RawHoldingRegisters = null;
            RawInputRegisters = null;

            RawCoilsStatusAddresses = null;
            RawDiscreteInputsAddresses = null;
            RawHoldingRegistersAddresses = null;
            RawInputRegistersAddresses = null;
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(ModbusIODevice)}] Name:{Name} Address:0x{Address:X2}";
        }

        /// <summary>
        /// 解析 XML 协议格式
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
                Logger.Warn($"({nameof(ModbusIODevice)}) 配置格式存在错误, {element}");
                return false;
            }
            if (!StringExtensions.TryParse<byte>(element.Attribute("Address").Value, out byte address))
            {
                Logger.Warn($"({nameof(ModbusIODevice)}) 配置格式存在错误, {element} 节点属性 Address 值错误");
                return false;
            }

            device = new ModbusIODevice(address, element.Attribute("Name")?.Value);
            String[] attributes = new String[] { "Unknown", "CoilsStatusCount", "DiscreteInputCount", "HoldingRegisterCount", "InputRegisterCount" };
            for (int i = 1; i < attributes.Length; i++)
            {
                if (String.IsNullOrWhiteSpace(element.Attribute(attributes[i])?.Value)) continue;

                ushort[] args = null; // = new ushort[2] { 0, 0 }; //count|startAddress
                StringExtensions.TryParse<ushort>(element.Attribute(attributes[i]).Value, ref args, ',');

                if (args?.Length == 0) continue;

                ushort count = 0;
                ushort startAddress = 0x0000;

                if (args.Length == 1)
                {
                    count = args[0];
                }
                else if (args.Length == 2)
                {
                    count = args[1];
                    startAddress = args[0];
                }
                else
                {
                    Logger.Warn($"解析寄存器错误, {element} 节点属性 {attributes[i]} 值格式错误");
                    continue;
                }

                for (ushort j = 0; j < count; j++, startAddress++)
                {
                    device.Registers.Add(new Register(startAddress, (RegisterType)Enum.Parse(typeof(RegisterType), i.ToString(), true), 1));

                    //if (!device.AddRegister(startAddress, (RegisterType)Enum.Parse(typeof(RegisterType), i.ToString(), true)))
                    //    Logger.Warn($"{device} 添加寄存器失败, {element} 节点属性 {attributes[i]} 值格式错误");
                }
            }

            if (!element.HasElements) return true;

            //Registers
            IEnumerable<XElement> regElements = element.Elements(nameof(Register));
            foreach(var regElement in regElements)
            {
                if (!Register.TryParse(regElement, out Register register)) device.Registers.Add(register);
            }

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
        /// 跟据寄存器描述对象，从寄存器原始存储数据集中获取数据
        /// </summary>
        /// <param name="registers"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        internal static ulong GetRegisterValue(IReadOnlyDictionary<ushort, ushort> registers, Register description)
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
                //Console.WriteLine($"{description.IsLittleEndian},{address},,{value},,{bitOffset},,{longValue}");

            }

            return longValue;
        }
        /// <summary>
        /// 跟据寄存器描述对象，从寄存器原始存储数据集中获取数据
        /// </summary>
        /// <param name="registers"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        internal static ulong GetRegisterValue(IReadOnlyDictionary<ushort, bool> registers, Register description)
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
