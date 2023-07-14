using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Modbus.Device;
using SpaceCG.Extensions;
using SpaceCG.Net;
using SpaceCG.Extensions.Modbus;
using ModbusDevice = SpaceCG.Extensions.Modbus.ModbusDevice;

namespace SpaceCG.Generic
{
    /// <summary>
    /// 连接管理，单例对象
    /// </summary>
    public class ConnectionManagement
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(ConnectionManagement));

        /// <summary> <see cref="XConnections"/> Name </summary>
        public const string XConnections = "Connections";
        /// <summary> <see cref="XConnection"/> Name </summary>
        public const string XConnection = "Connection";
        /// <summary> <see cref="XParameters"/> Name </summary>
        public const string XParameters = "Parameters";

        /// <summary> <see cref="XData"/> Name </summary>
        public const string XData = "Data";
        /// <summary> <see cref="XDisposed"/> Name </summary>
        public const string XDisposed = "Disposed";
        /// <summary> <see cref="XInitialized"/> Name </summary>
        public const string XInitialized = "Initialized";
        /// <summary> <see cref="XInputChanged"/> Name </summary>
        public const string XInputChanged = "InputChanged";
        /// <summary> <see cref="XOutputChanged"/> Name </summary>
        public const string XOutputChanged = "OutputChanged";

        /// <summary> <see cref="XBytes"/> Name </summary>
        public const string XBytes = "Bytes";
        /// <summary> <see cref="XMessage"/> Name </summary>
        public const string XMessage = "Message";
        /// <summary> <see cref="XMinValue"/> Name </summary>
        public const string XMinValue = "MinValue";
        /// <summary> <see cref="XMaxValue"/> Name </summary>
        public const string XMaxValue = "MaxValue";
        /// <summary> <see cref="XDeviceAddress"/> Name </summary>
        public const string XDeviceAddress = "DeviceAddress";

        /// <summary>
        /// Connection Management Name
        /// </summary>
        public string Name { get; private set;} = null;
        /// <summary>
        /// Connection Management Reflection Controller
        /// </summary>
        public ReflectionController ReflectionController { get; set; } = null;

        private IEnumerable<XElement> ConnectionElements;
        private ConcurrentDictionary<string, IReadOnlyCollection<DataEventParams>> ConnectionDataEvents;

        /// <summary>
        /// 连接管理实例
        /// </summary>
        public static ConnectionManagement Instance => instance;
        private static readonly ConnectionManagement instance = new ConnectionManagement();
        
        /// <summary>
        /// 连接管理对象
        /// </summary>
        private ConnectionManagement()
        {
            this.ConnectionDataEvents = new ConcurrentDictionary<string, IReadOnlyCollection<DataEventParams>>();
        }

        /// <summary>
        /// 解析连接配置，并将正确认的连接添加到 <see cref="ReflectionController.AccessObjects"/> 集合中
        /// <code>//配置示例：
        /// &lt;Connections Name="ConnectionManagement"&gt;
        ///     &lt;Connection Name = "tcpServer" Type="TcpServer" Parameters="0.0.0.0,3000" /&gt;
        ///     &lt;Connection Name = "tcp" Type="TcpClient" Parameters="127.0.0.1,9600" ReadTimeout="30" &gt;
        ///         &lt;Event Type = "Data" Message="Hello" &gt;
        ///             &lt;Action Target = "Bus.#01" Method="TurnSingleCoil" Params="0x02, 1" /&gt;
        ///         &lt;/Event&gt;
        ///         &lt;Event Type = "Data" Bytes="0x01,0x02,0x03" &gt;
        ///             &lt;Action Target = "Bus.#01" Method="TurnSingleCoil" Params="0x02, 1" /&gt;
        ///         &lt;/Event&gt;
        ///     &lt;/Connection&gt;
        /// &lt;/Connections&gt;
        /// </code>
        /// </summary>
        /// <param name="connectionsElement">至少具有 Name, Type, Parameters 属性的 Connection 节点集合 </param>
        public void TryParseConnectionConfiguration(XElement connectionsElement)
        {
            if (connectionsElement == null) throw new ArgumentNullException(nameof(connectionsElement), "参数不能为空");
            if (ReflectionController == null) throw new InvalidOperationException($"{nameof(ReflectionController)} 不能为空，请先配置 {nameof(ReflectionController)} 对象");
            
            Disconnections();

            string name = connectionsElement.Attribute(ReflectionController.XName)?.Value;
            this.Name = !string.IsNullOrWhiteSpace(name) ? name : nameof(ConnectionManagement);
            this.ReflectionController.AccessObjects.Add(this.Name, this);
            this.ConnectionElements = connectionsElement.Elements(XConnection);

            foreach (XElement connectionElement in ConnectionElements)
            {
                if (connectionElement.Name.LocalName != XConnection) continue;
                name = connectionElement.Attribute(ReflectionController.XName)?.Value;
                if (string.IsNullOrWhiteSpace(name))
                {
                    Logger.Warn($"配置格式错误, 属性 {ReflectionController.XName} 不能为空, {connectionElement}");
                    continue;
                }
                if (this.ReflectionController.AccessObjects.ContainsKey(name))
                {
                    Logger.Warn($"配置错误, 访问对象名称 {name} 已存在, {connectionElement}");
                    continue;
                }

                if (!CreateConnection(connectionElement, out object connectionObject)) continue;

                ReflectionController.AccessObjects.Add(name, connectionObject);
                IReadOnlyCollection<DataEventParams> dataEvents = GetConnectionDataEvents(connectionElement.Elements(ReflectionController.XEvent));
                ConnectionType connectionType = (ConnectionType)Enum.Parse(typeof(ConnectionType), connectionElement.Attribute(ReflectionController.XType).Value, true);

                switch (connectionType)
                {
                    case ConnectionType.ModbusRtu:
                    case ConnectionType.ModbusTcp:
                    case ConnectionType.ModbusUdp:
                    case ConnectionType.ModbusTcpRtu:
                    case ConnectionType.ModbusUdpRtu:
                        IEnumerable<XElement> deviceElements = connectionElement.Elements(nameof(ModbusDevice));
                        IEnumerable<XElement> eventElements = connectionElement.Elements(ReflectionController.XEvent);
                        if (deviceElements?.Count() <= 0 || eventElements?.Count() <= 0) continue;
                        {
                            IModbusMaster master = connectionObject as IModbusMaster;
                            ModbusSyncMaster transport = new ModbusSyncMaster(master);
                            transport.Name = name;

                            foreach (XElement deviceElement in deviceElements)
                            {
                                if (ModbusDevice.TryParse(deviceElement, out ModbusDevice device))
                                {
                                    transport.ModbusDevices.Add(device);
                                }
                            }

                            transport.InputChangeEvent += ModbusSyncMaster_InputChangeEvent;
                            transport.OutputChangeEvent += ModbusSyncMaster_OutputChangeEvent;

                            transport.Start();
                            ReflectionController.AccessObjects[name] = transport;
                        }
                        break;

                    case ConnectionType.SerialPort:
                        if (dataEvents?.Count <= 0) continue;
                        {
                            SerialPort serialPort = connectionObject as SerialPort;
                            serialPort.DataReceived += SerialPort_DataReceived;
                            ConnectionDataEvents.TryAdd(serialPort.GetCustomName(), dataEvents);
                        }
                        break;

                    case ConnectionType.TcpClient:
                    case ConnectionType.UdpClient:
                        if (dataEvents?.Count <= 0) continue;
                        {
                            IAsyncClient client = connectionObject as IAsyncClient;
                            client.Name = name;
                            client.DataReceived += Network_DataReceived;
                            ConnectionDataEvents.TryAdd(client.Name, dataEvents);
                        }
                        break;

                    case ConnectionType.TcpServer:
                    case ConnectionType.UdpServer:
                        if (dataEvents?.Count <= 0) continue;
                        {
                            IAsyncServer server = connectionObject as IAsyncServer;
                            server.Name = name;
                            server.ClientDataReceived += Network_DataReceived;
                            ConnectionDataEvents.TryAdd(server.Name, dataEvents);
                        }
                        break;
                }
            }

            //XInitialized
            CallEventType(XInitialized);
        }

        private void ModbusSyncMaster_OutputChangeEvent(ModbusSyncMaster modbusSyncMaster, ModbusDevice modbusDevice, Register register)
        {
            CallEventType(modbusSyncMaster.Name, XOutputChanged, modbusDevice.Address, register);
        }
        private void ModbusSyncMaster_InputChangeEvent(ModbusSyncMaster modbusSyncMaster, ModbusDevice modbusDevice, Register register)
        {
            CallEventType(modbusSyncMaster.Name, XInputChanged, modbusDevice.Address, register);
        }
        private void Network_DataReceived(object sender, AsyncDataEventArgs e)
        {
            IConnection connection = sender as IConnection;
            CallEventData(connection.Name, e.Bytes);
        }
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType != SerialData.Eof) return;

            SerialPort serialPort = (SerialPort)sender;
            byte[] buffer = null;
            try
            {
                buffer = new byte[serialPort.BytesToRead];
                serialPort.Read(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return;
            }

            CallEventData(serialPort.GetCustomName(), buffer);
        }

        /// <summary>
        /// 在所有连接配置中，跟据事件类型调用事件集合
        /// </summary>
        /// <param name="eventType"></param>
        public void CallEventType(string eventType)
        {
            if (ConnectionElements?.Count() <= 0 || string.IsNullOrWhiteSpace(eventType)) return;

            IEnumerable<XElement> events = from evt in ConnectionElements.Descendants(ReflectionController.XEvent)
                                           where evt.Attribute(ReflectionController.XType)?.Value == eventType
                                           select evt;

            ReflectionController.TryParseControlMessage(events.Elements());
        }
        /// <summary>
        /// 在所有连接配置中，跟据连接名称, 事件类型调用事件集合
        /// </summary>
        /// <param name="connectionName"></param>
        /// <param name="eventType"></param>
        public void CallEventType(string connectionName, string eventType)
        {
            if (ConnectionElements?.Count() <= 0 || string.IsNullOrWhiteSpace(connectionName) || string.IsNullOrWhiteSpace(eventType)) return;

            IEnumerable<XElement> events = from connection in ConnectionElements.Elements(XConnection)
                                           where connection.Attribute(ReflectionController.XName)?.Value == connectionName
                                           from evt in connection.Elements(ReflectionController.XEvent)
                                           where evt.Attribute(ReflectionController.XType)?.Value == eventType
                                           select evt;

            ReflectionController.TryParseControlMessage(events.Elements());
        }
        /// <summary>
        /// 在所有连接配置中，跟据连接名称、事件类型和其它多个参数调用事件集合
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="connectionName"></param>
        /// <param name="deviceAddress"></param>
        /// <param name="register"></param>
        protected void CallEventType(string connectionName, string eventType, byte deviceAddress, Register register)
        {
            if (ConnectionElements?.Count() <= 0 || string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(connectionName)) return;

            if (Logger.IsDebugEnabled)
                Logger.Debug($"{eventType} {connectionName} > 0x{deviceAddress:X2} > #{register.Address:X4} > {register.Type} > {register.Value}");

            foreach (XElement connection in ConnectionElements)
            {
                if (connection.Attribute(ReflectionController.XName)?.Value != connectionName) continue;

                IEnumerable<XElement> events = connection.Descendants(ReflectionController.XEvent);
                foreach (XElement evt in events)
                {
                    if (evt.Attribute(ReflectionController.XType)?.Value != eventType) continue;

                    if (!StringExtensions.ToNumber(evt.Attribute(XDeviceAddress)?.Value, out byte devAddress)) continue;
                    if (devAddress != deviceAddress) continue;

                    if (!StringExtensions.ToNumber(evt.Attribute($"{register.Type}Address")?.Value, out ushort regAddress)) continue;
                    if (regAddress != register.Address) continue;

                    if (StringExtensions.ToNumber(evt.Attribute(ReflectionController.XValue)?.Value, out long regValue) && regValue == register.Value)
                    {
                        if (Logger.IsInfoEnabled)
                            Logger.Info($"{eventType} {connectionName} > 0x{deviceAddress:X2} > #{register.Address:X4} > {register.Type} > {register.Value}");

                        ReflectionController.TryParseControlMessage(evt.Elements());
                        continue;
                    }
                    else if (StringExtensions.ToNumber(evt.Attribute(XMinValue)?.Value, out long minValue) && StringExtensions.ToNumber(evt.Attribute(XMaxValue)?.Value, out long maxValue))
                    {
                        if (maxValue > minValue && register.Value <= maxValue && register.Value >= minValue && (register.LastValue < minValue || register.LastValue > maxValue))
                        {
                            if (Logger.IsInfoEnabled)
                                Logger.Info($"{eventType} {connectionName} > 0x{deviceAddress:X2} > #{register.Address:X4} > {register.Type} > {register.Value}");

                            ReflectionController.TryParseControlMessage(evt.Elements());
                        }
                    }
                }//End for                
            }//End for
        }
        /// <summary>
        /// 在所有连接配置中，跟据连接名称和数据参数调用事件集合
        /// </summary>
        /// <param name="connectionName"></param>
        /// <param name="bytes"></param>
        public void CallEventData(string connectionName, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(connectionName) || bytes?.Length <= 0 || ConnectionDataEvents.Count <= 0) return;

            if (ConnectionDataEvents.TryGetValue(connectionName, out IReadOnlyCollection<DataEventParams> dataEvents))
            {
                foreach (DataEventParams dataEvent in dataEvents)
                {
                    if ((dataEvent.Bytes != null && bytes.SequenceEqual(dataEvent.Bytes)) ||
                        (dataEvent.Message != null && bytes.SequenceEqual(dataEvent.Message)))
                    {
                        this.ReflectionController.TryParseControlMessage(dataEvent.Actions);
                    }
                }
            }
        }
        /// <summary>
        /// 在所有连接配置中，跟据连接名称和数据参数调用事件集合
        /// </summary>
        /// <param name="connectionName"></param>
        /// <param name="message"></param>
        public void CallEventData(string connectionName, string message) => CallEventData(connectionName, Encoding.UTF8.GetBytes(message));
        /// <summary>
        /// 在所有连接配置中，跟据事件名称，调用事件集合
        /// </summary>
        /// <param name="eventName"></param>
        public void CallEventName(string eventName)
        {
            if (ConnectionElements?.Count() <= 0 || string.IsNullOrWhiteSpace(eventName)) return;

            IEnumerable<XElement> events = from evt in ConnectionElements.Descendants(ReflectionController.XEvent)
                                           where evt.Attribute(ReflectionController.XName)?.Value == eventName
                                           select evt;

            ReflectionController.TryParseControlMessage(events.Elements());
        }
        /// <summary>
        /// 在所有连接配置中，跟据连接名称和事件名称调用事件集合
        /// </summary>
        /// <param name="connectionName"></param>
        /// <param name="eventName"></param>
        public void CallEventName(string connectionName, string eventName)
        {
            if (ConnectionElements?.Count() <= 0 || string.IsNullOrWhiteSpace(connectionName) || string.IsNullOrWhiteSpace(eventName)) return;

            IEnumerable<XElement> events = from connection in ConnectionElements.Elements(XConnection)
                                           where connection.Attribute(ReflectionController.XName)?.Value == connectionName
                                           from evt in connection.Elements(ReflectionController.XEvent)
                                           where evt.Attribute(ReflectionController.XName)?.Value == eventName
                                           select evt;

            ReflectionController.TryParseControlMessage(events.Elements());
        }

        /// <summary>
        /// 获取连接的数据事件集合
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        protected IReadOnlyCollection<DataEventParams> GetConnectionDataEvents(IEnumerable<XElement> events)
        {
            if (events == null || events.Count() == 0) return null;

            List<DataEventParams> dataEvents = new List<DataEventParams>();

            foreach (XElement evt in events)
            {
                if (evt.Name.LocalName != ReflectionController.XEvent ||
                    evt.Attribute(ReflectionController.XType)?.Value != XData) continue;

                string bytes = evt.Attribute(XBytes)?.Value;
                string message = evt.Attribute(XMessage)?.Value;
                DataEventParams dataArgs = new DataEventParams();

                if (!string.IsNullOrWhiteSpace(message))
                {
                    dataArgs.Message = Encoding.UTF8.GetBytes(message);
                }
                if (!string.IsNullOrWhiteSpace(bytes))
                {
                    if (TypeExtensions.ConvertFrom(bytes.Split(','), typeof(byte[]), out object conversionValue))
                    {
                        dataArgs.Bytes = (byte[])conversionValue;
                    }
                }

                dataArgs.Actions = evt.Elements(ReflectionController.XAction);
                dataEvents.Add(dataArgs);
            }

            return dataEvents;
        }

        /// <summary>
        /// 断开所有连接对象，并从 <see cref="ReflectionController.AccessObjects"/> 集合中移除
        /// </summary>
        public void Disconnections()
        {
            if (ReflectionController == null || ConnectionElements == null || ConnectionElements.Count() <= 0) return;

            CallEventType(XDisposed);
            Thread.Sleep(100);

            ConnectionDataEvents?.Clear();
            Instance.ConnectionDataEvents.Clear();
            ReflectionController.AccessObjects.Remove(Name);

            Type DisposableType = typeof(IDisposable);
            foreach (XElement connection in ConnectionElements)
            {
                if (connection.Name.LocalName != XConnection) continue;
                string connectionName = connection.Attribute(ReflectionController.XName)?.Value;

                if (string.IsNullOrWhiteSpace(connectionName)) continue;
                if (!ReflectionController.AccessObjects.ContainsKey(connectionName)) continue;

                object instanceObj = ReflectionController.AccessObjects[connectionName];
                if (instanceObj == null)
                {
                    ReflectionController.AccessObjects.Remove(connectionName);
                    continue;
                }

                try
                {
                    if (DisposableType.IsAssignableFrom(instanceObj.GetType())) (instanceObj as IDisposable)?.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                }
                finally
                {
                    ReflectionController.AccessObjects.Remove(connectionName);
                }
            }

            ConnectionElements = null;
        }

        /// <summary>
        /// 获取连接对象
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public object GetConnection(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name), "参数不能为空");
            if (ReflectionController == null || ConnectionElements == null || ConnectionElements.Count() <= 0) return null;

            int count = (from connection in ConnectionElements
                         where connection.Attribute(ReflectionController.XName)?.Value == name
                         select connection).Count();

            if(count != 1) return null;
            return ReflectionController.AccessObjects.ContainsKey(name) ? ReflectionController.AccessObjects[name] : null;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(ConnectionManagement)}] {nameof(Name)}:{Name}";
        }

        /// <summary>
        /// 创建连接对象
        /// </summary>
        /// <param name="type"></param>
        /// <param name="connectionObject"></param>
        /// <param name="args"></param>
        /// <returns>创建的连接对象有效时返回 true, 否则返 false </returns>
        public static bool CreateConnection(ConnectionType type, out object connectionObject, params object[] args)
        {
            connectionObject = null;
            if (args.Length < 2 || !int.TryParse(args[1].ToString(), out int portOrRate))
            {
                Logger.Warn($"连接类型 {type} 的参数 {nameof(args)} 错误, 其参数值不能少于 2 个");
                return false;
            }

            switch (type)
            {
                case ConnectionType.SerialPort:
                    try
                    {
                        SerialPort serialPort = new SerialPort(args[0].ToString(), portOrRate);

                        if (args.Length >= 3)
                            serialPort.Parity = args[2].GetType() == typeof(Parity) ? (Parity)args[2] : Enum.TryParse(args[2].ToString(), true, out Parity parity) ? parity : serialPort.Parity;
                        if (args.Length >= 4)
                            serialPort.DataBits = args[3].GetType() == typeof(int) ? (int)args[3] : int.TryParse(args[3].ToString(), out int dataBits) ? dataBits : serialPort.DataBits;
                        if (args.Length >= 5)
                            serialPort.StopBits = args[4].GetType() == typeof(StopBits) ? (StopBits)args[4] : Enum.TryParse(args[4].ToString(), true, out StopBits stopBits) ? stopBits : serialPort.StopBits;

                        serialPort.Open();
                        connectionObject = serialPort;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"创建连接对象 {type}/{nameof(SerialPort)} 错误: {ex}");
                        return false;
                    }
                    break;

                case ConnectionType.ModbusRtu:
                    try
                    {
                        object[] otherArgs = new object[args.Length - 2];
                        for (int i = 2; i < args.Length; i++) otherArgs[i] = args[i];

                        NModbus4SerialPortAdapter serialPortAdapter = new NModbus4SerialPortAdapter(args[0].ToString(), portOrRate, otherArgs);
                        IModbusMaster master = ModbusSerialMaster.CreateRtu(serialPortAdapter);

                        connectionObject = master;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"创建连接对象 {type}/{nameof(IModbusMaster)} 错误: {ex}");
                        return false;
                    }
                    break;

                case ConnectionType.TcpServer:
                case ConnectionType.UdpServer:
                    try
                    {
                        IAsyncServer server = null;
                        if (type == ConnectionType.TcpServer) server = new AsyncTcpServer(args[0].ToString(), (ushort)portOrRate);
                        if (type == ConnectionType.UdpServer) server = new AsyncUdpServer(args[0].ToString(), (ushort)portOrRate);

                        connectionObject = server;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"创建连接对象 {type}/{nameof(IAsyncServer)} 错误: {ex}");
                        return false;
                    }
                    break;

                case ConnectionType.TcpClient:
                case ConnectionType.UdpClient:
                    try
                    {
                        IAsyncClient client = null;
                        if (type == ConnectionType.UdpClient) client = new AsyncUdpClient();
                        if (type == ConnectionType.TcpClient)
                        {
                            client = new AsyncTcpClient();
                            client.Disconnected += (s, e) =>
                            {
                                Task.Run(() =>
                                {
                                    Thread.Sleep(1000);
                                    client.Connect();
                                });
                            };
                        }

                        client.Connect(args[0].ToString(), (ushort)portOrRate);
                        connectionObject = client;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"创建连接对象 {type}/{nameof(IAsyncClient)} 错误: {ex}");
                        return false;
                    }
                    break;

                case ConnectionType.ModbusTcp:
                case ConnectionType.ModbusTcpRtu:
                    try
                    {
                        IModbusMaster master = null;
                        NModbus4TcpClientAdapter tcpClientAdapter = new NModbus4TcpClientAdapter(args[0].ToString(), (ushort)portOrRate);

                        if (type == ConnectionType.ModbusTcp) master = ModbusIpMaster.CreateIp(tcpClientAdapter);
                        if (type == ConnectionType.ModbusTcpRtu) master = ModbusSerialMaster.CreateRtu(tcpClientAdapter);

                        connectionObject = master;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"创建连接对象 {type}/{nameof(IModbusMaster)} 错误: {ex}");
                        return false;
                    }
                    break;

                case ConnectionType.ModbusUdp:
                case ConnectionType.ModbusUdpRtu:
                    try
                    {
                        IModbusMaster master = null;
                        UdpClient udpClient = new UdpClient(args[0].ToString(), (ushort)portOrRate);

                        if (type == ConnectionType.ModbusUdp) master = ModbusIpMaster.CreateIp(udpClient);
                        if (type == ConnectionType.ModbusUdpRtu) master = ModbusSerialMaster.CreateRtu(udpClient);

                        connectionObject = master;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"创建连接对象 {type}/{nameof(IModbusMaster)} 错误: {ex}");
                        return false;
                    }
                    break;

                default:
                    Logger.Warn($"未处理的连接类型 {type}");
                    return false;
            }

            return true;
        }
        /// <summary>
        /// 创建连接对象
        /// </summary>
        /// <param name="connectionElement"></param>
        /// <param name="connectionObject"></param>
        /// <returns>创建的连接对象有效时返回 true, 否则返 false</returns>
        public static bool CreateConnection(XElement connectionElement, out object connectionObject)
        {
            connectionObject = null;
            if (connectionElement == null || !connectionElement.HasAttributes) return false;

            string type = connectionElement.Attribute(ReflectionController.XType)?.Value;
            if (string.IsNullOrWhiteSpace(type))
            {
                Logger.Warn($"配置格式错误, 属性 {ReflectionController.XType} 不能为空, {connectionElement}");
                return false;
            }
            if (!Enum.TryParse(type, true, out ConnectionType connectionType))
            {
                Logger.Warn($"配置格式错误, 属性 {ReflectionController.XType} 值 {type} 错误, 不存在的值类型, {connectionElement}");
                return false;
            }

            string parameters = connectionElement.Attribute(XParameters)?.Value;
            if (string.IsNullOrWhiteSpace(parameters))
            {
                Logger.Warn($"配置格式错误, 属性 {XParameters} 不能为空, {connectionElement}");
                return false;
            }
            string[] args = parameters.Split(',');
            if (args.Length < 2)
            {
                Logger.Warn($"配置格式错误, 属性 {XParameters} 值 {parameters} 错误, 其参数值不能少于 2 个, {connectionElement}");
                return false;
            }

            if (!CreateConnection(connectionType, out connectionObject, args)) return false;
            object connection = connectionObject;

            switch (connectionType)
            {
                case ConnectionType.ModbusRtu:
                case ConnectionType.ModbusTcp:
                case ConnectionType.ModbusUdp:
                case ConnectionType.ModbusTcpRtu:
                case ConnectionType.ModbusUdpRtu:
                    connection = (connectionObject as IModbusMaster)?.Transport;
                    break;
            }

            //设置实例的其它属性值
            if (connectionElement.Attributes()?.Count() > 3)
            {
                XElement elementClone = XElement.Parse(connectionElement.ToString());
                elementClone.Attribute(XParameters).Remove();
                elementClone.Attribute(ReflectionController.XName).Remove();
                elementClone.Attribute(ReflectionController.XType).Remove();

                InstanceExtensions.SetInstancePropertyValues(connection, elementClone.Attributes());
            }

            return true;
        }

    }

    /// <summary>
    /// 连接对象的数据事件参数
    /// </summary>
    public class DataEventParams
    {
        /// <summary>
        /// Bytes
        /// </summary>
        public byte[] Bytes;
        /// <summary>
        /// Message
        /// </summary>
        public byte[] Message;
        /// <summary>
        /// Actions
        /// </summary>
        public IEnumerable<XElement> Actions;
        /// <summary>
        /// 连接对象的数据事件参数
        /// </summary>
        public DataEventParams() 
        {            
        }
    }
}
