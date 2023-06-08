using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SpaceCG.Generic;

namespace SpaceCG.Net
{
    /// <summary>
    /// TCP 客户端
    /// </summary>
    public class AsyncTcpClient : IAsyncClient
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(AsyncTcpClient));

        /// <inheritdoc/>
        public bool IsConnected => _TcpClient != null && _TcpClient.Connected;

        /// <inheritdoc/>
        public IPEndPoint LocalEndPoint { get; private set; }
        /// <inheritdoc/>
        public IPEndPoint RemoteEndPoint { get; private set; }       
        
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
        /// <summary>
        /// 连接参数
        /// </summary>
        private IPEndPoint _ConnectEP;
        private String _ConnectStatus = "Ready";

        /// <summary>
        /// 异步 TCP 客户端对象
        /// </summary>
        public AsyncTcpClient() 
        {
            LocalEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 0);
            RemoteEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 0);
        }

        internal AsyncTcpClient(TcpClient tcpClient):base()
        {
            this._TcpClient = tcpClient;
            _ConnectEP = tcpClient.Client.RemoteEndPoint as IPEndPoint;
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
                Exception?.Invoke(this, new AsyncExceptionEventArgs(_ConnectEP, ex));
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
                _ConnectStatus = "Connecting";
                _TcpClient = new TcpClient();
                _ConnectEP = new IPEndPoint(remoteAddress, remotePort);
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
                Exception?.Invoke(this, new AsyncExceptionEventArgs(_ConnectEP, ex));
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
                Exception?.Invoke(this, new AsyncExceptionEventArgs(_ConnectEP, ex));
            }

            if (_TcpClient.Connected)
            {
                _ConnectStatus = "ConnectSuccess";

                LocalEndPoint = _TcpClient.Client.LocalEndPoint as IPEndPoint;
                RemoteEndPoint = _TcpClient.Client.RemoteEndPoint as IPEndPoint;

                Connected?.Invoke(this, new AsyncEventArgs(_ConnectEP));
                _TcpClient.GetStream().BeginRead(_Buffer, 0, _Buffer.Length, ReadCallback, _TcpClient);
            }
            else
            {
                _ConnectStatus = "ConnectFailed";
                Disconnected?.Invoke(this, new AsyncEventArgs(_ConnectEP));
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
                Exception?.Invoke(this, new AsyncExceptionEventArgs(_ConnectEP, ex));
            }

            if (count == 0)
            {
                Close();
                Disconnected?.Invoke(this, new AsyncEventArgs(_ConnectEP));
                return;
            }

            byte[] buffer = new byte[count];
            Buffer.BlockCopy(_Buffer, 0, buffer, 0, count);
            DataReceived?.Invoke(this, new AsyncDataEventArgs(_ConnectEP, buffer));

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
                Exception?.Invoke(this, new AsyncExceptionEventArgs(_ConnectEP, ex));
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
                Exception?.Invoke(this, new AsyncExceptionEventArgs(_ConnectEP, ex));
                return;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Close();

            _Buffer = null;
            _ConnectEP = null;
            _TcpClient = null;
            _ConnectStatus = null;

            LocalEndPoint = null;
            RemoteEndPoint = null;
        }

        public override string ToString()
        {
            return $"[{nameof(AsyncTcpClient)}] {LocalEndPoint} => {RemoteEndPoint}";
        }
    }
}
