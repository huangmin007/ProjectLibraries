using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SpaceCG.Generic;

namespace SpaceCG.Net
{
    /// <summary>
    /// 简单的异步 UDP Server 类
    /// </summary>
    public class AsyncUdpServer : IAsyncServer
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(AsyncUdpServer));

        /// <inheritdoc/>
        public string Name { get; set; }
        /// <inheritdoc/>
        public ConnectionType Type => ConnectionType.UdpServer;
        /// <summary>
        /// 与客户端的连接状态，服务端已启动且客户端连接大于 0
        /// </summary>
        public bool IsConnected => IsListening && ClientCount > 0;

        /// <inheritdoc/>
        public int ClientCount => clients != null ? clients.Count : 0;
        /// <inheritdoc/>
        public bool IsListening => udpClient?.Client != null && udpClient.Client.IsBound;
        /// <inheritdoc/>
        public IPEndPoint LocalEndPoint => udpClient.Client.LocalEndPoint as IPEndPoint;
        /// <inheritdoc/>
        public ICollection<EndPoint> Clients => clients?.ToArray();
        /// <inheritdoc/>
        public event EventHandler<AsyncEventArgs> ClientConnected;
        /// <inheritdoc/>
        public event EventHandler<AsyncEventArgs> ClientDisconnected;
        /// <inheritdoc/>
        public event EventHandler<AsyncDataEventArgs> ClientDataReceived;
        /// <inheritdoc/>
        public event EventHandler<AsyncExceptionEventArgs> ExceptionEvent;

        private UdpClient udpClient;
        private IPEndPoint localEndPoint;
        private List<IPEndPoint> clients;

        /// <summary>
        /// 异步 UDP 服务
        /// </summary>
        /// <param name="listenPort"></param>
        public AsyncUdpServer(ushort listenPort) : this(IPAddress.Any, listenPort)
        {
        }
        /// <summary>
        /// 异步 UDP 服务
        /// </summary>
        /// <param name="localEP"></param>
        public AsyncUdpServer(IPEndPoint localEP) : this(localEP.Address, (ushort)localEP.Port)
        {
        }
        /// <summary>
        /// 异步 UDP 服务
        /// </summary>
        /// <param name="localIPAddress"></param>
        /// <param name="listenPort"></param>
        public AsyncUdpServer(IPAddress localIPAddress, ushort listenPort)
        {
            clients = new List<IPEndPoint>();
            localEndPoint = new IPEndPoint(localIPAddress, listenPort);
            
        }
        /// <summary>
        /// 异步 UDP 服务
        /// </summary>
        /// <param name="localIPAddress"></param>
        /// <param name="listenPort"></param>
        public AsyncUdpServer(String localIPAddress, ushort listenPort) : this(IPAddress.Parse(localIPAddress), listenPort)
        {
        }

        /// <inheritdoc/>
        public bool Start()
        {
            if (IsListening) return true;

            udpClient?.Dispose();

            udpClient = new UdpClient(localEndPoint);
            udpClient.EnableBroadcast = true;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try { udpClient.AllowNatTraversal(true); }
                catch { }
            }

            udpClient.BeginReceive(ReceiveCallback, null);

            return IsListening;
        }
        /// <inheritdoc/>
        public bool Stop()
        {
            if (!IsListening) return true;
            clients.Clear();

            try
            {
                udpClient?.Close();
                udpClient?.Dispose();
            }
            catch (Exception ex)
            {
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(LocalEndPoint, ex));
            }

            return !IsListening;
        }
        
        /// <summary>
        /// 接收数据的方法
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCallback(IAsyncResult ar)
        {
            if (!IsListening || udpClient == null) return;

            byte[] buffer = null;
            IPEndPoint remote = null;

            try
            {
                buffer = udpClient.EndReceive(ar, ref remote);
                if (clients.IndexOf(remote) == -1)
                {
                    clients.Add(remote);
                    ClientConnected?.Invoke(this, new AsyncEventArgs(remote));
                }
                ClientDataReceived?.Invoke(this, new AsyncDataEventArgs(remote, buffer));
            }
            catch (Exception ex)
            {
                if (ar.AsyncState != null)
                {
                    remote = (IPEndPoint)ar.AsyncState;
                    clients.Remove(remote);
                    ClientDisconnected?.Invoke(this, new AsyncEventArgs(remote));
                }
                Logger.Error(ex.ToString());
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(remote, ex));
            }

            if (remote != null) udpClient.BeginReceive(ReceiveCallback, remote);
        }

        /// <inheritdoc/>
        public bool SendBytes(byte[] datagram, EndPoint remote)
        {
            if (!IsListening || remote?.GetType() != typeof(IPEndPoint)) return false;

            try
            {
                udpClient.BeginSend(datagram, datagram.Length, (IPEndPoint)remote, SendCallback, remote);
            }
            catch (Exception ex)
            {
                Logger.Error($"数据报异步发送到目标 {remote} 异常：{ex}");
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(remote, ex));
                return false;
            }
            return true;
        }
        /// <inheritdoc/>
        public bool SendBytes(byte[] datagram, String ipAddress, int port)
        {
            IPEndPoint remote = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            try
            {
                udpClient.BeginSend(datagram, datagram.Length, ipAddress, port, SendCallback, remote);
            }
            catch (Exception ex)
            {
                Logger.Error($"数据报异步发送到目标 {ipAddress}:{port} 异常：{ex}");
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(remote, ex));
                return false;
            }
            return true;
        }
        /// <inheritdoc/>
        public bool SendMessage(String message, EndPoint remote) => SendBytes(Encoding.UTF8.GetBytes(message), remote);
        /// <inheritdoc/>
        public bool SendMessage(String message, String ipAddress, int port) => SendBytes(Encoding.UTF8.GetBytes(message), ipAddress, port);
        private void SendCallback(IAsyncResult ar)
        {
            if (!ar.IsCompleted) return;

            int count = 0;
            try
            {
                count = udpClient.EndSend(ar);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ar.AsyncState} 结束挂起的异步发送异常：{ex}");
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs((IPEndPoint)ar.AsyncState, ex));
            }
        }
        /// <summary>
        /// 异步发送数据到所有远程客户端
        /// </summary>
        /// <param name="datagram">要发送的数据</param>
        /// <returns>函数调用成功则返回 True, 否则返回 False</returns>
        public bool SendBytes(byte[] datagram)
        {
            foreach(var client in clients)
            {
                SendBytes(datagram, client);
            }
            return true;
        }
        /// <summary>
        /// 异步发送数据到所有远程客户端
        /// </summary>
        /// <param name="message"></param>
        /// <returns>函数调用成功则返回 True, 否则返回 False</returns>
        public bool SendMessage(String message) => SendBytes(Encoding.Default.GetBytes(message));

        /// <inheritdoc/>
        public void Dispose()
        {
            Stop();

            clients?.Clear();
            clients = null;

            udpClient?.Dispose();
            udpClient = null;

        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(AsyncUdpServer)}] {nameof(IsListening)}:{IsListening}  {nameof(LocalEndPoint)}:{LocalEndPoint}";
        }

    }
}
