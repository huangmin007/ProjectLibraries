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
        public ConnectionType Type => ConnectionType.TcpServer;
        /// <summary>
        /// 与客户端的连接状态，服务端已启动且客户端连接大于 0
        /// </summary>
        public bool IsConnected => IsListening && ClientCount > 0;

        /// <inheritdoc/>
        public bool IsListening { get; private set; }
        /// <inheritdoc/>
        public IPEndPoint LocalEndPoint => udpClient.Client?.LocalEndPoint as IPEndPoint;

        /// <inheritdoc/>
        public int ClientCount => clients?.Count ?? 0;
        /// <inheritdoc/>
        public IReadOnlyCollection<EndPoint> Clients => clients?.ToArray();

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
        public AsyncUdpServer(string localIPAddress, ushort listenPort) : this(IPAddress.Parse(localIPAddress), listenPort)
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

            IsListening = true;
            udpClient.BeginReceive(ReceiveCallback, null);

            return IsListening;
        }
        /// <inheritdoc/>
        public bool Stop()
        {
            if (!IsListening) return true;

            clients.Clear(); 
            IsListening = false;

            try
            {
                udpClient?.Close();
            }
            catch (Exception ex)
            {
                IsListening = false;
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
        public bool SendBytes(byte[] datagram, EndPoint remoteEndPoint)
        {
            if (!IsListening || remoteEndPoint?.GetType() != typeof(IPEndPoint)) return false;

            try
            {
                udpClient.BeginSend(datagram, datagram.Length, (IPEndPoint)remoteEndPoint, SendCallback, remoteEndPoint);
            }
            catch (Exception ex)
            {
                Logger.Error($"数据报异步发送到目标 {remoteEndPoint} 异常：{ex}");
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(remoteEndPoint, ex));
                return false;
            }
            return true;
        }
        /// <inheritdoc/>
        public bool SendBytes(byte[] datagram, string ipAddress, int port)
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
        public bool SendMessage(string message, EndPoint remote) => SendBytes(Encoding.UTF8.GetBytes(message), remote);
        /// <inheritdoc/>
        public bool SendMessage(string message, string ipAddress, int port) => SendBytes(Encoding.UTF8.GetBytes(message), ipAddress, port);
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
        /// <inheritdoc/>
        public bool SendBytes(byte[] datagram)
        {
            foreach (var client in clients)
            {
                SendBytes(datagram, client);
            }
            return true;
        }
        /// <inheritdoc/>
        public bool SendMessage(string message) => SendBytes(Encoding.UTF8.GetBytes(message));

        /// <inheritdoc/>
        public bool SendBytes(byte[] datagram, int offset, int count)
        {
            if (!IsListening || datagram?.Length <= 0 || offset <= 0 || count <= 0 
                || offset >= datagram.Length || count - offset > datagram.Length) return false;
            
            foreach (var client in clients)
            {
                try
                {
                    udpClient.Client.BeginSendTo(datagram, offset, count, SocketFlags.None, client, SendCallback, client);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"数据报异步发送到目标 {client} 异常：{ex}");
                    ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(client, ex));
                }
            }
            return true;
        }

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
