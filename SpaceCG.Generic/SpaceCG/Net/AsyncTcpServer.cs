using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SpaceCG.Generic;

namespace SpaceCG.Net
{
    /// <summary>
    /// 简单的异步 TCP Server 类
    /// </summary>
    public class AsyncTcpServer : IAsyncServer
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(AsyncTcpServer));

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
        public IPEndPoint LocalEndPoint => tcpListener.LocalEndpoint as IPEndPoint;

        /// <inheritdoc/>
        public int ClientCount => clients?.Count ?? 0;
        /// <inheritdoc/>
        public IReadOnlyCollection<EndPoint> Clients => clients?.Keys as IReadOnlyCollection<EndPoint>;

        /// <inheritdoc/>
        public event EventHandler<AsyncEventArgs> ClientConnected;
        /// <inheritdoc/>
        public event EventHandler<AsyncEventArgs> ClientDisconnected;
        /// <inheritdoc/>
        public event EventHandler<AsyncDataEventArgs> ClientDataReceived;
        /// <inheritdoc/>
        public event EventHandler<AsyncExceptionEventArgs> ExceptionEvent;

        /// <summary>
        /// 服务器使用的异步TcpListener
        /// </summary>
        private TcpListener tcpListener;
        private ConcurrentDictionary<EndPoint, byte[]> buffers;
        private ConcurrentDictionary<EndPoint, TcpClient> clients;

        /// <summary>
        /// 异步 TCP 服务器
        /// </summary>
        /// <param name="listenPort">监听的端口</param>
        public AsyncTcpServer(ushort listenPort) : this(IPAddress.Any, listenPort)
        {
        }
        /// <summary>
        /// 异步 TCP 服务器
        /// </summary>
        /// <param name="localEP">监听的终结点</param>
        public AsyncTcpServer(IPEndPoint localEP) : this(localEP.Address, (ushort)localEP.Port)
        {
        }
        /// <summary>
        /// 异步 TCP 服务器
        /// </summary>
        /// <param name="localIPAddress">监听的IP地址</param>
        /// <param name="listenPort">监听的端口</param>
        public AsyncTcpServer(String localIPAddress, ushort listenPort) : this(IPAddress.Parse(localIPAddress), listenPort)
        {
        }
        /// <summary>
        /// 异步 TCP 服务器
        /// </summary>
        /// <param name="localIPAddress">监听的IP地址</param>
        /// <param name="listenPort">监听的端口</param>
        public AsyncTcpServer(IPAddress localIPAddress, ushort listenPort)
        {
            try
            {
                tcpListener = new TcpListener(localIPAddress, listenPort);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex}");
                return;
            }
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try { tcpListener.AllowNatTraversal(true); }
                catch { }
            }

            buffers = new ConcurrentDictionary<EndPoint, byte[]>();
            clients = new ConcurrentDictionary<EndPoint, TcpClient>();
        }

        /// <inheritdoc/>
        public bool Start()
        {
            if (tcpListener == null) return false;
            if (IsListening) return true;

            IsListening = true;
            try
            {
                tcpListener.Start(1024);
                tcpListener.BeginAcceptTcpClient(AcceptCallback, tcpListener);
            }
            catch (Exception ex)
            {
                IsListening = false;
                Logger.Error($"{nameof(AsyncTcpServer)} 启动失败：{ex}");
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(LocalEndPoint, ex));
                return false;
            }

            return IsListening;
        }
        /// <inheritdoc/>
        public bool Stop()
        {
            if (tcpListener == null) return true;
            if (!IsListening) return true;

            IsListening = false;
            CloseAllClients();

            try
            {
                tcpListener.Stop();
            }
            catch (Exception ex)
            {
                Logger.Error($"{nameof(AsyncTcpServer)} 停止失败：{ex}");
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(LocalEndPoint, ex));
                return false;
            }

            return !IsListening;
        }

        /// <summary>
        /// 关闭所有客户端连接
        /// </summary>
        private void CloseAllClients()
        {
            if (clients == null) return;
            foreach (TcpClient client in clients.Values)
            {
                try { client.Close(); }
                catch { }
            }
            clients.Clear();
            buffers.Clear();
        }

        private void RemoveClient(ref TcpClient tcpClient, EndPoint endPoint)
        {
            if (endPoint == null) 
                endPoint = tcpClient.Client?.RemoteEndPoint;

            clients.TryRemove(endPoint, out _);
            buffers.TryRemove(endPoint, out byte[] buffer);

            tcpClient?.Dispose();
            tcpClient = null;

            Logger.Info($"客户端 {endPoint} 断开连接");
            ClientDisconnected?.Invoke(this, new AsyncEventArgs(endPoint));
        }

        /// <summary>
        /// 接受客户端连接处理
        /// </summary>
        /// <param name="ar"></param>
        private void AcceptCallback(IAsyncResult ar)
        {
            if (!IsListening || ar.AsyncState == null) return;

            TcpClient tcpClient = tcpListener.EndAcceptTcpClient(ar);
            EndPoint endPoint = tcpClient.Client.RemoteEndPoint;

            if (clients.TryAdd(endPoint, tcpClient) && buffers.TryAdd(endPoint, new byte[Math.Max(Math.Min(tcpClient.ReceiveBufferSize, 8192), 2048)]))
            {
                Logger.Info($"客户端 {endPoint} 连接成功");

                tcpClient.NoDelay = true;
                ClientConnected?.Invoke(this, new AsyncEventArgs(endPoint));
                tcpClient.GetStream().BeginRead(buffers[endPoint], 0, buffers[endPoint].Length, ReadCallback, tcpClient);
            }
            else
            {
                buffers.TryRemove(endPoint, out byte[] buffer);
                clients.TryRemove(endPoint, out TcpClient client);

                tcpClient?.Dispose();
                Logger.Info($"客户端 {endPoint} 连接失败, 已移除");
            }

            tcpListener.BeginAcceptTcpClient(AcceptCallback, tcpListener);
        }

        /// <summary>
        /// 接收客户端数据处理
        /// </summary>
        /// <param name="ar"></param>
        private void ReadCallback(IAsyncResult ar)
        {
            if (!IsListening || ar.AsyncState == null) return;

            TcpClient tcpClient = ar.AsyncState as TcpClient;
            if(tcpClient == null) return;

            EndPoint endPoint = null;
            try
            {
                endPoint = tcpClient.Client?.RemoteEndPoint;
            }
            catch (ObjectDisposedException) { return; }
            catch (Exception) { }

            if (!AsyncTcpClient.IsOnline(ref tcpClient))
            {
                RemoveClient(ref tcpClient, endPoint);
                return;
            }

            int count = 0;
            try
            {
                count = tcpClient.GetStream()?.EndRead(ar) ?? 0;
                if (count > 0) Logger.Info($"接收客户端 {endPoint} 数据 {count} Bytes");
            }
            catch (Exception ex)
            {
                count = 0;
                Logger.Error($"客户端 {endPoint} 处理异步读取异常：{ex}");
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(endPoint, ex));
            }

            if (count == 0)
            {
                RemoveClient(ref tcpClient, endPoint);
                return;
            }

            byte[] buff = new byte[count];
            Buffer.BlockCopy(buffers[endPoint], 0, buff, 0, count);
            ClientDataReceived?.Invoke(this, new AsyncDataEventArgs(endPoint, buff));

            if (AsyncTcpClient.IsOnline(ref tcpClient))
                tcpClient.GetStream().BeginRead(buffers[endPoint], 0, buffers[endPoint].Length, ReadCallback, tcpClient);
        }

        /// <inheritdoc/>
        public bool SendBytes(byte[] data, String ipAddress, int port) => SendBytes(data, new IPEndPoint(IPAddress.Parse(ipAddress), port));
        /// <inheritdoc/>
        public bool SendBytes(byte[] data, EndPoint remote)
        {
            if (!IsListening || remote == null) return false;

            if (clients.TryGetValue(remote, out TcpClient tcpClient))
            {
                if(!AsyncTcpClient.IsOnline(ref tcpClient))
                {
                    RemoveClient(ref tcpClient, remote);
                    return false;
                }

                try
                {
                    tcpClient.GetStream().BeginWrite(data, 0, data.Length, WriteCallback, tcpClient);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Warn($"客户端 {remote} 发送数据异常：{ex}");
                    ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(remote, ex));
                    return false;
                }
            }
            else
            {
                Logger.Warn($"不存在的客户端 {remote} 连接");
                return false;
            }
        }
        /// <inheritdoc/>
        public bool SendMessage(String message, EndPoint remote) => SendBytes(Encoding.UTF8.GetBytes(message), remote);
        /// <inheritdoc/>
        public bool SendMessage(String message, String ipAddress, int port) => SendBytes(Encoding.UTF8.GetBytes(message), ipAddress, port);

        /// <summary>
        /// 发送数据完成处理回调函数
        /// </summary>
        /// <param name="ar">目标客户端Socket</param>
        private void WriteCallback(IAsyncResult ar)
        {
            if (ar.AsyncState == null) return;

            TcpClient tcpClient = (TcpClient)ar.AsyncState;
            EndPoint endPoint = tcpClient.Client.RemoteEndPoint;

            try
            {
                tcpClient.GetStream().EndWrite(ar);
            }
            catch (Exception ex)
            {
                Logger.Warn($"客户端 {endPoint} 发送数据异常：{ex}");
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(endPoint, ex));
            }
        }

        /// <inheritdoc/>
        public bool SendBytes(byte[] data)
        {
            foreach (var kv in clients)
            {
                TcpClient tcpClient = kv.Value;
                EndPoint remote = tcpClient.Client.RemoteEndPoint;
                if (!AsyncTcpClient.IsOnline(ref tcpClient))
                {
                    RemoveClient(ref tcpClient, remote);
                    continue;
                }

                try
                {
                    tcpClient.GetStream().BeginWrite(data, 0, data.Length, WriteCallback, tcpClient);
                }
                catch (Exception ex)
                {
                    Logger.Warn($"客户端 {remote} 发送数据异常：{ex}");
                    ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(remote, ex));
                }
            }
            return true;
        }
        /// <inheritdoc/>
        public bool SendMessage(String message) => SendBytes(Encoding.UTF8.GetBytes(message));

        /// <inheritdoc/>
        public void Dispose()
        {
            Stop();
            CloseAllClients();

            if (tcpListener != null)
            {
                tcpListener.Server?.Dispose();
                tcpListener = null;
            }

            clients = null;
            buffers = null;
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(AsyncTcpServer)}] {nameof(IsListening)}:{IsListening}  {nameof(LocalEndPoint)}:{LocalEndPoint}";
        }

    }
}
