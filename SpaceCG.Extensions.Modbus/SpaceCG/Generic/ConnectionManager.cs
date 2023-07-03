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

namespace SpaceCG.Generic
{
    /// <summary>
    /// 连接管理
    /// </summary>
    public class ConnectionManager
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(ConnectionManager));

        /// <summary> <see cref="XConnections"/> Name </summary>
        public const string XConnections = "Connections";
        /// <summary> <see cref="XConnection"/> Name </summary>
        public const string XConnection = "Connection";
        /// <summary> <see cref="XParameters"/> Name </summary>
        public const string XParameters = "Parameters";

        /// <summary> <see cref="XData"/> Name </summary>
        public const string XData = "Data";
        /// <summary> <see cref="XBytes"/> Name </summary>
        public const string XBytes = "Bytes";
        /// <summary> <see cref="XMessage"/> Name </summary>
        public const string XMessage = "Message";

        /// <summary>
        /// Current Object Name
        /// </summary>
        public String Name { get; private set; } = null;
        private ReflectionInterface ReflectionInterface;
        private IEnumerable<XElement> ConnectionElements;
        private ConcurrentDictionary<string, IReadOnlyCollection<DataEventParams>> ConnectionDataEvents;

        /// <summary>
        /// 连接管理对象
        /// </summary>
        /// <param name="reflectionInterface"></param>
        /// <param name="name"></param>
        public ConnectionManager(ReflectionInterface reflectionInterface, string name)
        {
            if (reflectionInterface == null)
                throw new ArgumentNullException("参数错误，不能为空");

            this.Name = name;
            this.ReflectionInterface = reflectionInterface;
            this.ConnectionDataEvents = new ConcurrentDictionary<string, IReadOnlyCollection<DataEventParams>>();

            if (!string.IsNullOrWhiteSpace(Name)) this.ReflectionInterface.AccessObjects.Add(name, this);
        }

        /// <summary>
        /// 解析连接配置 Connection 节点的集合，并添加到 <see cref="ReflectionInterface.AccessObjects"/> 集合中
        /// <code>//配置示例：
        /// &lt;Connection Name = "tcpServer" Type="TcpServer" Parameters="0.0.0.0,3000" /&gt;
        /// &lt;Connection Name = "tcp" Type="TcpClient" Parameters="127.0.0.1,9600" ReadTimeout="30" &gt;
        ///    &lt;Event Type = "Data" Message="Hello" &gt;
        ///        &lt;Action Target = "Bus.#01" Method="TurnSingleCoil" Params="0x02, 1" /&gt;
        ///    &lt;/Event&gt;
        ///    &lt;Event Type = "Data" Bytes="0x01,0x02,0x03" &gt;
        ///        &lt;Action Target = "Bus.#01" Method="TurnSingleCoil" Params="0x02, 1" /&gt;
        ///    &lt;/Event&gt;
        /// &lt;/Connection&gt;
        /// </code>
        /// </summary>
        /// <param name="connectionElements">至少具有 Name, Type, Parameters 属性的 Connection 节点集合 </param>
        public void TryParseElements(IEnumerable<XElement> connectionElements)
        {
            if (connectionElements?.Count() <= 0) return;

            RemoveAll();
            this.ConnectionElements = connectionElements;

            foreach (XElement connectionElement in connectionElements)
            {
                if (connectionElement.Name.LocalName != XConnection) continue;

                string name = connectionElement.Attribute(ReflectionInterface.XName)?.Value;
                if (string.IsNullOrWhiteSpace(name))
                {
                    Logger.Warn($"配置格式错误, 属性 {ReflectionInterface.XName} 不能为空, {connectionElement}");
                    continue;
                }

                if (!CreateConnection(connectionElement, out object connectionObject)) continue;
                if (this.ReflectionInterface.AccessObjects.ContainsKey(name))
                {
                    Logger.Warn($"配置格式错误, 访问对象名称 {name} 已存在, {connectionElement}");
                    if (typeof(IDisposable).IsAssignableFrom(connectionObject.GetType())) (connectionObject as IDisposable)?.Dispose();
                    continue;
                }

                ReflectionInterface.AccessObjects.Add(name, connectionObject);
                IReadOnlyCollection<DataEventParams> dataEvents = GetConnectionDataEvents(connectionElement.Elements(ReflectionInterface.XEvent));

                if (dataEvents?.Count <= 0) continue;
                ConnectionType connectionType = (ConnectionType)Enum.Parse(typeof(ConnectionType), connectionElement.Attribute(ReflectionInterface.XType).Value, true);

                switch (connectionType)
                {
                    case ConnectionType.ModbusRtu:
                    case ConnectionType.ModbusTcp:
                    case ConnectionType.ModbusUdp:
                    case ConnectionType.ModbusTcpRtu:
                    case ConnectionType.ModbusUdpRtu:
                        break;

                    case ConnectionType.SerialPort:
                        SerialPort serialPort = connectionObject as SerialPort;
                        serialPort.DataReceived += SerialPort_DataReceived;
                        ConnectionDataEvents.TryAdd($"{serialPort.PortName}_{serialPort.BaudRate}", dataEvents);
                        break;

                    case ConnectionType.TcpClient:
                    case ConnectionType.UdpClient:
                        IAsyncClient client = connectionObject as IAsyncClient;
                        client.Name = name;
                        client.DataReceived += Network_DataReceived;
                        ConnectionDataEvents.TryAdd(client.Name, dataEvents);
                        break;

                    case ConnectionType.TcpServer:
                    case ConnectionType.UdpServer:
                        IAsyncServer server = connectionObject as IAsyncServer;
                        server.Name = name;
                        server.ClientDataReceived += Network_DataReceived;
                        ConnectionDataEvents.TryAdd(server.Name, dataEvents);

                        break;
                }
            }
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
                if (evt.Name.LocalName != ReflectionInterface.XEvent ||
                    evt.Attribute(ReflectionInterface.XType)?.Value != XData) continue;

                string bytes = evt.Attribute(XBytes)?.Value;
                string message = evt.Attribute(XMessage)?.Value;
                DataEventParams dataArgs = new DataEventParams();

                if (!string.IsNullOrWhiteSpace(message))
                {
                    dataArgs.Message = Encoding.UTF8.GetBytes(message);
                }
                if (!string.IsNullOrWhiteSpace(bytes))
                {
                    if (StringExtensions.ConvertChangeTypeToArrayType(bytes.Split(','), typeof(byte[]), out Array conversionValue))
                    {
                        dataArgs.Bytes = (byte[])conversionValue;
                    }
                }

                dataArgs.Actions = evt.Elements(ReflectionInterface.XAction);
                dataEvents.Add(dataArgs);
            }

            return dataEvents;
        }
        /// <summary>
        /// 试图调用匹配数据的对象
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bytes"></param>
        protected void TryCallDataEvents(string name, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(name) || bytes?.Length <= 0 || ConnectionDataEvents.Count <= 0) return;

            if (ConnectionDataEvents.TryGetValue(name, out IReadOnlyCollection<DataEventParams> dataEvents))
            {
                foreach (DataEventParams dataEvent in dataEvents)
                {
                    if ((dataEvent.Message != null && bytes.SequenceEqual(dataEvent.Message)) ||
                        (dataEvent.Bytes != null && bytes.SequenceEqual(dataEvent.Bytes)))
                    {
                        this.ReflectionInterface.TryParseControlMessage(dataEvent.Actions);
                    }
                }
            }
        }

        private void Network_DataReceived(object sender, AsyncDataEventArgs e)
        {
            IConnection connection = sender as IConnection;
            TryCallDataEvents(connection.Name, e.Bytes);
        }
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType != SerialData.Eof) return;

            SerialPort serialPort = (SerialPort)sender;
            string objName = $"{serialPort.PortName}_{serialPort.BaudRate}";

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

            TryCallDataEvents(objName, buffer);
        }

        /// <summary>
        /// 移除并断开所有连接对象，并从 <see cref="ReflectionInterface.AccessObjects"/> 集合中移除
        /// </summary>
        public void RemoveAll()
        {
            if (ConnectionElements == null || ConnectionElements.Count() <= 0) return;

            ConnectionDataEvents.Clear();
            Type DisposableType = typeof(IDisposable);
            foreach (XElement connection in ConnectionElements)
            {
                if (connection.Name.LocalName != XConnection) continue;
                string name = connection.Attribute(ReflectionInterface.XName)?.Value;

                if (string.IsNullOrWhiteSpace(name)) continue;
                if (!ReflectionInterface.AccessObjects.ContainsKey(name)) continue;

                object obj = ReflectionInterface.AccessObjects[name];
                if (obj == null)
                {
                    ReflectionInterface.AccessObjects.Remove(name);
                    continue;
                }

                try
                {
                    if (DisposableType.IsAssignableFrom(obj.GetType())) (obj as IDisposable)?.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex);
                }
                finally
                {
                    ReflectionInterface.AccessObjects.Remove(name);
                }
            }

            ConnectionElements = null;
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

            string type = connectionElement.Attribute(ReflectionInterface.XType)?.Value;
            if (string.IsNullOrWhiteSpace(type))
            {
                Logger.Warn($"配置格式错误, 属性 {ReflectionInterface.XType} 不能为空, {connectionElement}");
                return false;
            }
            if (!Enum.TryParse(type, true, out ConnectionType connectionType))
            {
                Logger.Warn($"配置格式错误, 属性 {ReflectionInterface.XType} 值 {type} 错误, 不存在的值类型, {connectionElement}");
                return false;
            }

            string parameters = connectionElement.Attribute(XParameters)?.Value;
            if (string.IsNullOrWhiteSpace(parameters))
            {
                Logger.Warn($"配置格式错误, 属性 {XParameters} 不能为空, {connectionElement}");
                return false;
            }
            String[] args = parameters.Split(',');
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
                elementClone.Attribute(ReflectionInterface.XName).Remove();
                elementClone.Attribute(ReflectionInterface.XType).Remove();

                InstanceExtensions.SetInstancePropertyValues(connection, elementClone.Attributes());
            }

            return true;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(ConnectionManager)}] {nameof(Name)}:{Name}";
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
