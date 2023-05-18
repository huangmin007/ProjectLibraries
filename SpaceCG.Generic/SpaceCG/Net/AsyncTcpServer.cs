using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SpaceCG.Net
{
    /// <summary>
    /// 简单的异步 TCP Server 类
    /// </summary>
    public class AsyncTcpServer : IAsyncServer
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(nameof(AsyncTcpServer));

        /// <inheritdoc/>
        public int ClientCount => _Clients != null ? _Clients.Count : 0;
        /// <inheritdoc/>
        public bool IsRunning { get; private set; }
        /// <inheritdoc/>
        public int LocalPort { get; private set; }
        /// <inheritdoc/>
        public IPAddress LocalAddress { get; private set; }        
        /// <inheritdoc/>
        public ICollection<EndPoint> Clients => _Clients?.Keys;
        /// <inheritdoc/>
        public event EventHandler<AsyncEventArgs> ClientConnected;
        /// <inheritdoc/>
        public event EventHandler<AsyncEventArgs> ClientDisconnected;
        /// <inheritdoc/>
        public event EventHandler<AsyncDataEventArgs> ClientDataReceived;
        /// <inheritdoc/>
        public event EventHandler<AsyncExceptionEventArgs> ExceptionEventHandler;

        /// <summary>
        /// 服务器使用的异步TcpListener
        /// </summary>
        private TcpListener _Listener;
        private IPEndPoint _LocalEndPoint;
        private ConcurrentDictionary<EndPoint, byte[]> _Buffers;// = new ConcurrentDictionary<EndPoint, byte[]>();
        private ConcurrentDictionary<EndPoint, TcpClient> _Clients;// = new ConcurrentDictionary<EndPoint, TcpClient>();

        /// <summary>
        /// 异步 TCP 服务器
        /// </summary>
        /// <param name="listenPort">监听的端口</param>
        public AsyncTcpServer(int listenPort) : this(IPAddress.Any, listenPort)
        {
        }
        /// <summary>
        /// 异步 TCP 服务器
        /// </summary>
        /// <param name="localEP">监听的终结点</param>
        public AsyncTcpServer(IPEndPoint localEP) : this(localEP.Address, localEP.Port)
        {
        }
        /// <summary>
        /// 异步 TCP 服务器
        /// </summary>
        /// <param name="localIPAddress">监听的IP地址</param>
        /// <param name="listenPort">监听的端口</param>
        public AsyncTcpServer(IPAddress localIPAddress, int listenPort)
        {
            LocalPort = listenPort;
            LocalAddress = localIPAddress;
            _LocalEndPoint = new IPEndPoint(localIPAddress, listenPort);

            _Buffers = new ConcurrentDictionary<EndPoint, byte[]>();
            _Clients = new ConcurrentDictionary<EndPoint, TcpClient>();

            _Listener = new TcpListener(LocalAddress, LocalPort);
            _Listener.AllowNatTraversal(true);
        }
        /// <summary>
        /// 异步 TCP 服务器
        /// </summary>
        /// <param name="localIPAddress">监听的IP地址</param>
        /// <param name="listenPort">监听的端口</param>
        public AsyncTcpServer(String localIPAddress, int listenPort) : this(IPAddress.Parse(localIPAddress), listenPort)
        {
        }

        /// <inheritdoc/>
        public bool Start()
        {
            if (IsRunning) return true;

            IsRunning = true;
            try
            {
                _Listener.Start(1024);
                _Listener.BeginAcceptTcpClient(AcceptCallback, _Listener);
                _LocalEndPoint = (IPEndPoint)_Listener.Server.LocalEndPoint;
            }
            catch (Exception ex)
            {
                IsRunning = _Listener.Server.IsBound;
                Logger.Error($"{nameof(AsyncTcpServer)} 启动失败：{ex}");
                ExceptionEventHandler?.Invoke(this, new AsyncExceptionEventArgs(_LocalEndPoint, ex));
                return false;
            }

            return IsRunning;
        }
        /// <inheritdoc/>
        public bool Stop()
        {
            if (!IsRunning) return true;

            IsRunning = false;
            CloseAllClients();

            try
            {
                _Listener.Stop();
            }
            catch (Exception ex)
            {
                IsRunning = _Listener.Server.IsBound;
                Logger.Error($"{nameof(AsyncTcpServer)} 停止失败：{ex}");
                ExceptionEventHandler?.Invoke(this, new AsyncExceptionEventArgs(_LocalEndPoint, ex));
                return false;
            }

            return !IsRunning;
        }

        /// <summary>
        /// 关闭所有客户端连接
        /// </summary>
        private void CloseAllClients()
        {
            if (_Clients == null) return;
            foreach (TcpClient client in _Clients.Values)
            {
                client.Close();
            }
            _Clients.Clear();
            _Buffers.Clear();
        }

        /// <summary>
        /// 接受客户端连接处理
        /// </summary>
        /// <param name="ar"></param>
        private void AcceptCallback(IAsyncResult ar)
        {
            if (!IsRunning || ar.AsyncState == null) return;

            TcpClient tcpClient = _Listener.EndAcceptTcpClient(ar);
            EndPoint endPoint = tcpClient.Client.RemoteEndPoint;

            if (_Clients.TryAdd(endPoint, tcpClient) && _Buffers.TryAdd(endPoint, new byte[Math.Max(Math.Min(tcpClient.ReceiveBufferSize, 8192), 2048)]))
            {
                if (Logger.IsDebugEnabled) Logger.Debug($"客户端 {endPoint} 连接成功");

                tcpClient.NoDelay = true;
                ClientConnected?.Invoke(this, new AsyncEventArgs(endPoint));
                tcpClient.GetStream().BeginRead(_Buffers[endPoint], 0, _Buffers[endPoint].Length, ReadCallback, tcpClient);
            }
            else
            {
                _Buffers.TryRemove(endPoint, out byte[] buffer);
                _Clients.TryRemove(endPoint, out TcpClient client);

                tcpClient?.Dispose();
                Logger.Error($"客户端 {endPoint} 连接失败, 已移除");
            }

            _Listener.BeginAcceptTcpClient(AcceptCallback, _Listener);
        }

        /// <summary>
        /// 接收客户端数据处理
        /// </summary>
        /// <param name="ar"></param>
        private void ReadCallback(IAsyncResult ar)
        {
            if (!IsRunning || ar.AsyncState == null) return;

            TcpClient tcpClient = (TcpClient)ar.AsyncState;
            //NetworkStream stream = tcpClient.GetStream();
            EndPoint endPoint = tcpClient.Client.RemoteEndPoint;

            int count = 0;
            try
            {
                count = tcpClient.GetStream().EndRead(ar);
                if (Logger.IsDebugEnabled && count > 0) Logger.Debug($"接收客户端 {endPoint} 数据 {count} Bytes");
            }
            catch (Exception ex)
            {
                count = 0;
                if (Logger.IsDebugEnabled) Logger.Debug($"客户端 {endPoint} 处理异步读取异常：{ex}");
                ExceptionEventHandler?.Invoke(this, new AsyncExceptionEventArgs(endPoint, ex));
            }

            if (count == 0)
            {
                tcpClient?.Close();
                if (Logger.IsDebugEnabled) Logger.Debug($"客户端 {endPoint} 断开连接");

                _Buffers.TryRemove(endPoint, out byte[] buffer);
                _Clients.TryRemove(endPoint, out TcpClient client);
                ClientDisconnected?.Invoke(this, new AsyncEventArgs(endPoint));
                return;
            }

            byte[] buff = new byte[count];
            Buffer.BlockCopy(_Buffers[endPoint], 0, buff, 0, count);
            ClientDataReceived?.Invoke(this, new AsyncDataEventArgs(endPoint, buff));

            tcpClient.GetStream().BeginRead(_Buffers[endPoint], 0, _Buffers[endPoint].Length, ReadCallback, tcpClient);
        }

        /// <inheritdoc/>
        public bool SendBytes(byte[] data, String ipAddress, int port) => SendBytes(data, new IPEndPoint(IPAddress.Parse(ipAddress), port));
        /// <inheritdoc/>
        public bool SendBytes(byte[] data, EndPoint remote)
        {
            if (!IsRunning || remote == null) return false;

            if (_Clients.TryGetValue(remote, out TcpClient tcpClient))
            {
                if (!tcpClient.Connected)
                {
                    _Buffers.TryRemove(remote, out byte[] buffer);
                    _Clients.TryRemove(remote, out TcpClient client);
                    ClientDisconnected?.Invoke(this, new AsyncEventArgs(remote));
                    return false;
                }

                try
                {
                    tcpClient.GetStream().BeginWrite(data, 0, data.Length, WriteCallback, tcpClient);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error($"客户端 {remote} 发送数据异常：{ex}");
                    ExceptionEventHandler?.Invoke(this, new AsyncExceptionEventArgs(remote, ex));
                    return false;
                }
            }
            else
            {
                Logger.Warn($"不存在的客户端 {remote} 连接");
                return false;
            }
        }
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
            catch(Exception ex)
            {
                Logger.Error($"客户端 {endPoint} 发送数据异常：{ex}");
                ExceptionEventHandler?.Invoke(this, new AsyncExceptionEventArgs(endPoint, ex));
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Stop();
            CloseAllClients();

            if (_Listener != null)
            {
                _Listener.Server.Dispose();
                _Listener = null;
            }

            _Clients = null;
            _Buffers = null;
            //GC.SuppressFinalize(this);
        }

    }

}
