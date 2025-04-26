using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SpaceCG.Net
{
    /// <summary>
    /// TCP 客户端扩展方法
    /// </summary>
    public static class TcpClientExtensions
    {
        /// <summary>
        /// <see cref="TcpClient"/> 连接状态
        /// </summary>
        /// <param name="tcpClient"></param>
        /// <returns></returns>
        public static bool IsOnline(this TcpClient tcpClient)
        {
            if (tcpClient == null || tcpClient.Client == null) return false;
            return !((tcpClient.Client.Poll(1000, SelectMode.SelectRead) && (tcpClient.Client.Available == 0)) || !tcpClient.Client.Connected);
        }
    }


    /// <summary>
    /// 优化后的RPC客户端，支持自动重连
    /// </summary>
    public class RPCClient2 : IDisposable
    {
        private int _remotePort;
        private string _remoteHost;

        private bool _searching = false;
        private bool _isDisposed = false;
        private TcpClient _tcpClient;
        private CancellationTokenSource _cts;

        /// <summary> 读取超时 </summary>
        public int ReadTimeout { get; set; } = 3000;
        /// <summary> 写入超时 </summary>
        public int WriteTimeout { get; set; } = 3000;

        /// <summary> 是否已连接 </summary>
        public bool IsConnected => _tcpClient?.IsOnline() ?? false;
        //public bool IsConnected => _tcpClient?.Connected ?? false;

        /// <summary> 连接状态变更事件 </summary>
        public event EventHandler<bool> ConnectionStateChanged;

        private readonly byte[] ReadBuffer = new byte[RPCServer2.BUFFER_SIZE];

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="remoteHost"></param>
        /// <param name="remotePort"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public RPCClient2(string remoteHost, int remotePort)
        {
            if (string.IsNullOrEmpty(remoteHost))
                throw new ArgumentNullException(nameof(remoteHost), "远程主机不能为空");
            if (remotePort < 1 || remotePort > 65535)
                throw new ArgumentOutOfRangeException(nameof(remotePort), "端口必须在 1~65535 之间");

            _remoteHost = remoteHost;
            _remotePort = remotePort;            
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="remotePort"></param>
        /// <param name="serverName"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public RPCClient2(int remotePort, string serverName)
        {
            if (remotePort < 1 || remotePort > 65535)
                throw new ArgumentOutOfRangeException(nameof(remotePort), "端口必须在 1~65535 之间");
            if (string.IsNullOrWhiteSpace(serverName))
                throw new ArgumentException("远程服务端名称不能为空", nameof(serverName));

            this._remotePort = remotePort;
            SearchDiscoveryService(remotePort, serverName);
        }

        /// <summary>
        /// 搜索并连接服务端
        /// </summary>
        /// <param name="remotePort"></param>
        /// <param name="serverName"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void SearchDiscoveryService(int remotePort, string serverName)
        {
            if (remotePort < 1 || remotePort > 65535)
                throw new ArgumentOutOfRangeException(nameof(remotePort), "端口必须在 1~65535 之间");
            if (string.IsNullOrWhiteSpace(serverName))
                throw new ArgumentException("远程服务端名称不能为空", nameof(serverName));

            if (_searching) return;
            _searching = true;

            Task.Run(async () =>
            {
                using (UdpClient udpClient = new UdpClient())
                {
                    udpClient.EnableBroadcast = true;
                    byte[] message = Encoding.UTF8.GetBytes($"DISCOVER:{serverName}");
                    IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, remotePort);                    

                    while (_searching)
                    {
                        if (!_searching) return;

                        udpClient.Send(message, message.Length, broadcastEndPoint);
                        await Task.Delay(500, _cts.Token);

                        if (udpClient.Available > 0)
                        {
                            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                            var bufffer = udpClient.Receive(ref remoteEP);
                            string response = Encoding.UTF8.GetString(bufffer);
                            Trace.TraceInformation($"Search Discovery XML-RPC Server {serverName} Response: {response} {remoteEP}");

                            if (!response.StartsWith("SERVER_INFO:")) continue;

                            string[] parts = response.Split(',');
                            if (parts.Length != 2 || int.TryParse(parts[1], out int serverPort) == false) continue;

                            if (!IsConnected)
                            {
                                this._remotePort = serverPort;
                                this._remoteHost = remoteEP.Address.ToString();

                                await StartAsync();
                            }
                            _searching = false;
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 启动客户端并尝试连接
        /// </summary>
        public async Task StartAsync()
        {
            if (_isDisposed) 
                throw new ObjectDisposedException(nameof(RPCClient));

            if (string.IsNullOrWhiteSpace(_remoteHost) || _remotePort < 1 || _remotePort > 65535)
                throw new InvalidOperationException("连接远程主机和端口没能正确设置");

            if (IsConnected || _cts != null) return;

            _cts = new CancellationTokenSource();
            await ConnectWithRetryAsync();
        }
        /// <summary>
        /// 停止客户端并断开连接
        /// </summary>
        /// <returns></returns>
        public async Task StopAsync()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(RPCClient));

            if (_cts == null) return;

            _cts.Cancel();
            await Task.Delay(10);

            CloseConnection();

            _cts.Dispose();
            _cts = null;
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        private void CloseConnection()
        {
            try
            {
                _tcpClient?.Dispose();
            }
            catch
            {
                // 忽略
            }
            finally
            {
                _tcpClient = null;
            }
        }
        /// <summary>
        /// 处理连接断开
        /// </summary>
        private async void HandleDisconnection()
        {
            ConnectionStateChanged?.Invoke(this, false);
            CloseConnection();

            if (!_cts.IsCancellationRequested)
            {
                await ConnectWithRetryAsync();
            }
        }

        /// <summary>
        /// 带重试机制的连接方法
        /// </summary>
        private async Task ConnectWithRetryAsync()
        {
            int reconnectCount = 0;            // 当前重连次数
            int reconnectInterval = 5;         // 重连间隔(秒)
            const int MaxReconnectCount = 120;   // 最大重连次数

            while (!_cts.IsCancellationRequested)
            {
                try
                {                    
                    await InternalConnectAsync();

                    // 重置重连计数器、间隔时间
                    reconnectCount = 0;
                    reconnectInterval = 5000;
                    break;
                }
                catch (Exception ex)
                {
                    reconnectCount++;

                    Trace.TraceWarning($"连接失败: ({ex.GetType().Name}){ex.Message}");
                    Trace.TraceInformation($"等待 {reconnectInterval} 秒后，尝试第 {reconnectCount} 次重新连接 ...");

                    if (reconnectCount >= MaxReconnectCount)
                    {
                        reconnectInterval = 10; // 重连间隔增加
                    }

                    await Task.Delay(reconnectInterval * 1000, _cts.Token);
                }
            }

            Debug.WriteLine($"已连接到服务端 {_remoteHost}:{_remotePort}，重连机制已退出。");
        }
        /// <summary>
        /// 内部连接方法
        /// </summary>
        private async Task InternalConnectAsync()
        {
            if (IsConnected) return;

            CloseConnection(); // 确保先关闭现有连接

            _tcpClient = new TcpClient();
            _tcpClient.SendTimeout = WriteTimeout;
            _tcpClient.ReceiveTimeout = ReadTimeout;

            Debug.WriteLine($"正在连接远程服务端 {_remoteHost}:{_remotePort} ...");

            await _tcpClient.ConnectAsync(_remoteHost, _remotePort);

            if (IsConnected)
            {
                Trace.TraceInformation($"已成功连接到远程服务端: {_tcpClient.Client.LocalEndPoint} -> {_tcpClient.Client.RemoteEndPoint}");
                ConnectionStateChanged?.Invoke(this, true);

                // 启动接收任务
                //_ = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);
            }
        }
        /// <summary>
        /// 接收消息循环
        /// </summary>
        private async Task ReceiveLoopAsync(CancellationToken cancelToken)
        {
            try
            {
                byte[] buffer = new byte[RPCServer2.BUFFER_SIZE];

                while (!cancelToken.IsCancellationRequested && IsConnected)
                {
                    var networkStream = _tcpClient.GetStream();
                    var readBytesCount = await networkStream.ReadAsync(buffer, 0, buffer.Length, cancelToken);

                    if (readBytesCount == 0)
                    {
                        Trace.TraceWarning("远程服务端关闭了连接");
                        HandleDisconnection();
                        break;
                    }

                    var receivedMessage = Encoding.UTF8.GetString(buffer, 0, readBytesCount);
                    Debug.WriteLine($"收到服务端的消息: {receivedMessage}");

                    // 这里可以添加消息处理逻辑

                    // 这里响应消息
                    var resposeMessage = "hello";
                    var resposeBytes = Encoding.UTF8.GetBytes(resposeMessage);
                    await networkStream.WriteAsync(resposeBytes, 0, resposeBytes.Length, cancelToken);
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
            {
                // 正常退出
            }
            catch (Exception ex)
            {
                Trace.TraceError($"接收消息时出错: ({ex.GetType().Name}){ex.Message}");
                HandleDisconnection();
            }
        }

        /// <summary>
        /// 异步调用远程实例对象的方法
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancelToken"></param>
        /// <returns>如果本地消息没有发送成功，则返回 null </returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ArgumentException"></exception>
        protected async Task<InvokeResult> CallRemoteMethodAsync(string message, CancellationToken cancelToken)
        {
            if (_isDisposed) 
                throw new ObjectDisposedException(nameof(RPCClient));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("参数不能为空", nameof(message));

            if (!IsConnected) return null;

#if false
            // 在这里验证一次消息格式是否是 XML 格式
            XElement invokeMessage;
            //NetworkStream networkStream;
            try
            {
                invokeMessage = XElement.Parse(message);
                //networkStream = _tcpClient.GetStream();
            }
            catch (Exception ex)
            {
                Trace.TraceError($"解析消息失败: ({ex.GetType().Name}){ex.Message}");
                return null;
            }
#endif
            try
            {
                var networkStream = _tcpClient.GetStream();

                #region 清空读取缓冲区
                networkStream.ReadTimeout = 1;
                while (networkStream.DataAvailable)
                {
                    int bytesRead = networkStream.Read(ReadBuffer, 0, ReadBuffer.Length);
                    if (bytesRead <= 0) break;  // 没有数据了
                }
                #endregion

                networkStream.ReadTimeout = ReadTimeout;
                networkStream.WriteTimeout = WriteTimeout;

                var requestBytes = Encoding.UTF8.GetBytes(message);
                await networkStream.WriteAsync(requestBytes, 0, requestBytes.Length, cancelToken);
                //await networkStream.FlushAsync(cancelToken);

                Stopwatch stopwatch = Stopwatch.StartNew();
                while (!cancelToken.IsCancellationRequested && IsConnected && stopwatch.ElapsedMilliseconds < ReadTimeout)
                {
                    if (networkStream.DataAvailable)
                    {
                        var readBytesCount = await networkStream.ReadAsync(ReadBuffer, 0, ReadBuffer.Length, cancelToken);
                        if (readBytesCount == 0)
                        {
                            Trace.TraceWarning("远程服务端关闭了连接");
                            HandleDisconnection();
                            break;
                        }

                        // 这里可以解析响应消息
                        // 简化示例，实际实现需要更复杂的响应处理
                        string exceptionMessage = string.Empty;
                        try
                        {
                            var responseMessage = Encoding.UTF8.GetString(ReadBuffer, 0, readBytesCount);
                            Debug.WriteLine($"收到服务端的响应: {responseMessage}");

                            var result = XElement.Parse(responseMessage);
                            return new InvokeResult(result);
                        }
                        catch (Exception ex)
                        {
                            exceptionMessage = $"解析响应消息时格式异常: ({ex.GetType().Name}){ex.Message}";
                        }

                        try
                        {
                            XElement invokeMessage = XElement.Parse(message);
                            var objectMethod = $"{invokeMessage.Attributes(nameof(InvokeMessage.ObjectName))} .{invokeMessage.Attributes(nameof(InvokeMessage.MethodName))}";
                            
                            Trace.TraceError($"调用远程对象方法 {objectMethod} 异常，{exceptionMessage}");
                            return new InvokeResult(InvokeStatusCode.Failed, objectMethod, exceptionMessage);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError($"解析消息失败: ({ex.GetType().Name}){ex.Message}");
                            return null;
                        }
                    }
                    else
                    {
                        await Task.Delay(100, cancelToken);
                    }
                }

                stopwatch.Stop();
                return new InvokeResult(InvokeStatusCode.Timeout, null, "服务端响应超时");
            }
            catch (Exception ex)
            {
                Trace.TraceError($"发送消息失败: ({ex.GetType().Name}){ex.Message}");
                HandleDisconnection();
                return null;
            }
        }
        /// <summary>
        /// 异步调用远程实例对象的方法
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <returns></returns>
        public async Task<InvokeResult> CallRemoteMethodAsync(XElement invokeMessage) => await CallRemoteMethodAsync(invokeMessage.ToString(), _cts.Token);
        /// <summary>
        /// 异步调用远程实例对象的方法
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <returns></returns>
        public async Task<InvokeResult> CallRemoteMethodAsync(InvokeMessage2 invokeMessage) => await CallRemoteMethodAsync(invokeMessage.ToString(), _cts.Token);
        
        /// <summary>
        /// 异步调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public async Task<InvokeResult> CallRemoteMethodAsync(string objectName, string methodName) => await CallRemoteMethodAsync(new InvokeMessage2(objectName, methodName));
        /// <summary>
        /// 异步调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<InvokeResult> CallRemoteMethodAsync(string objectName, string methodName, object[] parameters) => await CallRemoteMethodAsync(new InvokeMessage2(objectName, methodName, parameters));
        /// <summary>
        /// 异步调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="asynchronous"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<InvokeResult> CallRemoteMethodAsync(string objectName, string methodName, bool asynchronous, object[] parameters) => await CallRemoteMethodAsync(new InvokeMessage2(objectName, methodName, asynchronous, parameters));

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed) return;

            _isDisposed = true;
            StopAsync().Wait();
        }
    }
}
