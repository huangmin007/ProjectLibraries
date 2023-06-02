using System;
using System.Net;
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
        private IPEndPoint _RemoteEP;

        /// <summary>
        /// AsyncUdpClient
        /// </summary>
        public AsyncUdpClient()
        {
        }
        /// <inheritdoc/>
        public bool Close()
        {
            first_io = false;
            if (_UdpClient != null)
            {
                _UdpClient.Close();
                _UdpClient = null;
                _RemoteEP = null;
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
                RemotePort = remotePort;
                RemoteAddress = remoteAddress;
                _RemoteEP = new IPEndPoint(remoteAddress, remotePort);

                try
                {
                    _UdpClient = new UdpClient();
                    _UdpClient.EnableBroadcast = true;
                    _UdpClient.ExclusiveAddressUse = true;
                    _UdpClient.AllowNatTraversal(true);
                    _UdpClient.Connect(remoteAddress, remotePort);
                    
                    LocalPort = ((IPEndPoint)(_UdpClient.Client.LocalEndPoint)).Port;
                    LocalAddress = ((IPEndPoint)(_UdpClient.Client.LocalEndPoint)).Address;
                }
                catch(Exception ex)
                {
                    _UdpClient?.Close();
                    _UdpClient = null;

                    Logger.Error($"{ex}");
                    Exception?.Invoke(this, new AsyncExceptionEventArgs(_RemoteEP, ex));
                    return false;
                }

                if(_UdpClient.Client.Connected)
                {
                    first_io = true;
                    Connected?.Invoke(this, new AsyncEventArgs(_RemoteEP));
                    _UdpClient.BeginReceive(ReceiveCallback, _UdpClient);
                }
                else
                {
                    Disconnected?.Invoke(this, new AsyncEventArgs(_RemoteEP));
                }
            }

            return true;
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            if (_UdpClient == null) return;

            byte[] data;
            IPEndPoint remoteEP = _RemoteEP;

            try
            {
                data = _UdpClient.EndReceive(ar, ref remoteEP);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex}");
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
                Logger.Error($"{ex}");
                Exception?.Invoke(this, new AsyncExceptionEventArgs(_RemoteEP, ex));
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
                Logger.Error($"{ex}");
                Exception?.Invoke(this, new AsyncExceptionEventArgs(_RemoteEP, ex));
            }

            if (count > 0 && !first_io)
            {
                first_io = true;
                Connected?.Invoke(this, new AsyncEventArgs(_RemoteEP));
            }

            if (count == 0)
            {
                Close();
                Disconnected?.Invoke(this, new AsyncEventArgs(_RemoteEP));
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Close();

            _RemoteEP = null;
            _UdpClient = null;

            LocalPort = -1;
            LocalAddress = null;

            RemotePort = -1;
            RemoteAddress = null;
        }
    }
}
