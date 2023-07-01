using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using SpaceCG.Generic;
using SpaceCG.Net;

namespace SpaceCG.Extensions
{
    /// <summary>
    /// NetworkExtensions
    /// </summary>
    public static partial class NetworkExtensions
    {
        /// <summary>
        /// log4net.Logger 对象
        /// </summary>
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(InstanceExtensions));


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
            connection = default(T);
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

        /// <summary>
        /// 获取本机的 IPv4 地址
        /// </summary>
        /// <returns></returns>
        public static IPAddress[] GetIPAddress()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            IEnumerable<IPAddress> ips = from ipAddress in host.AddressList
                                         where ipAddress.AddressFamily == AddressFamily.InterNetwork
                                         select ipAddress;

            return ips.ToArray();
        }

        /// <summary>
        /// 获取本机的 IPv4 子网掩码地址
        /// </summary>
        /// <returns></returns>
        public static IPAddress[] GetMaskAddress()
        {
            //IPv4 获取子网掩码地址
            IEnumerable<IPAddress> masks = from adapter in NetworkInterface.GetAllNetworkInterfaces()
                                           from unicast in adapter.GetIPProperties().UnicastAddresses
                                           where unicast.Address.AddressFamily == AddressFamily.InterNetwork
                                           select unicast.IPv4Mask;

            return masks.ToArray();
        }

        /// <summary>
        /// 获取 IPv4 广播地址
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IPAddress> GetBroadcastAddress()
        {
            List<IPAddress> ips = GetIPAddress().ToList();
            IEnumerable<IPAddress> masks = GetMaskAddress();

            int k = 0;
            IPAddress[] broadcasts = new IPAddress[Math.Min(ips.Count(), masks.Count())];

            foreach (IPAddress mask in masks)
            {
                int index = ips.IndexOf(mask);
                if (index == -1) continue;

                byte[] maskAdd = mask.GetAddressBytes();
                byte[] ipAdd = ips[index].GetAddressBytes();

                for (int i = 0; i < ipAdd.Length; i++)
                {
                    ipAdd[i] = (byte)(~maskAdd[i] | ipAdd[i]);
                }

                broadcasts[k++] = new IPAddress(ipAdd);
            }

            return broadcasts;
        }

        /// <summary>
        /// 获取指定的 IPv4 广播地址
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static IPAddress GetBroadcastAddress(IPAddress ipAddress)
        {
            List<IPAddress> masks = GetMaskAddress().ToList();

            int index = masks.IndexOf(ipAddress);
            if (index == -1) return IPAddress.Parse("0.0.0.0");

            byte[] ipAdd = ipAddress.GetAddressBytes();
            byte[] maskAdd = masks[index].GetAddressBytes();

            for (int i = 0; i < ipAdd.Length; i++)
            {
                ipAdd[i] = (byte)(~maskAdd[i] | ipAdd[i]);
            }

            return new IPAddress(ipAdd);
        }

        /// <summary>
        /// 获取指定的 IPv4 广播地址
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static IPAddress GetBroadcastAddress(String ipAddress)
        {
            if (IPAddress.TryParse(ipAddress, out IPAddress address))
                return GetBroadcastAddress(address);

            return IPAddress.Parse("0.0.0.0");
        }
    }
}
