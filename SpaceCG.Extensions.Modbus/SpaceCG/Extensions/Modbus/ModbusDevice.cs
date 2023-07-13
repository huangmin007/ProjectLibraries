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
        public byte Count { get; private set; } = 1;       

        /// <summary>
        /// 位偏移量，暂时保留
        /// </summary>
        public byte Offset { get; private set; } = 0;

        /// <summary>
        /// 寄存器名称
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// 寄存器单位描述，暂时保留
        /// </summary>
        public string Units { get; set; } = null;

        /// <summary>
        /// 字节顺序(Endianness)，低地址数据在后，高地址数据在前
        /// </summary>
        public bool IsLittleEndian { get; private set; } = true;

        /// <summary>
        /// 允许 IO Change 事件
        /// </summary>
        public bool EnabledChangeEvent { get; set; } = true;

        /// <summary>
        /// 64位 寄存器的最新数据
        /// </summary>
        public long Value { get; internal set; } = long.MaxValue;

        /// <summary>
        /// 64位 寄存器的上一次的数据
        /// </summary>
        public long LastValue { get; internal set; } = long.MaxValue;

#if false
        /// <summary>
        /// 寄存器的原始数据存储块，待我思考一会儿，用于未来出现 大于64位数据时 在升级吧~~~
        /// </summary>
        internal ushort[] RawBytes {get; private set;}
#endif

        /// <summary>
        /// 寄存器描述对象
        /// </summary>
        /// <param name="address"></param>
        /// <param name="type"></param>
        /// <param name="count"></param>
        /// <param name="isLittleEndian"></param>
        /// <exception cref="ArgumentOutOfRangeException">寄存器类型不支持连接的数量</exception>
        public Register(ushort address, RegisterType type, byte count = 1, bool isLittleEndian = true)
        {
            if (type == RegisterType.HoldingRegister || type == RegisterType.InputRegister)
            {
                if (count < 1 || count > 4) throw new ArgumentOutOfRangeException(nameof(count), $"寄存器类型 {type} 暂时只支持连续的 1~4 个");
            }
            if (type == RegisterType.CoilsStatus || type == RegisterType.DiscreteInput)
            {
                if (count < 1 || count > 64) throw new ArgumentOutOfRangeException(nameof(count), $"寄存器类型 {type} 暂时只支持连续的 1~64 个");
            }

            this.Type = type;
            this.Count = count;
            this.Address = address;
            this.IsLittleEndian = isLittleEndian;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(Register)}#0x{Address:X4}  {nameof(Type)}:{Type}  {nameof(Count)}:{Count}  {nameof(Value)}:{Value}";
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

            if (StringExtensions.ToNumber(element.Attribute(nameof(Address))?.Value, out ushort address) &&
                Enum.TryParse(element.Attribute(nameof(Type))?.Value, true, out RegisterType type) && type != RegisterType.Unknown)
            {
                bool result = byte.TryParse(element.Attribute(nameof(Count))?.Value, out byte count);
                register = new Register(address, type, result ? count : (byte)0x01);
            }
            else
            {
                Logger.Warn($"({nameof(Register)}) 配置格式存在错误, {element} 节点属性 Address, Type 值错误");
                return false;
            }

            register.Name = element.Attribute(nameof(Name))?.Value;
            register.Units = element.Attribute(nameof(Units))?.Value;
            register.Offset = byte.TryParse(element.Attribute(nameof(Offset))?.Value, out byte offset) ? offset : (byte)0x00;
            register.IsLittleEndian = bool.TryParse(element.Attribute(nameof(IsLittleEndian))?.Value, out bool isLittleEndian) ? isLittleEndian : true;
            register.EnabledChangeEvent = bool.TryParse(element.Attribute(nameof(EnabledChangeEvent))?.Value, out bool enabledChangeEvent) ? enabledChangeEvent : true;

            return true;
        }
    }

    /// <summary>
    /// Modbus Input/Output Device 对象
    /// </summary>
    public class ModbusDevice : IDisposable
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(ModbusDevice));

        /// <summary>
        /// Read Only 寄存器数据 Change 处理
        /// <para>(ModbusIODevice ioDevice, Register register)</para>
        /// </summary>
        internal event Action<ModbusDevice, Register> InputChangeHandler;
        /// <summary>
        /// Read Write 寄存器数据 Change 处理
        /// <para>(ModbusIODevice ioDevice, Register register)</para>
        /// </summary>
        internal event Action<ModbusDevice, Register> OutputChangeHandler;

        /// <summary> 设备名称 </summary>
        public String Name { get; set; }
        /// <summary> 设备地址  </summary>
        public byte Address { get; private set; } = 0x01;
        /// <summary> 当前设备上的寄存器集合，用于描述寄存器及数据  </summary>
        public List<Register> Registers { get; private set; } = new List<Register>(32);
        /// <summary> 寄存器数量  </summary>
        internal int RegistersCount { get; private set; } = 0;

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
#if false
        /// <summary>
        /// 允许输入同步寄存器
        /// </summary>
        public bool EnabledInputSync { get; set; } = true;
#endif
        /// <summary>
        /// Modbus Input/Output Device 构造函数
        /// </summary>
        /// <param name="address"></param>
        public ModbusDevice(byte address)
        {
            this.Address = address;
            this.Name = $"{nameof(ModbusDevice)}#{Address:00}";
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
            InitializeRegisters();
            Logger.Info($"{this} Initialize Device.");
        }

        private void InitializeRegisters()
        {
            if (RegistersCount == Registers.Count) return;

            ClearRawRegisters();
            RegistersCount = Registers.Count;

            foreach (Register register in Registers)
            {
                byte count = register.Count;
                ushort startAddress = register.Address;

                register.Value = long.MaxValue;
                register.LastValue = long.MaxValue;

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
#if false
            foreach (Register reg in Registers)
            {
                reg.Value = long.MaxValue;
                reg.LastValue = long.MaxValue;
            }
#endif
            SyncInputRegisters();
            SyncOutputRegisters();
            InitializeRegistersValues();
        }

        /// <summary>
        /// 同步传输输出寄存器
        /// <para>需要在线程中实时或间隔时间调用，一般只用于初使化，或长时间间隔更新对比</para>
        /// </summary>
        internal void SyncOutputRegisters()
        {
            InitializeRegisters();

            if (RawCoilsStatus?.Count > 0 && Master?.Transport != null)
            {
                try
                {
                    foreach (var kv in RawCoilsStatusAddresses)
                    {
                        //if (Master?.Transport == null) break;
                        bool[] result = Master?.ReadCoils(Address, kv.Key, kv.Value);
                        if (result != null) UpdateRawRegisterValues(kv.Key, RegisterType.CoilsStatus, result);
                        else break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"{this}");
                    Logger.Error(ex);
                    Thread.Sleep(100);
                }
            }

            if (RawHoldingRegisters?.Count > 0 && Master?.Transport != null)
            {
                try
                {
                    foreach (var kv in RawHoldingRegistersAddresses)
                    {
                        //if (Master?.Transport == null) break;
                        ushort[] result = Master?.ReadHoldingRegisters(Address, kv.Key, kv.Value);
                        if (result != null) UpdateRawRegisterValues(kv.Key, RegisterType.HoldingRegister, result);
                        else break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"{this}");
                    Logger.Error(ex);
                    Thread.Sleep(100);
                }
            }
        }
        /// <summary>
        /// 同步传输输入寄存器
        /// <para>需要在线程中实时或间隔时间调用</para>
        /// </summary>
        internal void SyncInputRegisters()
        {
            InitializeRegisters();

            if (RawDiscreteInputs?.Count > 0 && Master?.Transport != null)
            {
                try
                {
                    foreach (var kv in RawDiscreteInputsAddresses)
                    {
                        //if (Master?.Transport == null) break;
                        bool[] result = Master?.ReadInputs(Address, kv.Key, kv.Value);
                        if (result != null) UpdateRawRegisterValues(kv.Key, RegisterType.DiscreteInput, result);
                        else break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"{this}");
                    Logger.Error(ex);
                    Thread.Sleep(100);
                }
            }

            if (RawInputRegisters?.Count > 0 && Master?.Transport != null)
            {
                try
                {
                    foreach (var kv in RawInputRegistersAddresses)
                    {
                        //if (Master?.Transport == null) break;
                        ushort[] result = Master?.ReadInputRegisters(Address, kv.Key, kv.Value);
                        if (result != null) UpdateRawRegisterValues(kv.Key, RegisterType.InputRegister, result);
                        else break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"{this}");
                    Logger.Error(ex);
                    Thread.Sleep(100);
                }
            }
        }

        /// <summary>
        /// 初使化寄存器的 Value, LastValue 为事件处理做准备
        /// </summary>
        private void InitializeRegistersValues()
        {
            foreach (Register register in Registers)
            {
                //CoilsStatus
                if (register.Type == RegisterType.CoilsStatus && RawCoilsStatus.Count > 0)
                {
                    long value = GetRegisterValue(RawCoilsStatus, register);
                    if (register.Value == long.MaxValue && register.LastValue == long.MaxValue)
                    {
                        register.Value = value;
                        register.LastValue = value;
                        continue;
                    }
                }
                //DiscreteInput
                else if (register.Type == RegisterType.DiscreteInput && RawDiscreteInputs.Count > 0)
                {
                    long value = GetRegisterValue(RawDiscreteInputs, register);
                    if (register.Value == long.MaxValue && register.LastValue == long.MaxValue)
                    {
                        register.Value = value;
                        register.LastValue = value;
                        continue;
                    }
                }
                //HoldingRegisters
                else if (register.Type == RegisterType.HoldingRegister && RawHoldingRegisters.Count > 0)
                {
                    long value = GetRegisterValue(RawHoldingRegisters, register);
                    if (register.Value == long.MaxValue && register.LastValue == long.MaxValue)
                    {
                        register.Value = value;
                        register.LastValue = value;
                        continue;
                    }
                }
                //InputRegisters
                else if (register.Type == RegisterType.InputRegister && RawInputRegisters.Count > 0)
                {
                    long value = GetRegisterValue(RawInputRegisters, register);
                    if (register.Value == long.MaxValue && register.LastValue == long.MaxValue)
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
            foreach (Register register in Registers)
            {
                //CoilsStatus
                if (register.Type == RegisterType.CoilsStatus && RawCoilsStatus.Count > 0)
                {
                    long value = GetRegisterValue(RawCoilsStatus, register);
                    //check new value
                    if (register.Value != value)
                    {
                        register.LastValue = register.Value;
                        register.Value = value;
                        if (register.EnabledChangeEvent) OutputChangeHandler?.Invoke(this, register);
                    }
                }
                //DiscreteInput
                else if (register.Type == RegisterType.DiscreteInput && RawDiscreteInputs.Count > 0)
                {
                    long value = GetRegisterValue(RawDiscreteInputs, register);
                    if (register.Value != value)
                    {
                        register.LastValue = register.Value;
                        register.Value = value;
                        if (register.EnabledChangeEvent) InputChangeHandler?.Invoke(this, register);
                    }
                }
                //HoldingRegisters
                else if (register.Type == RegisterType.HoldingRegister && RawHoldingRegisters.Count > 0)
                {
                    long value = GetRegisterValue(RawHoldingRegisters, register);
                    if (register.Value != value)
                    {
                        register.LastValue = register.Value;
                        register.Value = value;
                        if (register.EnabledChangeEvent) OutputChangeHandler?.Invoke(this, register);
                    }
                }
                //InputRegisters
                else if (register.Type == RegisterType.InputRegister && RawInputRegisters.Count > 0)
                {
                    long value = GetRegisterValue(RawInputRegisters, register);
                    if (register.Value != value)
                    {
                        register.LastValue = register.Value;
                        register.Value = value;
                        if (register.EnabledChangeEvent) InputChangeHandler?.Invoke(this, register);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Master = null;

            ClearRawRegisters();
            Registers.Clear();

            /*
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
            */
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(ModbusDevice)}] {nameof(Name)}:{Name} {nameof(Address)}:0x{Address:X2}";
        }

        /// <summary>
        /// 解析 XML 协议格式
        /// </summary>
        /// <param name="element"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public static bool TryParse(XElement element, out ModbusDevice device)
        {
            device = null;
            if (element == null) return false;
            if (element.Name.LocalName != nameof(ModbusDevice) || string.IsNullOrWhiteSpace(element.Attribute(nameof(Address))?.Value))
            {
                Logger.Warn($"({nameof(ModbusDevice)}) 配置格式存在错误, {element}");
                return false;
            }
            if (!StringExtensions.ToNumber(element.Attribute(nameof(Address)).Value, out byte address))
            {
                Logger.Warn($"({nameof(ModbusDevice)}) 配置格式存在错误, {element} 节点属性 Address 值错误");
                return false;
            }

            device = new ModbusDevice(address);
            device.Name = element.Attribute(nameof(Name))?.Value;
            string[] attributes = new string[] { nameof(RegisterType.Unknown), $"{nameof(RegisterType.CoilsStatus)}Count", $"{nameof(RegisterType.DiscreteInput)}Count", $"{nameof(RegisterType.HoldingRegister)}Count", $"{nameof(RegisterType.InputRegister)}Count" };
            for (int i = 1; i < attributes.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(element.Attribute(attributes[i])?.Value)) continue;

                ushort[] args = null; //= new ushort[2] { 0, 0 }; //count|startAddress
                StringExtensions.ToNumberArray(element.Attribute(attributes[i]).Value, ref args, ',');

                if (args?.Length == 0) continue;

                ushort count = 0;
                ushort startAddress = 0x0000;
                if (args.Length == 1)
                {
                    count = args[0];
                }
                else if (args.Length == 2)
                {
                    startAddress = args[0];
                    count = args[1];
                }
                else
                {
                    Logger.Warn($"解析寄存器错误, {element} 节点属性 {attributes[i]} 值格式错误");
                    continue;
                }

                RegisterType type = (RegisterType)Enum.Parse(typeof(RegisterType), i.ToString(), true);
                for (ushort j = 0; j < count; j++, startAddress++)
                {
                    device.Registers.Add(new Register(startAddress, type));
                }
            }

            if (!element.HasElements) return device.Registers.Count > 0;

            //Registers
            IEnumerable<XElement> registerElements = element.Elements(nameof(Register));
            foreach (XElement reggisterElement in registerElements)
            {
                if (!Register.TryParse(reggisterElement, out Register register))
                    device.Registers.Add(register);
            }

            return device.Registers.Count > 0;
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
        /// <param name="rawRegisters"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        internal static long GetRegisterValue(IReadOnlyDictionary<ushort, ushort> rawRegisters, Register description)
        {
            long longValue = (long)0;
            if (description.Count <= 0 || description.Count > 4) return longValue;

            int i = 0;

            for (ushort address = description.Address; address < description.Address + description.Count; address++, i++)
            {
                long value = rawRegisters.ContainsKey(address) ? rawRegisters[address] : (ushort)0x0000;

                //BitConverter
                int bitOffset = description.IsLittleEndian ? i * 16 : (description.Count - 1 - i) * 16;
                longValue |= (long)(value << bitOffset);
                //Console.WriteLine($"{description.IsLittleEndian},{address},,{value},,{bitOffset},,{longValue}");
            }

            return longValue;
        }
        /// <summary>
        /// 跟据寄存器描述对象，从寄存器原始存储数据集中获取数据
        /// </summary>
        /// <param name="rawRegisters"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        internal static long GetRegisterValue(IReadOnlyDictionary<ushort, bool> rawRegisters, Register description)
        {
            long longValue = (long)0;
            if (description.Count <= 0 || description.Count > 4) return longValue;

            int i = 0;
            for (ushort address = description.Address; address < description.Address + description.Count; address++, i++)
            {
                long value = rawRegisters.ContainsKey(address) && rawRegisters[address] ? (byte)0x01 : (byte)0x00;

                //BitConverter
                int bitOffset = description.IsLittleEndian ? i * 1 : (description.Count - 1 - i) * 1;
                longValue |= (long)(value << bitOffset);
            }

            return longValue;
        }
    }
}
