using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
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

        /// <summary>
        /// Read/Write Timeout
        /// </summary>
        public int Timeout { get; set; } = 3000;

        /// <summary>
        /// 是否连接到远程主机
        /// </summary>
        public bool IsConnected
        {
            get
            {
                try
                {
                    if (tcpClient == null) return false;
                    return tcpClient.Connected;
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

        /// <summary>
        /// 连接远程服务端
        /// </summary>
        public async void ConnectAsync()
        {
            if (remotePort <= 0) return;
            Close();

            try
            {
                tcpClient = new TcpClient();
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
                await Task.Delay(1000);
                ConnectAsync();
            }
        }
        /// <summary>
        /// 连接远程服务端
        /// </summary>
        /// <param name="remoteHost"></param>
        /// <param name="remotePort"></param>
        public void ConnectAsync(string remoteHost, ushort remotePort)
        {
            this.remotePort = remotePort;
            this.remoteHost = remoteHost;

            ConnectAsync();
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            try
            {
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
            Close();

            buffer = null;
            remotePort = 0;
            remoteHost = null;
        }

        /// <summary>
        /// 同步连接远程服务端
        /// </summary>
        protected void Connect()
        {
            if (remotePort <= 0) return;
            Close();

            try
            {
                tcpClient = new TcpClient();
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
        /// Write And Read Messages
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <param name="invokeResult"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected bool WriteAndReadMessages(string invokeMessage, out string invokeResult)
        {
            if (string.IsNullOrWhiteSpace(invokeMessage))
                throw new ArgumentNullException(nameof(invokeMessage), "参数不能为空");

            invokeResult = null;
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
                networkStream.ReadTimeout = Timeout;
                networkStream.WriteTimeout = Timeout;

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
                Logger.Error($"RPC Client Write Exception: {ex}");
                return false;
            }

            try
            {
                while (networkStream != null)
                {
                    int count = networkStream.Read(buffer, 0, buffer.Length);

                    if (count <= 0)
                    {
                        ConnectAsync();
                        Logger.Warn("RPC Server is Closed");
                        return false;
                    }

                    invokeResult = Encoding.UTF8.GetString(buffer, 0, count);
                    Logger.Debug($"RPC Client Read Size: {count}, Response Message: {invokeResult}");
                    break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"RPC Client Read Exception: {ex}");
                return false;
            }

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

            string InvokeMessages = $"{nameof(InvokeMessage)}s";
            StringBuilder builer = new StringBuilder(RPCServer.BUFFER_SIZE);
            builer.AppendLine($"<{InvokeMessages}>");
            for (int i = 0; i < invokeMessages.Count(); i++)
            {
                builer.AppendLine(invokeMessages.ElementAt(i).ToXElementString());
            }
            builer.AppendLine($"</{InvokeMessages}>");

            return TryCallMethods(XElement.Parse(builer.ToString()), out invokeResults); ;
        }
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="invokeMessages"></param>
        /// <param name="invokeResults"></param>
        /// <returns>远程服务端有任何响应信息时, 都会返回 true, 否则返回 false</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public bool TryCallMethods(XElement invokeMessages, out IEnumerable<InvokeResult> invokeResults)
        {
            if (invokeMessages?.Elements(nameof(InvokeMessage)).Count() == 0)
                throw new ArgumentNullException(nameof(invokeMessages), "参数不能为空");

            if (invokeMessages.Name.LocalName != $"{nameof(InvokeMessage)}s")
                throw new ArgumentException(nameof(invokeMessages), "格式协议错误");

            invokeResults = Enumerable.Empty<InvokeResult>();
            if (!WriteAndReadMessages(invokeMessages.ToString(), out string responseMessage))
            {
                return false;
            }

            XElement elements = null;
            try
            {
                elements = XElement.Parse(responseMessage);
            }
            catch (Exception ex)
            {
                Logger.Info($"RPC Client Receive Message: {responseMessage}");
                Logger.Error($"RPC Server Return Result Format Exception: {ex}");
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
        /// 调用远程实例对象的方法
        /// <para>返回结果为 true 时, 此时输出参数 <see cref="InvokeResult"/> 不为 null</para>
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <param name="invokeResult"></param>
        /// <param name="throwException">远程方法或函数执行异常时，是否在本地抛出异常信息</param>
        /// <returns>远程服务端有任何响应信息时, 都会返回 true, 否则返回 false</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        public bool TryCallMethod(InvokeMessage invokeMessage, out InvokeResult invokeResult, bool throwException = false)
        {
            invokeResult = null;
            if (invokeMessage == null) throw new ArgumentNullException(nameof(invokeMessage), "参数不能为空");

            if (!WriteAndReadMessages(invokeMessage.ToXElementString(), out string responseMessage))
            {
                return false;
            }

            XElement element = null;
            try
            {
                element = XElement.Parse(responseMessage);
            }
            catch (Exception ex)
            {
                invokeResult = new InvokeResult(StatusCodes.Unknow, invokeMessage.ObjectMethod, $"RPC Server Invoke Result Format Exception: {ex.Message}");

                Logger.Info($"RPC Client Receive Message: {responseMessage}");
                Logger.Error($"RPC Server Return Result Format Exception: {ex}");
                if (throwException) throw ex;
                return true;
            }

            invokeResult = new InvokeResult(element);
            if (string.IsNullOrEmpty(invokeResult.ObjectMethod)) 
                invokeResult.ObjectMethod = invokeMessage.ObjectMethod;

            if (throwException && invokeResult.StatusCode < StatusCodes.Success)
                throw new Exception(invokeResult.ExceptionMessage);

            return true;
        }
        /// <summary>
        /// 调用远程实例对象的方法
        /// <para>返回结果为 true 时, 此时输出参数 <see cref="InvokeResult"/> 不为 null</para>
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <param name="invokeResult"></param>
        /// <param name="throwException">远程方法或函数执行异常时，是否在本地抛出异常信息</param>
        /// <returns>远程服务端有任何响应信息时, 都会返回 true, 否则返回 false</returns>
        public bool TryCallMethod(XElement invokeMessage, out InvokeResult invokeResult, bool throwException = false)
        {
            if (invokeMessage == null)
                throw new ArgumentNullException(nameof(invokeMessage), "参数不能为空");
            if (!InvokeMessage.IsValid(invokeMessage))
                throw new ArgumentException(nameof(invokeMessage), "参数不符合协议要求");

            invokeResult = null;
            if (!WriteAndReadMessages(invokeMessage.ToString(), out string responseMessage))
            {
                return false;
            }

            XElement element = null;
            try
            {
                element = XElement.Parse(responseMessage);
            }
            catch (Exception ex)
            {
                invokeResult = new InvokeResult(StatusCodes.Unknow, new InvokeMessage(invokeMessage).ObjectMethod, $"RPC Server Invoke Result Format Exception: {ex.Message}");

                Logger.Info($"RPC Client Receive Message: {responseMessage}");
                Logger.Error($"RPC Server Return Result Format Exception: {ex}");

                if (throwException) throw ex;
                return true;
            }

            invokeResult = new InvokeResult(element);
            if (string.IsNullOrEmpty(invokeResult.ObjectMethod))
            {
                invokeResult.ObjectMethod = new InvokeMessage(invokeMessage).ObjectMethod;
            }

            if (throwException && invokeResult.StatusCode < StatusCodes.Success)
                throw new Exception(invokeResult.ExceptionMessage);

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
        public bool TryCallMethod(string objectName, string methodName, out InvokeResult invokeResult)
            => TryCallMethod(new InvokeMessage(objectName, methodName), out invokeResult, false);
        /// <summary>
        /// 调用远程实例对象的方法
        /// <para>返回结果为 true 时, 此时输出参数 <see cref="InvokeResult"/> 不为 null</para>
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="invokeResult"></param>
        /// <returns>远程服务端有任何响应信息时, 都会返回 true, 否则返回 false</returns>
        public bool TryCallMethod(string objectName, string methodName, object[] parameters, out InvokeResult invokeResult)
            => TryCallMethod(new InvokeMessage(objectName, methodName, parameters, true, null), out invokeResult, false);


        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="message"></param>
        /// <returns>远程服务端有任何响应信息时, 异步操作返回对象 <see cref="InvokeResult"/> 不为 null </returns>
        public Task<InvokeResult> TryCallMethodAsync(InvokeMessage message) => Task.Run(() =>
        {
            return TryCallMethod(message, out InvokeResult invokeResult) ? invokeResult : null;
        });
        /// <summary>
        /// 调用远程实例对象的方法
        /// <para></para>
        /// </summary>
        /// <param name="message"></param>
        /// <returns>远程服务端有任何响应信息时, 异步操作返回对象 <see cref="InvokeResult"/> 不为 null </returns>
        public Task<InvokeResult> TryCallMethodAsync(XElement message) => Task.Run(() =>
        {
            return TryCallMethod(message, out InvokeResult invokeResult) ? invokeResult : null;
        });
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <returns>远程服务端有任何响应信息时, 异步操作返回对象 <see cref="InvokeResult"/> 不为 null </returns>
        public Task<InvokeResult> TryCallMethodAsync(string objectName, string methodName) => Task.Run(() =>
        {
            return TryCallMethod(objectName, methodName, out InvokeResult invokeResult) ? invokeResult : null;
        });
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns>远程服务端有任何响应信息时, 异步操作返回对象 <see cref="InvokeResult"/> 不为 null </returns>
        public Task<InvokeResult> TryCallMethodAsync(string objectName, string methodName, object[] parameters) => Task.Run(() =>
        {
            return TryCallMethod(objectName, methodName, parameters, out InvokeResult invokeResult) ? invokeResult : null;
        });
                
    }
}
