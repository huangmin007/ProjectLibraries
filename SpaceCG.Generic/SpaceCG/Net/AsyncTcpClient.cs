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
        public string Name { get; set; }
        /// <inheritdoc/>
        public ConnectionType Type => ConnectionType.TcpClient;
        /// <summary>
        /// 与服务端的连接状态
        /// </summary>
        public bool IsConnected => tcpClient != null && tcpClient.Client != null && !((tcpClient.Client.Poll(1000, SelectMode.SelectRead) && (tcpClient.Client.Available == 0)) || !tcpClient.Client.Connected);

        /// <inheritdoc/>
        public IPEndPoint LocalEndPoint => tcpClient?.Client?.LocalEndPoint as IPEndPoint;
        /// <inheritdoc/>
        public IPEndPoint RemoteEndPoint => tcpClient?.Client?.RemoteEndPoint as IPEndPoint;
        
        /// <inheritdoc/>
        public int ReadTimeout
        {
            get => tcpClient.Client?.ReceiveTimeout ?? -1;
            set => tcpClient.Client.ReceiveTimeout = value;
        }
        /// <inheritdoc/>
        public int WriteTimeout
        {
            get => tcpClient.Client?.SendTimeout ?? -1;
            set => tcpClient.Client.SendTimeout = value;
        }

        /// <inheritdoc/>
        public event EventHandler<AsyncEventArgs> Connected;
        /// <inheritdoc/>
        public event EventHandler<AsyncEventArgs> Disconnected;
        /// <inheritdoc/>
        public event EventHandler<AsyncDataEventArgs> DataReceived;
        /// <inheritdoc/>
        public event EventHandler<AsyncExceptionEventArgs> ExceptionEvent;

        private byte[] buffer;
        private TcpClient tcpClient;        
        private IPEndPoint remoteEndPoint = null;
        private ConnectStatus connectStatus = ConnectStatus.Ready;

        enum ConnectStatus
        {
            Ready,
            Connecting,
            ConnectSuccess,
            ConnectFailed,
            ConnectException,
        }

        /// <summary>
        /// 异步 TCP 客户端对象
        /// </summary>
        public AsyncTcpClient() 
        {
            buffer = new byte[8192];
        }
        /// <summary>
        /// 异步 TCP 客户端对象
        /// </summary>
        public AsyncTcpClient(TcpClient tcpClient):this()
        {
            if (tcpClient == null) throw new ArgumentNullException(nameof(tcpClient), "参数不能为空");

            this.tcpClient = tcpClient;
            remoteEndPoint = tcpClient.Client?.RemoteEndPoint as IPEndPoint;
        }

        /// <inheritdoc/>
        public bool Close()
        {
            connectStatus = ConnectStatus.Ready;

            try
            {
                if(IsConnected)  tcpClient?.Close();
                return true;
            }
            catch(Exception ex)
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
        public bool Connect(string remoteIpAddress, ushort remotePort) => Connect(IPAddress.Parse(remoteIpAddress), remotePort);
        /// <inheritdoc/>
        public bool Connect(IPAddress remoteAddress, ushort remotePort)
        {
            if (remoteAddress == null || remotePort == 0) throw new ArgumentException("参数异常");
            if (IsConnected || connectStatus == ConnectStatus.Connecting) return true;

            tcpClient?.Dispose();
            tcpClient = new TcpClient();

            connectStatus = ConnectStatus.Connecting;
            remoteEndPoint = new IPEndPoint(remoteAddress, remotePort);

            try
            {
                tcpClient.BeginConnect(remoteAddress, remotePort, ConnectCallback, tcpClient);
                return true;
            }
            catch (Exception ex)
            {
                connectStatus = ConnectStatus.ConnectException;
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(remoteEndPoint, ex));
                return false;
            }
        }        
       
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                tcpClient?.EndConnect(ar);
            }
            catch(Exception ex)
            {
                connectStatus = ConnectStatus.ConnectException;
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(remoteEndPoint, ex));
            }

            if (this.IsConnected)
            {
                connectStatus = ConnectStatus.ConnectSuccess;
                Connected?.Invoke(this, new AsyncEventArgs(remoteEndPoint));
                tcpClient.GetStream().BeginRead(buffer, 0, buffer.Length, ReadCallback, tcpClient);
            }
            else
            {
                connectStatus = ConnectStatus.ConnectFailed;
                Disconnected?.Invoke(this, new AsyncEventArgs(remoteEndPoint));
            }
        }
        private void ReadCallback(IAsyncResult ar)
        {
            if (!this.IsConnected)
            {
                Disconnected?.Invoke(this, new AsyncEventArgs(remoteEndPoint));
                return;
            }

            int count = -1;
            try
            {
                count = tcpClient.GetStream()?.EndRead(ar) ?? -1;
            }
            catch (Exception ex)
            {
                count = 0;
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(remoteEndPoint, ex));
            }

            if (count <= -1) return;
            if (count == 0)
            {
                Disconnected?.Invoke(this, new AsyncEventArgs(remoteEndPoint));
                return;
            }            

            byte[] buffer = new byte[count];
            Buffer.BlockCopy(this.buffer, 0, buffer, 0, count);
            DataReceived?.Invoke(this, new AsyncDataEventArgs(remoteEndPoint, buffer));

            if(IsConnected && tcpClient.GetStream() != null)
                tcpClient.GetStream().BeginRead(this.buffer, 0, this.buffer.Length, ReadCallback, tcpClient);
        }

        /// <summary>
        /// 异步发送数据到远程服务端
        /// </summary>
        /// <param name="data">要发送的数据</param>
        /// <returns>函数调用成功则返回 True, 否则返回 False</returns>
        public bool SendBytes(byte[] data) => SendBytes(data, 0, data.Length);
        
        /// <summary>
        /// 异步发送数据到远程服务端
        /// </summary>
        /// <param name="data">要发送的数据</param>
        /// <param name="offset">从零开始的字节偏移量，从此处开始将字节复制到该流</param>
        /// <param name="count">最多写入的字节数</param>
        /// <returns></returns>
        public bool SendBytes(byte[] data, int offset, int count)
        {
            if (!this.IsConnected || data?.Length <= 0 || offset <= 0 || count <= 0 ||
                offset >= data.Length || count - offset > data.Length) return false;

            try
            {
                tcpClient.GetStream().WriteAsync(data, offset, count);
                return true;
            }
            catch (Exception ex)
            {
                ExceptionEvent?.Invoke(this, new AsyncExceptionEventArgs(remoteEndPoint, ex));
                return false;
            }
        }

        /// <summary>
        /// 异步发送数据到远程服务端
        /// </summary>
        /// <param name="message"></param>
        /// <returns>函数调用成功则返回 True, 否则返回 False</returns>
        public bool SendMessage(string message) => SendBytes(Encoding.UTF8.GetBytes(message));

        /// <inheritdoc/>
        public void Dispose()
        {
            this.ExceptionEvent = null;
            this.Disconnected = null;
            this.Connected = null;
            this.DataReceived = null;

            tcpClient?.Close();
            tcpClient?.Dispose();
            this.tcpClient = null;

            if (buffer != null)
            {
                Array.Clear(buffer, 0, buffer.Length);
                this.buffer = null;
            }

            this.remoteEndPoint = null;
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{nameof(AsyncTcpClient)}] {nameof(Name)}:{Name} {nameof(IsConnected)}:{IsConnected} {LocalEndPoint} => {RemoteEndPoint}";
        }

        /// <summary>
        /// <see cref="TcpClient"/> 连接状态
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <returns></returns>
        public static bool IsOnline(ref TcpClient tcpClient)
        {
            if (tcpClient == null || tcpClient.Client == null) return false;
            return !((tcpClient.Client.Poll(1000, SelectMode.SelectRead) && (tcpClient.Client.Available == 0)) || !tcpClient.Client.Connected);
        }
    }
}
