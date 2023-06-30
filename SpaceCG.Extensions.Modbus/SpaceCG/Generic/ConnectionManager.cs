using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;
using Modbus.Device;
using SpaceCG.Extensions;
using SpaceCG.Net;

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
        /// <summary> <see cref="XReadTimeout"/> Name </summary>
        public const string XReadTimeout = "ReadTimeout";
        /// <summary> <see cref="XWriteTimeout"/> Name </summary>
        public const string XWriteTimeout = "WriteTimeout";

        /// <summary>
        /// Name
        /// </summary>
        public String Name { get; private set; } = null;
        private ControlInterface ControlInterface;
        private IEnumerable<XElement> ConnectionElements;

        private ConcurrentDictionary<string, IReadOnlyCollection<DataEventParams>> ConnectionDataEvents = new ConcurrentDictionary<string, IReadOnlyCollection<DataEventParams>>();

        /// <summary>
        /// 连接管理对象
        /// </summary>
        /// <param name="controlInterface"></param>
        /// <param name="name"></param>
        public ConnectionManager(ControlInterface controlInterface, string name)
        {
            if (controlInterface == null || string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("参数错误，不能为空");

            this.Name = name;
            this.ControlInterface = controlInterface;
            this.ControlInterface.AccessObjects.Add(name, this);
        }

        /// <summary>
        /// 解析连接配置，并添加到 <see cref="ControlInterface.AccessObjects"/> 集合中
        /// </summary>
        /// <param name="connectionElements">至少具有 Name, Type, Parameters 属性的 Connection 节点集合 </param>
        public void TryParseConnectionElements(IEnumerable<XElement> connectionElements)
        {
            if (connectionElements?.Count() <= 0) return;

            RemoveAll();
            this.ConnectionElements = connectionElements;

            foreach (XElement connectionElement in connectionElements)
            {
                if (connectionElement.Name.LocalName != XConnection) continue;
                string name = connectionElement.Attribute(ControlInterface.XName)?.Value;
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (this.ControlInterface.AccessObjects.ContainsKey(name))
                {
                    Logger.Warn($"对象名称 {name} 已存在, {connectionElement}");
                    continue;
                }

                string type = connectionElement.Attribute(ControlInterface.XType)?.Value;
                if (string.IsNullOrWhiteSpace(type)) continue;
                if (!Enum.TryParse(type, out ConnectionType connectionType))
                {
                    Logger.Warn($"连接类型 {type} 错误, 不存在的连接类型, {connectionElement}");
                    continue;
                }

                string parameters = connectionElement.Attribute(XParameters)?.Value;
                if (string.IsNullOrWhiteSpace(parameters)) continue;
                String[] args = parameters.Split(',');

                if (!CreateConnection(connectionType, out object connectionObject, args)) continue;
                ControlInterface.AccessObjects.Add(name, connectionObject);

                object connection = connectionObject;
                IReadOnlyCollection<DataEventParams> dataEvents = GetConnectionDataEvents(connectionElement.Elements(ControlInterface.XEvent));

                switch (connectionType)
                {
                    case ConnectionType.ModbusRtu:
                    case ConnectionType.ModbusTcp:
                    case ConnectionType.ModbusUdp:
                    case ConnectionType.ModbusTcpRtu:
                    case ConnectionType.ModbusUdpRtu:
                        connection = (connectionObject as IModbusMaster)?.Transport;
                        break;

                    case ConnectionType.SerialPort:
                        if (dataEvents?.Count > 0)
                        {
                            SerialPort serialPort = connectionObject as SerialPort;
                            serialPort.DataReceived += SerialPort_DataReceived;
                            ConnectionDataEvents.TryAdd($"{serialPort.PortName}_{serialPort.BaudRate}", dataEvents);
                        }
                        break;

                    case ConnectionType.TcpClient:
                    case ConnectionType.UdpClient:
                        IAsyncClient client = connectionObject as IAsyncClient;
                        client.Disconnected += (s, e) => { client.Connect(); };
                        if (dataEvents?.Count > 0)
                        { 
                            client.Name = name;
                            client.DataReceived += Client_DataReceived;
                            ConnectionDataEvents.TryAdd(client.Name, dataEvents);
                        }
                        break;

                    case ConnectionType.TcpServer:
                    case ConnectionType.UdpServer:
                        if (connectionElement.Elements(ControlInterface.XEvent)?.Count() > 0)
                        {
                            IAsyncServer server = connectionObject as IAsyncServer;
                            server.Name = name;
                            server.ClientDataReceived += Server_ClientDataReceived;
                            ConnectionDataEvents.TryAdd(server.Name, dataEvents);
                        }
                        break;
                }

                //设置实例的其它属性值
                if (connectionElement.Attributes()?.Count() > 3)
                {
                    XElement elementClone = XElement.Parse(connectionElement.ToString());
                    elementClone.Attribute(XParameters).Remove();
                    elementClone.Attribute(ControlInterface.XName).Remove();
                    elementClone.Attribute(ControlInterface.XType).Remove();

                    InstanceExtensions.SetInstancePropertyValues(connection, elementClone.Attributes());
                }
            }
        }

        /// <summary>
        /// 获取连接的数据事件集合
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        private IReadOnlyCollection<DataEventParams> GetConnectionDataEvents(IEnumerable<XElement> events)
        {
            if (events == null || events.Count() == 0) return null;

            List<DataEventParams> dataEvents = new List<DataEventParams>();

            foreach (XElement evt in events)
            {
                if (evt.Name.LocalName != ControlInterface.XEvent || 
                    evt.Attribute(ControlInterface.XType)?.Value != "Data") continue;

                DataEventParams dataArgs = new DataEventParams();
                string bytes = evt.Attribute("Bytes")?.Value;
                string message = evt.Attribute("Message")?.Value;

                if (!string.IsNullOrWhiteSpace(message))
                {
                    dataArgs.Message = (Encoding.UTF8.GetBytes(message));
                }
                if (!string.IsNullOrWhiteSpace(bytes))
                {
                    if(StringExtensions.ConvertChangeTypeToArrayType(bytes.Split(','), typeof(byte[]), out Array conversionValue))
                    {
                        dataArgs.Bytes = (byte[])conversionValue;
                    }
                }

                dataArgs.Actions = evt.Elements(ControlInterface.XAction);
                dataEvents.Add(dataArgs);
            }

            return dataEvents;
        }

        private void TryCallDataEvents(string name, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(name) || bytes?.Length <= 0) return;

            if (ConnectionDataEvents.TryGetValue(name, out IReadOnlyCollection<DataEventParams> dataEvents))
            {
                foreach (var dataEvent in dataEvents)
                {
                    if ((dataEvent.Message != null && bytes.SequenceEqual(dataEvent.Message)) ||
                        (dataEvent.Bytes != null && bytes.SequenceEqual(dataEvent.Bytes)))
                    {
                        foreach (var action in dataEvent.Actions)
                            this.ControlInterface.TryParseControlMessage(action, out object returnResult);
                    }
                }
            }
        }

        private void Client_DataReceived(object sender, AsyncDataEventArgs e)
        {
            IAsyncClient client = (IAsyncClient)sender;
            TryCallDataEvents(client.Name, e.Bytes);
        }
        private void Server_ClientDataReceived(object sender, AsyncDataEventArgs e)
        {
            IAsyncServer server = (IAsyncServer)sender;
            TryCallDataEvents(server.Name, e.Bytes);
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
            catch(Exception ex)
            {
                Logger.Error(ex);
                return;
            }

            TryCallDataEvents(objName, buffer);
        }

        /// <summary>
        /// 移除并断开指定连接，从 <see cref="ControlInterface.AccessObjects"/> 集合中移除
        /// </summary>
        /// <param name="connectionName"></param>
        public void Remove(string connectionName)
        {
            if (ConnectionElements?.Count() <= 0) return;

            Type DisposableType = typeof(IDisposable);
            foreach (XElement connection in ConnectionElements)
            {
                if (connection.Name.LocalName != XConnection) continue;
                string name = connection.Attribute(ControlInterface.XName)?.Value;
                
                if (string.IsNullOrWhiteSpace(name) || name != connectionName) continue;
                if (!ControlInterface.AccessObjects.ContainsKey(name)) continue;

                object obj = ControlInterface.AccessObjects[name];
                if (obj == null)
                {
                    ControlInterface.AccessObjects.Remove(name);
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
                    ControlInterface.AccessObjects.Remove(name);
                }
            }
        }

        /// <summary>
        /// 移除并断开所有连接，从 <see cref="ControlInterface.AccessObjects"/> 集合中移除
        /// </summary>
        public void RemoveAll()
        {
            if (ConnectionElements == null || ConnectionElements.Count() <= 0) return;

            Type DisposableType = typeof(IDisposable);
            foreach (XElement connection in ConnectionElements)
            {
                if (connection.Name.LocalName != XConnection) continue;
                string name = connection.Attribute(ControlInterface.XName)?.Value;

                if (string.IsNullOrWhiteSpace(name)) continue;
                if (!ControlInterface.AccessObjects.ContainsKey(name)) continue;

                object obj = ControlInterface.AccessObjects[name];
                if (obj == null)
                {
                    ControlInterface.AccessObjects.Remove(name);
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
                    ControlInterface.AccessObjects.Remove(name);
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
                Logger.Warn($"连接类型 {type} 参数错误, {args}");
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
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"创建连接类型 {type} 错误: {ex}");
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
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"创建连接类型 {type} 错误: {ex}");
                    }
                    break;

                case ConnectionType.TcpServer:
                case ConnectionType.UdpServer:
                    try
                    {
                        IAsyncServer Server = null;
                        if (type == ConnectionType.TcpServer) Server = new AsyncTcpServer(args[0].ToString(), (ushort)portOrRate);
                        if (type == ConnectionType.UdpServer) Server = new AsyncUdpServer(args[0].ToString(), (ushort)portOrRate);

                        connectionObject = Server;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"创建连接类型 {type} 错误: {ex}");
                    }
                    break;

                case ConnectionType.TcpClient:
                case ConnectionType.UdpClient:
                    try
                    {
                        IAsyncClient Client = null;
                        if (type == ConnectionType.TcpClient) Client = new AsyncTcpClient();
                        if (type == ConnectionType.UdpClient) Client = new AsyncUdpClient();

                        Client.Connect(args[0].ToString(), (ushort)portOrRate);
                        connectionObject = Client;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"创建连接类型 {type} 错误: {ex}");
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
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"创建连接类型 {type} 错误: {ex}");
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
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"创建连接类型 {type} 错误: {ex}");
                    }
                    break;
            }

            return false;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(ConnectionManager)}] {nameof(Name)}:{Name}";
        }
    }

    internal class DataEventParams
    {
        public byte[] Bytes;

        public byte[] Message;

        public IEnumerable<XElement> Actions;

        public DataEventParams() 
        {            
        }
    }
}
