using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using SpaceCG.Generic;

namespace SpaceCG.Net
{
    /// <summary>
    /// 异步 UDP 客户端对象
    /// </summary>
    public class AsyncUdpClient : IAsyncClient
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(AsyncUdpClient));

        /// <inheritdoc/>
        public bool IsConnected => _UdpClient?.Client != null && _UdpClient.Client.Connected;
        /// <inheritdoc/>
        public IPEndPoint LocalEndPoint { get; private set; }
        /// <inheritdoc/>
        public IPEndPoint RemoteEndPoint { get; private set; }

        /// <inheritdoc/>
        public int ReadTimeout
        {
            get => (int)(_UdpClient?.Client.ReceiveTimeout); 
            set
            {
                if(_UdpClient?.Client != null) 
                    _UdpClient.Client.ReceiveTimeout = value;
            }
        }
        /// <inheritdoc/>
        public int WriteTimeout
        {
            get => (int)(_UdpClient?.Client.SendTimeout);
            set
            {
                if (_UdpClient?.Client != null)
                    _UdpClient.Client.SendTimeout = value;
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

        private bool first_io;
        private UdpClient _UdpClient;
        /// <summary>
        /// 连接参数
        /// </summary>
        private IPEndPoint _ConnectEP;

        /// <summary>
        /// AsyncUdpClient
        /// </summary>
        public AsyncUdpClient()
        {
            LocalEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 0);
            RemoteEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), 0);
        }
        /// <inheritdoc/>
        public bool Close()
        {
            first_io = false;
            if (_UdpClient != null)
            {
                _UdpClient.Close();
                _UdpClient = null;
                _ConnectEP = null;
            }

            return true;
        }
        /// <inheritdoc/>
        public bool Connect(IPEndPoint remoteEP) => Connect(remoteEP.Address, (ushort)remoteEP.Port);
        /// <inheritdoc/>
        public bool Connect(string remoteIPAddress, ushort remotePort) => Connect(IPAddress.Parse(remoteIPAddress), remotePort);
        /// <inheritdoc/>
        public bool Connect(IPAddress remoteAddress, ushort remotePort)
        {
            if (_UdpClient == null)
            {
                _ConnectEP = new IPEndPoint(remoteAddress, remotePort);

                try
                {
                    _UdpClient = new UdpClient();
                    _UdpClient.EnableBroadcast = true;
                    _UdpClient.ExclusiveAddressUse = true;
                    if(Environment.OSVersion.Platform == PlatformID.Win32NT) _UdpClient.AllowNatTraversal(true);
                    _UdpClient.Connect(remoteAddress, remotePort);
                }
                catch(Exception ex)
                {
                    _UdpClient?.Close();
                    _UdpClient = null;
                    Exception?.Invoke(this, new AsyncExceptionEventArgs(_ConnectEP, ex));
                }

                if(_UdpClient.Client.Connected)
                {
                    first_io = true;
                    Connected?.Invoke(this, new AsyncEventArgs(_ConnectEP));
                    _UdpClient.BeginReceive(ReceiveCallback, _UdpClient);

                    LocalEndPoint = _UdpClient.Client.LocalEndPoint as IPEndPoint;
                    RemoteEndPoint = _UdpClient.Client.RemoteEndPoint as IPEndPoint;
                }
                else
                {
                    Disconnected?.Invoke(this, new AsyncEventArgs(_ConnectEP));
                }
            }

            return true;
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            if (_UdpClient == null) return;

            byte[] data;
            IPEndPoint remoteEP = _ConnectEP;

            try
            {
                data = _UdpClient.EndReceive(ar, ref remoteEP);
                RemoteEndPoint = remoteEP;
            }
            catch (Exception ex)
            {
                Disconnected?.Invoke(this, new AsyncEventArgs(remoteEP));
                Exception?.Invoke(this, new AsyncExceptionEventArgs(remoteEP, ex));
                return;
            }
            
            if (!first_io)
            {
                first_io = true;
                Connected?.Invoke(this, new AsyncEventArgs(remoteEP));
            }
            DataReceived?.Invoke(this, new AsyncDataEventArgs(remoteEP, data));

            _UdpClient.BeginReceive(ReceiveCallback, _UdpClient);
        }
        
        /// <inheritdoc/>
        public bool SendBytes(byte[] data)
        {
            if (_UdpClient == null || data?.Length <= 0) return false;

            try
            {
                _UdpClient.BeginSend(data, data.Length, SendCallback, _UdpClient);
                return true;
            }
            catch(Exception ex)
            {
                Exception?.Invoke(this, new AsyncExceptionEventArgs(_ConnectEP, ex));
                return false;
            }
        }
        /// <inheritdoc/>
        public bool SendMessage(string message) => SendBytes(Encoding.Default.GetBytes(message));
        private void SendCallback(IAsyncResult ar)
        {
            if (_UdpClient == null) return;
            int count;
            try
            {
                count = _UdpClient.EndSend(ar);
            }
            catch (Exception ex)
            {
                count = 0;
                Exception?.Invoke(this, new AsyncExceptionEventArgs(_ConnectEP, ex));
            }

            if (count > 0 && !first_io)
            {
                first_io = true;
                Connected?.Invoke(this, new AsyncEventArgs(_ConnectEP));
            }

            if (count == 0)
            {
                Close();
                Disconnected?.Invoke(this, new AsyncEventArgs(_ConnectEP));
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Close();

            _ConnectEP = null;
            _UdpClient = null;

            LocalEndPoint = null;
            RemoteEndPoint = null;
        }

        public override string ToString()
        {
            return $"[{nameof(AsyncUdpClient)}] {LocalEndPoint} => {RemoteEndPoint}";
        }
    }
}
