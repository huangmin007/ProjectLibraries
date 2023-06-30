using System;
using SpaceCG.Generic;
using System.Xml.Linq;
using System.Net;

namespace SpaceCG.Net
{
    /// <summary>
    /// 连接类型
    /// </summary>
    public enum ConnectionType
    {
        /// <summary> <see cref="Unknow"/> 未知的 或 不确定的类型  </summary>
        Unknow,

        /// <summary> <see cref="SerialPort"/> 类型 </summary>
        SerialPort,

        /// <summary> <see cref="TcpClient"/> 类型 </summary>
        TcpClient,

        /// <summary> <see cref="UdpClient"/> 类型 </summary>
        UdpClient,

        /// <summary> <see cref="TcpServer"/> 类型 </summary>
        TcpServer,

        /// <summary> <see cref="UdpServer"/> 类型 </summary>
        UdpServer,

        /// <summary> <see cref="ModbusRtu"/> 类型, RTU协议 </summary>
        ModbusRtu,

        /// <summary> <see cref="SerialPortRtu"/> 类型, RTU协议 </summary>
        SerialPortRtu = ModbusRtu,

        /// <summary> <see cref="ModbusTcp"/> 类型, RTU协议 </summary>
        ModbusTcp,

        /// <summary> <see cref="ModbusUdp"/> 类型, RTU协议 </summary>
        ModbusUdp,

        /// <summary> <see cref="ModbusUdp"/> 类型, RTU协议 </summary>
        ModbusTcpRtu,

        /// <summary> <see cref="ModbusUdpRtu"/> 类型, RTU协议 </summary>
        ModbusUdpRtu,
           
    }

    /// <summary>
    /// 连接对象接口
    /// </summary>
    public interface IConnection : IDisposable
    {
        /// <summary>
        /// 连接对象的类型
        /// </summary>
        ConnectionType Type { get; }

        /// <summary>
        /// 连接对象的名称
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// 连接对象的连接状态
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 向连接的另一端发送数据，或是广播数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns>发送成功则返回 True, 否则返回 False</returns>
        bool SendBytes(byte[] data);

        /// <summary>
        /// 向连接的另一端发送文本消息，或是广播文本消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns>发送成功则返回 True, 否则返回 False</returns>
        bool SendMessage(String message);
    }

    /// <summary>
    /// 网络连接
    /// </summary>
    public static class NetworkConnection
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(NetworkConnection));

        /// <summary>
        /// 创建网络连接对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        public static bool Create<T>(ConnectionType type,  string address, ushort port, out T connection) where T: IConnection, new()
        {
            connection = default(T);
            if (type == ConnectionType.Unknow || string.IsNullOrWhiteSpace(address) || port == 0)
            {
                Logger.Warn($"无效的参数 {type} {address} {port}");
                return false;
            }
            if(!IPAddress.TryParse(address, out IPAddress ipAddress))
            {
                Logger.Warn($"无效的 IP 地址 {address}");
                return false;
            }            

            switch (type)
            {
                case ConnectionType.TcpServer:
                case ConnectionType.UdpServer:
                    try
                    {
                        IAsyncServer Server = null;
                        if (type == ConnectionType.TcpServer) Server = new AsyncTcpServer(ipAddress, port);
                        if (type == ConnectionType.UdpServer) Server = new AsyncUdpServer(ipAddress, port);
                        
                        Server.Start();
                        connection = (T)Server;
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

                        connection = (T)Client; 
                        Client.Connect(ipAddress, port);
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
    }
}
