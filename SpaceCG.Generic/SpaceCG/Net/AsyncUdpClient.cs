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
        public string Name { get; set; }
        /// <inheritdoc/>
        public ConnectionType Type => ConnectionType.UdpClient;
        /// <summary>
        /// 与服务端的连接状态
        /// </summary>
        public bool IsConnected => udpClient?.Client != null && !((udpClient.Client.Poll(1000, SelectMode.SelectRead) && (udpClient.Client.Available == 0)) || !udpClient.Client.Connected);

        /// <inheritdoc/>
        public IPEndPoint LocalEndPoint => udpClient.Client?.LocalEndPoint as IPEndPoint;
        /// <inheritdoc/>
        public IPEndPoint RemoteEndPoint => udpClient.Client?.RemoteEndPoint as IPEndPoint;

        /// <inheritdoc/>
        public int ReadTimeout
        {
            get => udpClient.Client?.ReceiveTimeout ?? -1;
            set => udpClient.Client.ReceiveTimeout = value;
        }
        /// <inheritdoc/>
        public int WriteTimeout
        {
            get => udpClient.Client?.SendTimeout ?? -1;
            set => udpClient.Client.SendTimeout = value;
        }

        /// <inheritdoc/>
        public event EventHandler<AsyncEventArgs> Connected;
        /// <inheritdoc/>
        public event EventHandler<AsyncEventArgs> Disconnected;
        /// <inheritdoc/>
        public event EventHandler<AsyncDataEventArgs> DataReceived;
        /// <inheritdoc/>
        public event EventHandler<AsyncExceptionEventArgs> ExceptionEvent;

        private UdpClient udpClient;
        private IPEndPoint remoteEndPoint;

        /// <summary>
        /// AsyncUdpClient
        /// </summary>
        public AsyncUdpClient()
        {
        }
        /// <summary>
        /// 异步 Udp 客户端对象
        /// </summary>
        public AsyncUdpClient(UdpClient udpClient) : this()
        {
            if (udpClient == null) throw new ArgumentNullException(nameof(udpClient), "参数不能为空");

            this.udpClient = udpClient;
            remoteEndPoint = udpClient.Client?.RemoteEndPoint as IPEndPoint;
        }
        /// <inheritdoc/>
        public bool Close()
        {
            try
            {
                if (IsConnected) udpClient?.Close();
                return true;
            }
            catch (Exception ex)
            {
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(remoteEndPoint, ex));
                return false;
            }
        }
        /// <inheritdoc/>
        public bool Connect() => remoteEndPoint != null ? Connect(remoteEndPoint) : false;
        /// <inheritdoc/>
        public bool Connect(IPEndPoint remoteEP) => Connect(remoteEP.Address, (ushort)remoteEP.Port);
        /// <inheritdoc/>
        public bool Connect(string remoteIPAddress, ushort remotePort) => Connect(IPAddress.Parse(remoteIPAddress), remotePort);
        /// <inheritdoc/>
        public bool Connect(IPAddress remoteAddress, ushort remotePort)
        {
            if (IsConnected) return true;
            
            udpClient?.Dispose();
            remoteEndPoint = new IPEndPoint(remoteAddress, remotePort);

            try
            {
                udpClient = new UdpClient();
                udpClient.EnableBroadcast = true;
                udpClient.ExclusiveAddressUse = true;
                udpClient.Connect(remoteAddress, remotePort);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(remoteEndPoint, ex));
                return false;
            }

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                try { udpClient.AllowNatTraversal(true); }
                catch { }
            }

            if (IsConnected)
            {
                Connected?.Invoke(this, new AsyncEventArgs(remoteEndPoint));
                udpClient.BeginReceive(ReceiveCallback, udpClient);
            }
            else
            {
                Disconnected?.Invoke(this, new AsyncEventArgs(remoteEndPoint));
                return false;
            }

            return true;
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            if (!IsConnected)
            {
                Disconnected?.Invoke(this, new AsyncEventArgs(remoteEndPoint));
                return;
            }

            byte[] data;
            IPEndPoint remoteEP = remoteEndPoint;

            try
            {
                data = udpClient.EndReceive(ar, ref remoteEP);
            }
            catch (Exception ex)
            {
                Disconnected?.Invoke(this, new AsyncEventArgs(remoteEP));
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(remoteEP, ex));
                return;
            }
            
            DataReceived?.Invoke(this, new AsyncDataEventArgs(remoteEP, data));
            if(IsConnected) udpClient.BeginReceive(ReceiveCallback, udpClient);
        }
        
        /// <inheritdoc/>
        public bool SendBytes(byte[] data)
        {
            if (!IsConnected || data?.Length <= 0) return false;

            try
            {
                udpClient.SendAsync(data, data.Length);
                return true;
            }
            catch(Exception ex)
            {
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(remoteEndPoint, ex));
                return false;
            }
        }
        /// <inheritdoc/>
        public bool SendMessage(string message) => SendBytes(Encoding.Default.GetBytes(message));
        
        /// <inheritdoc/>
        public void Dispose()
        {
            Close();

            remoteEndPoint = null;
            udpClient = null;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(AsyncUdpClient)}] {nameof(Name)}:{Name} {nameof(IsConnected)}:{IsConnected} {LocalEndPoint} => {RemoteEndPoint}";
        }
    }
}
