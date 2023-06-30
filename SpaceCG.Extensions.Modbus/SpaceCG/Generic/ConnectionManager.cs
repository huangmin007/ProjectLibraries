using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Xml.Linq;
using Modbus.Device;
using SpaceCG.Extensions.Modbus;
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
        /// <param name="connectionElements"></param>
        public void TryParseConnectionElements(IEnumerable<XElement> connectionElements)
        {
            if (connectionElements?.Count() <= 0) return;

            RemoveAll();
            this.ConnectionElements = connectionElements;

            foreach (XElement connection in connectionElements)
            {
                if (connection.Name.LocalName != XConnection) continue;
                String name = connection.Attribute(ControlInterface.XName)?.Value;
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (this.ControlInterface.AccessObjects.ContainsKey(name))
                {
                    Logger.Warn($"对象名称 {name} 已存在, {connection}");
                    continue;
                }

                String type = connection.Attribute(ControlInterface.XType)?.Value;
                if (String.IsNullOrWhiteSpace(type)) continue;
                if (!Enum.TryParse<ConnectionType>(type, out ConnectionType connectionType))
                {
                    Logger.Warn($"连接类型 {type} 错误, 不存在的连接类型, {connection}");
                    continue;
                }

                String parameters = connection.Attribute(XParameters)?.Value;
                if (String.IsNullOrWhiteSpace(parameters)) continue;
                String[] args = parameters.Split(',');

                int length = connectionType == ConnectionType.TcpServer || connectionType == ConnectionType.UdpServer ? 1 : 2;
                if (args.Length < length || !int.TryParse(args[length - 1], out int portOrRate))
                {
                    Logger.Warn($"连接类型 {connectionType} 参数 {parameters} 错误, {connection}");
                    continue;
                }

                int readTimeout, writeTimeout;
                switch (connectionType)
                {
                    case ConnectionType.SerialPort:
                        try
                        {
                            SerialPort serialPort = new SerialPort(args[0], portOrRate);

                            if (args.Length >= 3)
                                serialPort.Parity = Enum.TryParse(args[2], true, out Parity parity) ? parity : serialPort.Parity;
                            if (args.Length >= 4)
                                serialPort.DataBits = int.TryParse(args[3], out int dataBits) ? dataBits : serialPort.DataBits;
                            if (args.Length >= 5)
                                serialPort.StopBits = Enum.TryParse(args[4], true, out StopBits stopBits) ? stopBits : serialPort.StopBits;

                            if (int.TryParse(connection.Attribute(XReadTimeout)?.Value, out readTimeout)) serialPort.ReadTimeout = readTimeout;
                            if (int.TryParse(connection.Attribute(XWriteTimeout)?.Value, out writeTimeout)) serialPort.WriteTimeout = writeTimeout;

                            serialPort.Open();
                            ControlInterface.AccessObjects.Add(name, serialPort);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(connection);
                            Logger.Warn($"创建连接类型 {connectionType} 错误: {ex}");
                            continue;
                        }
                        break;

                    case ConnectionType.ModbusRtu:
                    case ConnectionType.SerialPortRtu:
                        try
                        {
                            object[] otherArgs = new object[args.Length - 2];
                            for (int i = 2; i < args.Length; i++) otherArgs[i] = args[i];

                            NModbus4SerialPortAdapter serialPortAdapter = new NModbus4SerialPortAdapter(args[0], portOrRate, otherArgs);
                            IModbusMaster master = ModbusSerialMaster.CreateRtu(serialPortAdapter);

                            if (int.TryParse(connection.Attribute(XReadTimeout)?.Value, out readTimeout)) master.Transport.ReadTimeout = readTimeout;
                            if (int.TryParse(connection.Attribute(XWriteTimeout)?.Value, out writeTimeout)) master.Transport.WriteTimeout = writeTimeout;

                            ControlInterface.AccessObjects.Add(name, master);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(connection);
                            Logger.Warn($"创建连接类型 {connectionType} 错误: {ex}");
                            continue;
                        }
                        break;

                    case ConnectionType.TcpServer:
                    case ConnectionType.UdpServer:
                        try
                        {
                            IAsyncServer Server = null;
                            if (connectionType == ConnectionType.TcpServer) Server = new AsyncTcpServer((ushort)portOrRate);
                            if (connectionType == ConnectionType.UdpServer) Server = new AsyncUdpServer((ushort)portOrRate);
                            if (Server != null && Server.Start()) ControlInterface.AccessObjects.Add(name, Server);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(connection);
                            Logger.Warn($"创建连接类型 {connectionType} 错误: {ex}");
                        }
                        break;

                    case ConnectionType.TcpClient:
                    case ConnectionType.UdpClient:
                        try
                        {
                            IAsyncClient Client = null;
                            if (connectionType == ConnectionType.TcpClient) Client = new AsyncTcpClient();
                            if (connectionType == ConnectionType.UdpClient) Client = new AsyncUdpClient();

                            if (Client != null && Client.Connect(args[1], (ushort)portOrRate)) ControlInterface.AccessObjects.Add(name, Client);

                            if (int.TryParse(connection.Attribute(XReadTimeout)?.Value, out readTimeout)) Client.ReadTimeout = readTimeout;
                            if (int.TryParse(connection.Attribute(XWriteTimeout)?.Value, out writeTimeout)) Client.WriteTimeout = writeTimeout;
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(connection);
                            Logger.Warn($"创建连接类型 {connectionType} 错误: {ex}");
                        }
                        break;

                    case ConnectionType.ModbusTcp:
                    case ConnectionType.ModbusTcpRtu:
                        try
                        {
                            IModbusMaster master = null;
                            NModbus4TcpClientAdapter tcpClientAdapter = new NModbus4TcpClientAdapter(args[0], portOrRate);

                            if (connectionType == ConnectionType.ModbusTcp) master = ModbusIpMaster.CreateIp(tcpClientAdapter);
                            if (connectionType == ConnectionType.ModbusTcpRtu) master = ModbusSerialMaster.CreateRtu(tcpClientAdapter);

                            if (int.TryParse(connection.Attribute(XReadTimeout)?.Value, out readTimeout)) master.Transport.ReadTimeout = readTimeout;
                            if (int.TryParse(connection.Attribute(XWriteTimeout)?.Value, out writeTimeout)) master.Transport.WriteTimeout = writeTimeout;

                            ControlInterface.AccessObjects.Add(name, master);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(connection);
                            Logger.Warn($"创建连接类型 {connectionType} 错误: {ex}");
                            continue;
                        }
                        break;

                    case ConnectionType.ModbusUdp:
                    case ConnectionType.ModbusUdpRtu:
                        try
                        {
                            IModbusMaster master = null;
                            UdpClient udpClient = new UdpClient(args[0], portOrRate);

                            if (connectionType == ConnectionType.ModbusUdp) master = ModbusIpMaster.CreateIp(udpClient);
                            if (connectionType == ConnectionType.ModbusUdpRtu) master = ModbusSerialMaster.CreateRtu(udpClient);

                            if (int.TryParse(connection.Attribute(XReadTimeout)?.Value, out readTimeout)) master.Transport.ReadTimeout = readTimeout;
                            if (int.TryParse(connection.Attribute(XWriteTimeout)?.Value, out writeTimeout)) master.Transport.WriteTimeout = writeTimeout;

                            ControlInterface.AccessObjects.Add(name, master);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(connection);
                            Logger.Warn($"创建连接类型 {connectionType} 错误: {ex}");
                        }
                        break;
                }
            }
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
            if (ConnectionElements?.Count() <= 0) return;

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

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(ConnectionManager)}] {nameof(Name)}:{Name}";
        }
    }
}
