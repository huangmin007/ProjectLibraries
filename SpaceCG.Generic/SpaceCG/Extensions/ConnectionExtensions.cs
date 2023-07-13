using System;
using System.Net.Sockets;
using System.Net;
using System.Text;
using SpaceCG.Net;
using SpaceCG.Generic;

namespace SpaceCG.Extensions
{
    /// <summary>
    /// IP Connection Extensions
    /// </summary>
    public static class ConnectionExtensions
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(ConnectionExtensions));

        /// <summary>
        /// 指示 <see cref="TcpClient"/> 的连接状态
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static bool IsConnected(this TcpClient client) => client != null && client.Client.IsConnected();

        /// <summary>
        /// 指示 <see cref="UdpClient"/> 的连接状态
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public static bool IsConnected(this UdpClient client) => client != null && client.Client.IsConnected();

        /// <summary>
        /// 指示 <see cref="Socket"/> 的连接状态
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public static bool IsConnected(this Socket socket) => socket != null && !(socket.Poll(200, SelectMode.SelectRead) && socket.Available == 0 || !socket.Connected);

        /// <summary>
        /// 将字节异步写入当前 <see cref="TcpClient"/> 流
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static bool SendBytes(this TcpClient client, byte[] bytes)
        {
            if (!client.IsConnected()) return false;

            try
            {
                client.GetStream().WriteAsync(bytes, 0, bytes.Length);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex.Message);
                return false;
            }
        }
        /// <summary>
        /// 将消息异步写入当前 <see cref="TcpClient"/> 流
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool SendMessage(this TcpClient client, string message) => client.SendBytes(Encoding.UTF8.GetBytes(message));

        /// <summary>
        /// 将字节异步写入当前 <see cref="UdpClient"/> 流
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static bool SendBytes(this UdpClient client, byte[] bytes)
        {
            if (!client.IsConnected()) return false;

            try
            {
                client.SendAsync(bytes, bytes.Length);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex.Message);
                return false;
            }
        }
        /// <summary>
        /// 将消息异步写入当前 <see cref="UdpClient"/> 流
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool SendMessage(this UdpClient client, string message) => client.SendBytes(Encoding.UTF8.GetBytes(message));

        /// <summary>
        /// 创建 TCP/UDP 网络连接对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="type"></param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        public static bool CreateConnection<T>(ConnectionType type, string address, ushort port, out T connection) where T : IConnection, new()
        {
            connection = default;
            if (type == ConnectionType.Unknow || string.IsNullOrWhiteSpace(address) || port == 0)
            {
                Logger.Warn($"无效的参数 {type} {address} {port}");
                return false;
            }
            if (!IPAddress.TryParse(address, out IPAddress ipAddress))
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
