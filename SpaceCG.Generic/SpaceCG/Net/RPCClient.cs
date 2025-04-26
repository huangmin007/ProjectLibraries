using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using SpaceCG.Extensions;
using SpaceCG.Generic;

namespace SpaceCG.Net
{
    /// <summary>
    /// RPC (Remote Procedure Call) or (Reflection Program Control) Client
    /// </summary>
    public class RPCClient : IDisposable
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(RPCClient));

        private string remoteHost;
        private ushort remotePort;

        private byte[] buffer;
        private TcpClient tcpClient;

        /// <summary> 读取超时 </summary>
        public int ReadTimeout { get; set; } = 1000;
        /// <summary> 写入超时 </summary>
        public int WriteTimeout { get; set; } = 1000;

        /// <summary>
        /// 是否连接到远程主机
        /// </summary>
        public bool IsConnected
        {
            get
            {
                try
                {
                    return tcpClient?.Connected ?? false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
        /// <summary>
        /// <see cref="TcpClient"/> 客户端对象
        /// </summary>
        public TcpClient TcpClient => tcpClient;

        /// <summary>
        /// RPC (Remote Procedure Call) or (Reflection Program Control) Server
        /// </summary>
        public RPCClient()
        {
            this.buffer = new byte[RPCServer.BUFFER_SIZE];
        }
        /// <summary>
        /// RPC (Remote Procedure Call) or (Reflection Program Control) Server
        /// </summary>
        /// <param name="remoteHost"></param>
        /// <param name="remotePort"></param>
        public RPCClient(string remoteHost, ushort remotePort) : this()
        {
            this.remotePort = remotePort;
            this.remoteHost = remoteHost;
        }

        private bool _searching = false;
        /// <summary>
        /// 启动自动搜索 RPC 服务端，当搜索到服务端后会自动连接并中断搜索服务。
        /// </summary>
        /// <param name="remotePort"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public void StartSearchRPCServer(ushort remotePort, string name)
        {
            if (_searching) return;

            _searching = true;

            Task.Run(() =>
            {
                using (UdpClient udpClient = new UdpClient())
                {
                    udpClient.EnableBroadcast = true;
                    byte[] message = Encoding.UTF8.GetBytes($"<Broadcast Name=\"{name}\" />");

                    while (_searching)
                    {
                        if (!_searching) return;

                        udpClient.Send(message, message.Length, new IPEndPoint(IPAddress.Broadcast, remotePort));
                        Thread.Sleep(1000);

                        if (udpClient.Available > 0)
                        {
                            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

                            var bufffer = udpClient.Receive(ref remoteEP);
                            string response = Encoding.UTF8.GetString(bufffer);
                            Logger.Debug($"Search RPC Server {name} Response: {response} {remoteEP}");

                            if (!IsConnected) ConnectAsync(remoteEP.Address.ToString(), (ushort)remoteEP.Port);

                            _searching = false;
                        }
                    }
                }
            });
        }

        /// <summary>
        /// 同步连接远程服务端
        /// </summary>
        public void Connect()
        {
            if (remotePort <= 0) return;
            Close();

            try
            {
                tcpClient = new TcpClient();
                tcpClient.SendTimeout = WriteTimeout;
                tcpClient.ReceiveTimeout = ReadTimeout;
                tcpClient.Connect(remoteHost, remotePort);
            }
            catch (Exception ex)
            {
                Logger.Error($"RPC Client Connect(Sync) Exception: {ex}");
                return;
            }

            if (IsConnected)
                Logger.Info($"RPC Client {tcpClient.Client.LocalEndPoint} Connect(Sync) Server {tcpClient.Client.RemoteEndPoint} Success");
        }
        /// <summary>
        /// 连接远程服务端
        /// </summary>
        public async Task ConnectAsync()
        {
            if (remotePort <= 0) return;
            Close();
            //Trace.WriteLine($"RPC Client Connect(Async) {remoteHost}:{remotePort}");

            try
            {
                tcpClient = new TcpClient();
                tcpClient.SendTimeout = WriteTimeout;
                tcpClient.ReceiveTimeout = ReadTimeout;
                await tcpClient.ConnectAsync(remoteHost, remotePort);
            }
            catch (Exception ex)
            {
                Logger.Error($"RPC Client Connect(Async) Exception: {ex}");
            }

            if (IsConnected)
            {
                Logger.Info($"RPC Client {tcpClient.Client.LocalEndPoint} Connect(Async) Server {tcpClient.Client.RemoteEndPoint} Success");
            }
            else
            {
                Logger.Debug($"RPC Client Ready Reconnecting {remoteHost}:{remotePort}");
                //await Task.Delay(1000);
                //await ConnectAsync();
            }
        }
        /// <summary>
        /// 连接远程服务端
        /// </summary>
        /// <param name="remoteHost"></param>
        /// <param name="remotePort"></param>
        public async Task ConnectAsync(string remoteHost, ushort remotePort)
        {
            this.remotePort = remotePort;
            this.remoteHost = remoteHost;

            await ConnectAsync();
        }

        /// <summary> 关闭连接 </summary>
        public void Close()
        {
            try
            {
                if (tcpClient != null && tcpClient.Connected)
                {
                    tcpClient.Close();
                }

                tcpClient?.Dispose();
            }
            catch { }
            finally
            {
                tcpClient = null;
            }
        }
        /// <inheritdoc />
        public void Dispose()
        {
            _searching = false;

            Close();

            buffer = null;
            remotePort = 0;
            remoteHost = null;
        }

        private Stopwatch _readStopwatch = new Stopwatch();

        /// <summary>
        /// 写一次消息，并同步等待响应消息。
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <param name="responseMessage"></param>
        /// <returns>写-读成功返回 true, 否则返回 false </returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected bool ReadWriteMessage(string invokeMessage, out string responseMessage)
        {
            if (string.IsNullOrWhiteSpace(invokeMessage))
                throw new ArgumentNullException(nameof(invokeMessage), "参数不能为空");

            responseMessage = null;
            if (!IsConnected) Connect();
            if (!IsConnected)
            {
                Logger.Error("RPC Client Connect(Sync) Failed");
                return false;
            }

            NetworkStream networkStream = null;
            try
            {
                networkStream = tcpClient.GetStream();
                networkStream.ReadTimeout = ReadTimeout;
                networkStream.WriteTimeout = WriteTimeout;

                //clear read buffer
                while (networkStream.DataAvailable)
                {
                    int count = networkStream.Read(buffer, 0, buffer.Length);
                    Logger.Warn($"RPC Client Clear Buffer Size: {count}");
                }

                byte[] bytes = Encoding.UTF8.GetBytes(invokeMessage);
                networkStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Logger.Error($"RPC Client Write Message Exception: {ex}");
                return false;
            }

            try
            {
                _readStopwatch.Restart();
                while(networkStream != null && !networkStream.DataAvailable && _readStopwatch.ElapsedMilliseconds < 100)
                {
                    Thread.Sleep(1);
                }

                if (networkStream != null && !networkStream.DataAvailable)
                {
                    // 响应超时
                    return false;
                }
                else
                {
                    while (networkStream != null && networkStream.DataAvailable)
                    {
                        int count = networkStream.Read(buffer, 0, buffer.Length);

                        if (count <= 0)
                        {
                            Logger.Warn("RPC Server is Closed.");
                            return false;
                        }

                        responseMessage = Encoding.UTF8.GetString(buffer, 0, count)?.Trim();
                        Logger.Debug($"Receive RPC Server Invoke Result {count} Bytes \r\n{responseMessage}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"RPC Client Read Message Exception: {ex}");
                return false;
            }

            return true;
        }


        /// <summary>
        /// 调用远程实例对象的方法
        /// <para>返回结果为 true 时, 此时输出参数 <see cref="InvokeResult"/> 不为 null</para>
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <param name="invokeResult"></param>
        /// <returns>远程服务端有任何响应信息时, 都会返回 true, 否则返回 false</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public bool TryCallMethod(XElement invokeMessage, out InvokeResult invokeResult)
        {
            if (invokeMessage == null)
                throw new ArgumentNullException(nameof(invokeMessage), "参数不能为空");
            if (!InvokeMessage.IsValid(invokeMessage))
                throw new ArgumentException(nameof(invokeMessage), $"{nameof(InvokeMessage)} 调用消息不符合协议要求");

            invokeResult = null;
            if (!ReadWriteMessage(invokeMessage.ToString(), out string responseMessage))
            {
                return false;
            }

            XElement element = null;
            try
            {
                element = XElement.Parse(responseMessage);
                if (!InvokeResult.IsValid(element)) throw new FormatException("响应消息不符合协议要求");
            }
            catch (Exception ex)
            {
                invokeResult = new InvokeResult(InvokeStatusCode.Unknown, new InvokeMessage(invokeMessage).ObjectMethod, $"RPC Server Invoke Result Message Format Exception: {ex.Message}");

                Logger.Warn($"RPC Server Invoke Result Message: {responseMessage}");
                Logger.Error($"RPC Server Invoke Result Message Format Exception: {ex}");
                return true;
            }

            invokeResult = new InvokeResult(element);

            return true;
        }
        /// <summary>
        /// 调用远程实例对象的方法
        /// <para>返回结果为 true 时, 此时输出参数 <see cref="InvokeResult"/> 不为 null</para>
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <param name="invokeResult"></param>
        /// <returns>远程服务端有任何响应信息时, 都会返回 true, 否则返回 false</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public bool TryCallMethod(InvokeMessage invokeMessage, out InvokeResult invokeResult)
        {
            if (invokeMessage == null)
                throw new ArgumentNullException(nameof(invokeMessage), "参数不能为空");
            if (!invokeMessage.IsValid())
                throw new ArgumentException(nameof(invokeMessage), $"{nameof(InvokeMessage)} 调用消息不符合协议要求");

            invokeResult = null;
            if (!ReadWriteMessage(invokeMessage.ToFormatString(MessageFormatType.XML), out string responseMessage))
            {
                return false;
            }

            XElement element = null;
            try
            {
                element = XElement.Parse(responseMessage);
                if (!InvokeResult.IsValid(element)) throw new FormatException("响应消息不符合协议要求");
            }
            catch (Exception ex)
            {
                invokeResult = new InvokeResult(InvokeStatusCode.Unknown, invokeMessage.ObjectMethod, $"RPC Server Invoke Result Message Format Exception: {ex.Message}");

                Logger.Info($"RPC Server Invoke Result Message: {responseMessage}");
                Logger.Error($"RPC Server Invoke Result Message Format Exception: {ex}");
                return true;
            }

            invokeResult = new InvokeResult(element);

            return true;
        }
        /// <summary>
        /// 调用远程实例对象的方法
        /// <para>返回结果为 true 时, 此时输出参数 <see cref="InvokeResult"/> 不为 null</para>
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="invokeResult"></param>
        /// <returns>远程服务端有任何响应信息时, 都会返回 true, 否则返回 false</returns>
        public bool TryCallMethod(string objectName, string methodName, out InvokeResult invokeResult) => TryCallMethod(new InvokeMessage(objectName, methodName), out invokeResult);
        /// <summary>
        /// 调用远程实例对象的方法
        /// <para>返回结果为 true 时, 此时输出参数 <see cref="InvokeResult"/> 不为 null</para>
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="invokeResult"></param>
        /// <returns>远程服务端有任何响应信息时, 都会返回 true, 否则返回 false</returns>
        public bool TryCallMethod(string objectName, string methodName, object[] parameters, out InvokeResult invokeResult) => TryCallMethod(new InvokeMessage(objectName, methodName, parameters), out invokeResult);
        /// <summary>
        /// 调用远程实例对象的方法
        /// <para>返回结果为 true 时, 此时输出参数 <see cref="InvokeResult"/> 不为 null</para>
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="asynchronous">远程方法或函数是否异步执行，需要远端应用的支持</param>
        /// <param name="invokeResult"></param>
        /// <returns>远程服务端有任何响应信息时, 都会返回 true, 否则返回 false</returns>
        public bool TryCallMethod(string objectName, string methodName, object[] parameters, bool asynchronous, out InvokeResult invokeResult) => TryCallMethod(new InvokeMessage(objectName, methodName, parameters, asynchronous, null), out invokeResult);


        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <returns>远程实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象，否则返回 null 值 </returns>
        public InvokeResult TryCallMethod(XElement invokeMessage) => TryCallMethod(invokeMessage, out InvokeResult invokeResult) ? invokeResult : null;
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <returns>远程实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象，否则返回 null 值 </returns>
        public InvokeResult TryCallMethod(InvokeMessage invokeMessage) => TryCallMethod(invokeMessage, out InvokeResult invokeResult) ? invokeResult : null;
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <returns>远程实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象，否则返回 null 值 </returns>
        public InvokeResult TryCallMethod(string objectName, string methodName) => TryCallMethod(new InvokeMessage(objectName, methodName), out InvokeResult invokeResult)  ? invokeResult : null;
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns>远程实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象，否则返回 null 值 </returns>
        public InvokeResult TryCallMethod(string objectName, string methodName, object[] parameters) => TryCallMethod(new InvokeMessage(objectName, methodName, parameters), out InvokeResult invokeResult) ? invokeResult : null;
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="asynchronous">远程方法或函数是否异步执行，需要远端应用的支持</param>
        /// <returns>远程实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象，否则返回 null 值</returns>
        public InvokeResult TryCallMethod(string objectName, string methodName, object[] parameters, bool asynchronous) => TryCallMethod(new InvokeMessage(objectName, methodName, parameters, asynchronous, null), out InvokeResult invokeResult) ? invokeResult : null;


        /// <summary>
        /// 调用远程实例对象的方法
        /// <para></para>
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <returns>远程服务端有任何响应信息时, 异步操作返回对象 <see cref="InvokeResult"/> 不为 null </returns>
        public Task<InvokeResult> TryCallMethodAsync(XElement invokeMessage) => Task.Run(() => TryCallMethod(invokeMessage, out InvokeResult invokeResult) ? invokeResult : null);
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <returns>远程服务端有任何响应信息时, 异步操作返回对象 <see cref="InvokeResult"/> 不为 null </returns>
        public Task<InvokeResult> TryCallMethodAsync(InvokeMessage invokeMessage) => Task.Run(() => TryCallMethod(invokeMessage, out InvokeResult invokeResult) ? invokeResult : null);
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <returns>远程服务端有任何响应信息时, 异步操作返回对象 <see cref="InvokeResult"/> 不为 null </returns>
        public Task<InvokeResult> TryCallMethodAsync(string objectName, string methodName) => Task.Run(() => TryCallMethod(objectName, methodName, out InvokeResult invokeResult) ? invokeResult : null);
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns>远程服务端有任何响应信息时, 异步操作返回对象 <see cref="InvokeResult"/> 不为 null </returns>
        public Task<InvokeResult> TryCallMethodAsync(string objectName, string methodName, object[] parameters) => Task.Run(() => TryCallMethod(objectName, methodName, parameters, out InvokeResult invokeResult) ? invokeResult : null);
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="asynchronous">远程方法或函数是否异步执行，需要远端应用的支持</param>
        /// <returns>远程服务端有任何响应信息时, 异步操作返回对象 <see cref="InvokeResult"/> 不为 null</returns>
        public Task<InvokeResult> TryCallMethodAsync(string objectName, string methodName, object[] parameters, bool asynchronous) => Task.Run(() => TryCallMethod(objectName, methodName, parameters, asynchronous, out InvokeResult invokeResult) ? invokeResult : null);


        /// <summary>
        /// 调用远程多个实例对象的方法
        /// </summary>
        /// <param name="invokeMessages"></param>
        /// <param name="invokeResults"></param>
        /// <returns>远程服务端有任何响应信息时, 都会返回 true, 否则返回 false</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public bool TryCallMethods(XElement invokeMessages, out IEnumerable<InvokeResult> invokeResults)
        {
            if (invokeMessages == null)
                throw new ArgumentNullException(nameof(invokeMessages), "参数不能为空");
            if (!InvokeMessage.IsValid(invokeMessages))
                throw new ArgumentException(nameof(invokeMessages), $"{nameof(invokeMessages)} 调用消息不符合协议要求");

            invokeResults = null;
            if (!ReadWriteMessage(invokeMessages.ToString(), out string responseMessage))
            {
                return false;
            }

            XElement elements = null;
            try
            {
                elements = XElement.Parse(responseMessage);
                if (!InvokeResult.IsValid(elements)) throw new FormatException("响应消息不符合协议要求");
            }
            catch (Exception ex)
            {
                invokeResults = Enumerable.Empty<InvokeResult>();
                Logger.Info($"RPC Server Invoke Result: {responseMessage}");
                Logger.Error($"RPC Server Invoke Result Format Exception: {ex}");
                return true;
            }

            var xResults = elements.Elements(nameof(InvokeResult));
            InvokeResult[] iResults = new InvokeResult[xResults.Count()];
            for (int i = 0; i < xResults.Count(); i++)
            {
                iResults[i] = new InvokeResult(xResults.ElementAt(i));
            }

            invokeResults = iResults;
            return true;
        }
        /// <summary>
        /// 调用远程多个实例对象的方法
        /// </summary>
        /// <param name="invokeMessages"></param>
        /// <param name="invokeResults"></param>
        /// <returns>远程服务端有任何响应信息时, 都会返回 true, 否则返回 false</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool TryCallMethods(IEnumerable<InvokeMessage> invokeMessages, out IEnumerable<InvokeResult> invokeResults)
        {
            if (invokeMessages?.Count() == 0)
                throw new ArgumentNullException(nameof(invokeMessages), "参数不能为空");

            return TryCallMethods(XElement.Parse(InvokeMessage.ToFormatString(invokeMessages, MessageFormatType.XML)), out invokeResults);
        }
        /// <summary>
        /// 调用远程多个实例对象的方法
        /// </summary>
        /// <param name="invokeMessages"></param>
        /// <returns>远程实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象的集合，否则返回空集合 </returns>
        public IEnumerable<InvokeResult> TryCallMethods(XElement invokeMessages) => TryCallMethods(invokeMessages, out IEnumerable<InvokeResult> invokeResults) ? invokeResults : Enumerable.Empty<InvokeResult>();
        /// <summary>
        /// 调用远程多个实例对象的方法
        /// </summary>
        /// <param name="invokeMessages"></param>
        /// <returns>远程实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象的集合，否则返回空集合 </returns>
        public IEnumerable<InvokeResult> TryCallMethods(IEnumerable<InvokeMessage> invokeMessages) => TryCallMethods(invokeMessages, out IEnumerable<InvokeResult> invokeResults) ? invokeResults : Enumerable.Empty<InvokeResult>();
        /// <summary>
        /// 调用远程多个实例对象的方法
        /// </summary>
        /// <param name="invokeMessages"></param>
        /// <returns>远程实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象的集合，否则返回空集合</returns>
        public Task<IEnumerable<InvokeResult>> TryCallMethodsAsync(XElement invokeMessages) => Task.Run(() => TryCallMethods(invokeMessages));
        /// <summary>
        /// 调用远程多个实例对象的方法
        /// </summary>
        /// <param name="invokeMessages"></param>
        /// <returns>远程实例对象的方法调用成功，返回 <see cref="InvokeResult"/> 对象的集合，否则返回空集合</returns>
        public Task<IEnumerable<InvokeResult>> TryCallMethodsAsync(IEnumerable<InvokeMessage> invokeMessages) => Task.Run(() => TryCallMethods(invokeMessages));


#if false
        public bool TryCallMethod(System.Text.Json.JsonDocument invokeMessage, out InvokeResult invokeResult) { }
        public InvokeResult TryCallMethod(System.Text.Json.JsonDocument invokeMessage) { }
        public Task<InvokeResult> TryCallMethodAsync(System.Text.Json.JsonDocument invokeMessage) { }

        public bool TryCallMethods(System.Text.Json.JsonDocument invokeMessages, out IEnumerable<InvokeResult> invokeResults) { }
        public IEnumerable<InvokeResult> TryCallMethods(System.Text.Json.JsonDocument invokeMessages) { }
        public Task<IEnumerable<InvokeResult>> TryCallMethodsAsync(System.Text.Json.JsonDocument invokeMessages) { }
#endif
    }
}
