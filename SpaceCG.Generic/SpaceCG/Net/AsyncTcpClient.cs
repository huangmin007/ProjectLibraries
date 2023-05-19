using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SpaceCG.Net
{
    /// <summary>
    /// TCP 客户端
    /// </summary>
    public class AsyncTcpClient : IAsyncClient
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(nameof(AsyncTcpClient));

        /// <inheritdoc/>
        public bool IsConnected => _TcpClient != null && _TcpClient.Connected;
        /// <inheritdoc/>
        public int RemotePort { get; private set; }
        /// <inheritdoc/>
        public IPAddress RemoteAddress { get; private set; }
        /// <inheritdoc/>
        public int LocalPort { get; private set; }
        /// <inheritdoc/>
        public IPAddress LocalAddress { get; private set; }
        /// <inheritdoc/>
        public int ReadTimeout
        {
            get => (int)(_TcpClient?.Client.ReceiveTimeout);
            set
            {
                if (_TcpClient?.Client != null)
                    _TcpClient.Client.ReceiveTimeout = value;
            }
        }
        /// <inheritdoc/>
        public int WriteTimeout
        {
            get => (int)(_TcpClient?.Client.SendTimeout);
            set
            {
                if (_TcpClient?.Client != null)
                    _TcpClient.Client.SendTimeout = value;
            }
        }

        /// <inheritdoc/>
        public event EventHandler<AsyncEventArgs> Connected;
        /// <inheritdoc/>
        public event EventHandler<AsyncEventArgs> Disconnected;
        /// <inheritdoc/>
        public event EventHandler<AsyncDataEventArgs> DataReceived;
        /// <inheritdoc/>
        public event EventHandler<AsyncExceptionEventArgs> Exception;

        private byte[] _Buffer;
        private TcpClient _TcpClient;
        private IPEndPoint _RemoteEP;
        private String _ConnectStatus = "Ready";

        /// <summary>
        /// 异步 TCP 客户端对象
        /// </summary>
        public AsyncTcpClient() 
        {
        }

        /// <inheritdoc/>
        public bool Close()
        {
            if (_TcpClient == null) return false;

            _ConnectStatus = "Ready";
            try
            {
                _TcpClient?.Dispose();
                _TcpClient = null;

                if (_Buffer != null)
                {
                    Array.Clear(_Buffer, 0, _Buffer.Length);
                    _Buffer = null;
                }
                return true;
            }
            catch(Exception ex)
            {
                if (Logger.IsDebugEnabled) Logger.Debug(ex);
                Exception?.Invoke(this, new AsyncExceptionEventArgs(_RemoteEP, ex));
                return false;
            }
        }
        /// <inheritdoc/>
        public bool Connect(IPEndPoint remoteEP) => Connect(remoteEP.Address, (ushort)remoteEP.Port);
        /// <inheritdoc/>
        public bool Connect(string remoteIpAddress, ushort remotePort) => Connect(IPAddress.Parse(remoteIpAddress), remotePort);
        /// <inheritdoc/>
        public bool Connect(IPAddress remoteAddress, ushort remotePort)
        {
            if (_TcpClient != null && _TcpClient.Connected) return true;
            if (_TcpClient != null && _ConnectStatus == "Connecting") return false;

            if (_TcpClient == null)
            {
                RemotePort = remotePort;
                RemoteAddress = remoteAddress;
                _ConnectStatus = "Connecting";

                _TcpClient = new TcpClient();
                _RemoteEP = new IPEndPoint(remoteAddress, remotePort);
                _Buffer = new byte[Math.Max(Math.Min(_TcpClient.ReceiveBufferSize, 8192), 2048)];
            }

            try
            {
                _TcpClient.BeginConnect(remoteAddress, remotePort, ConnectCallback, _TcpClient);
                return true;
            }
            catch (Exception ex)
            {
                _ConnectStatus = "ConnectException";
                if (Logger.IsDebugEnabled) Logger.Debug(ex);
                Exception?.Invoke(this, new AsyncExceptionEventArgs(_RemoteEP, ex));
                return false;
            }
        }
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                _TcpClient.EndConnect(ar);
            }
            catch(Exception ex)
            {
                _ConnectStatus = "ConnectException";
                if (Logger.IsDebugEnabled) Logger.Debug(ex);
                Exception?.Invoke(this, new AsyncExceptionEventArgs(_RemoteEP, ex));
                return;
            }

            if (_TcpClient.Connected)
            {
                _ConnectStatus = "ConnectSuccess";

                RemotePort = _RemoteEP.Port;
                RemoteAddress = _RemoteEP.Address;
                _RemoteEP = (IPEndPoint)_TcpClient.Client.RemoteEndPoint;
                LocalPort = ((IPEndPoint)_TcpClient.Client.LocalEndPoint).Port;
                LocalAddress = ((IPEndPoint)_TcpClient.Client.LocalEndPoint).Address;

                Connected?.Invoke(this, new AsyncEventArgs(_RemoteEP));
                _TcpClient.GetStream().BeginRead(_Buffer, 0, _Buffer.Length, ReadCallback, _TcpClient);
            }
            else
            {
                _ConnectStatus = "ConnectFailed";
            }
        }
        private void ReadCallback(IAsyncResult ar)
        {
            if (_TcpClient == null || !_TcpClient.Connected) return;

            int count = 0;
            try
            {
                count = _TcpClient.GetStream().EndRead(ar);
            }
            catch (Exception ex)
            {
                count = 0;
                Logger.Warn(ex);
                Exception?.Invoke(this, new AsyncExceptionEventArgs(_RemoteEP, ex));
            }

            if (count == 0)
            {
                Close();
                Disconnected?.Invoke(this, new AsyncEventArgs(_RemoteEP));
                return;
            }

            byte[] buffer = new byte[count];
            Buffer.BlockCopy(_Buffer, 0, buffer, 0, count);
            DataReceived?.Invoke(this, new AsyncDataEventArgs(_RemoteEP, buffer));

            _TcpClient.GetStream().BeginRead(_Buffer, 0, _Buffer.Length, ReadCallback, _TcpClient);
        }

        /// <inheritdoc/>
        public bool SendMessage(string message) => SendBytes(Encoding.Default.GetBytes(message));
        /// <inheritdoc/>
        public bool SendBytes(byte[] data)
        {
            if (_TcpClient == null || !_TcpClient.Connected) return false;

            try
            {
                _TcpClient.GetStream().BeginWrite(data, 0, data.Length, SendCallback, _TcpClient);
                return true;
            }
            catch(Exception ex)
            {
                Logger.Warn(ex);
                Exception?.Invoke(this, new AsyncExceptionEventArgs(_RemoteEP, ex));
                return false;
            }
        }
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                _TcpClient.GetStream().EndWrite(ar);
            }
            catch(Exception ex)
            {
                Logger.Warn(ex);
                Exception?.Invoke(this, new AsyncExceptionEventArgs(_RemoteEP, ex));
                return;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Close();

            _Buffer = null;
            _RemoteEP = null;
            _TcpClient = null;
            _ConnectStatus = null;

            LocalPort = -1;
            LocalAddress = null;

            RemotePort = -1;
            RemoteAddress = null;
        }
    }
}
