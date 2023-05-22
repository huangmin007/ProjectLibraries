using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SpaceCG.Net
{
    /// <summary>
    /// 简单的异步 UDP Server 类
    /// </summary>
    public class AsyncUdpServer : IAsyncServer
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(nameof(AsyncUdpServer));

        /// <inheritdoc/>
        public int ClientCount => _Clients != null ? _Clients.Count : 0;
        /// <inheritdoc/>
        public bool IsRunning { get; private set; }
        /// <inheritdoc/>
        public int LocalPort { get; private set; }
        /// <inheritdoc/>
        public IPAddress LocalAddress { get; private set; }        
        /// <inheritdoc/>
        public ICollection<EndPoint> Clients => _Clients?.ToArray();
        /// <inheritdoc/>
        public event EventHandler<AsyncEventArgs> ClientConnected;
        /// <inheritdoc/>
        public event EventHandler<AsyncEventArgs> ClientDisconnected;
        /// <inheritdoc/>
        public event EventHandler<AsyncDataEventArgs> ClientDataReceived;
        /// <inheritdoc/>
        public event EventHandler<AsyncExceptionEventArgs> ExceptionEventHandler;

        private UdpClient _Server;
        private IPEndPoint _LocalEndPoint;
        private List<IPEndPoint> _Clients;

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
            this.LocalAddress = localIPAddress;
            this.LocalPort = listenPort;

            _LocalEndPoint = new IPEndPoint(localIPAddress, listenPort);
            _Clients = new List<IPEndPoint>();
            _Server = new UdpClient(new IPEndPoint(this.LocalAddress, this.LocalPort));
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
            if (!IsRunning)
            {
                IsRunning = true;
                _Server.EnableBroadcast = true;
                _Server.AllowNatTraversal(true);
                _Server.BeginReceive(ReceiveCallback, null);
            }

            return IsRunning;
        }
        /// <inheritdoc/>
        public bool Stop()
        {
            if (IsRunning)
            {
                IsRunning = false;
                try
                {
                    _Server.Close();
                }
                catch(Exception ex)
                {
                    IsRunning = false;
                    ExceptionEventHandler?.Invoke(this, new AsyncExceptionEventArgs(_LocalEndPoint, ex));
                }

                _Clients.Clear();
            }

            return !IsRunning;
        }
        
        /// <summary>
        /// 接收数据的方法
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCallback(IAsyncResult ar)
        {
            if (!IsRunning || _Server == null) return;

            byte[] buffer = null;
            IPEndPoint remote = null;

            try
            {
                buffer = _Server.EndReceive(ar, ref remote);
                if (_Clients.IndexOf(remote) == -1)
                {
                    _Clients.Add(remote);
                    ClientConnected?.Invoke(this, new AsyncEventArgs(remote));
                }
                ClientDataReceived?.Invoke(this, new AsyncDataEventArgs(remote, buffer));
            }
            catch (Exception ex)
            {
                if (ar.AsyncState != null)
                {
                    remote = (IPEndPoint)ar.AsyncState;
                    _Clients.Remove(remote);
                    ClientDisconnected?.Invoke(this, new AsyncEventArgs(remote));
                }
                Logger.Warn(ex);
                ExceptionEventHandler?.Invoke(this, new AsyncExceptionEventArgs(remote, ex));
            }
            finally
            {
                _Server.BeginReceive(ReceiveCallback, remote);
            }
        }

        /// <inheritdoc/>
        public bool SendBytes(byte[] datagram, EndPoint remote)
        {
            if (!IsRunning || remote?.GetType() != typeof(IPEndPoint)) return false;

            try
            {
                _Server.BeginSend(datagram, datagram.Length, (IPEndPoint)remote, SendCallback, remote);
            }
            catch (Exception ex)
            {
                Logger.Warn($"数据报异步发送到目标 {remote} 异常：{ex}");
                ExceptionEventHandler?.Invoke(this, new AsyncExceptionEventArgs(remote, ex));
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
                _Server.BeginSend(datagram, datagram.Length, ipAddress, port, SendCallback, remote);
            }
            catch (Exception ex)
            {
                Logger.Warn($"数据报异步发送到目标 {ipAddress}:{port} 异常：{ex}");
                ExceptionEventHandler?.Invoke(this, new AsyncExceptionEventArgs(remote, ex));
                return false;
            }
            return true;
        }
        private void SendCallback(IAsyncResult ar)
        {
            if (!ar.IsCompleted) return;

            int count = 0;
            try
            {
                count = _Server.EndSend(ar);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ar.AsyncState} 结束挂起的异步发送异常：{ex}");
                ExceptionEventHandler?.Invoke(this, new AsyncExceptionEventArgs((IPEndPoint)ar.AsyncState, ex));
            }
        }
        /// <inheritdoc/>
        public bool SendBytes(byte[] datagram)
        {
            foreach(var client in _Clients)
            {
                SendBytes(datagram, client);
            }
            return true;
        }
        /// <inheritdoc/>
        public bool SendMessage(String message) => SendBytes(Encoding.Default.GetBytes(message));

        /// <inheritdoc/>
        public void Dispose()
        {
            Stop();

            _Clients?.Clear();
            _Clients = null;

            _Server?.Dispose();
            _Server = null;
        }

    }
}
