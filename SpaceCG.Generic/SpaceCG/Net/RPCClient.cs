using System;
using System.Diagnostics;
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
        /// 远程方法调用异常时是否在本地抛出
        /// </summary>
        public bool IsThrowException { get; set; } = false;

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
        /// RPC (Remote Procedure Call) or (Reflection Program Control) Server
        /// </summary>
        public RPCClient()
        {
            this.buffer = new byte[RPCServer.BufferSize];
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
        /// 调用远程实例对象的方法
        /// <para>服务端有响应时, 返回 true, 此时输出参数 out returnResult 不为 null</para>
        /// </summary>
        /// <param name="action"></param>
        /// <param name="returnResult"></param>
        /// <returns>服务端有响应时, 返回 true, 否则返回 false</returns>
        public bool TryCallMethod(XElement action, out MethodInvokeResult returnResult)
        {
            returnResult = null;
            if (tcpClient == null)
            {
                Logger.Warn($"{nameof(TcpClient)} 已释放的对象执行操作异常");
                return false;
            }
            if (!MethodInvokeMessage.CheckFormat(action))
            {
                Logger.Warn($"{nameof(action)} 参数不能为空或参数格式错误");
                return false;
            }

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

                byte[] bytes = Encoding.UTF8.GetBytes(action.ToString());
                networkStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Logger.Error($"RPC Client Write Exception: {ex}");
                return false;
            }

            string responseMessage = null;
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

                    responseMessage = Encoding.UTF8.GetString(buffer, 0, count);
                    Logger.Debug($"RPC Client Read Size: {count}, Response Message: {responseMessage}");
                    break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"RPC Client Read Exception: {ex}");
                return false;
            }

            XElement element = null;
            try
            {
                element = XElement.Parse(responseMessage);
            }
            catch (Exception ex)
            {
                Logger.Info($"RPC Client Receive Message: {responseMessage}");
                Logger.Error($"RPC Server Return Result Format Exception: {ex}");
                return false;
            }

            returnResult = new MethodInvokeResult(element);
            if (IsThrowException && returnResult.Status != InvokeStatus.Success)
                throw new Exception(returnResult.ExceptionMessage);

            return true;
        }
        /// <summary>
        /// 调用远程实例对象的方法
        /// <para></para>
        /// </summary>
        /// <param name="action"></param>
        /// <returns>服务端有响应时, 异步操作返回对象 <see cref="MethodInvokeResult"/> 不为 null </returns>
        public Task<MethodInvokeResult> TryCallMethodAsync(XElement action) => Task.Run(() =>
        {
            return TryCallMethod(action, out MethodInvokeResult invokeResult) ? invokeResult : null;
        });

        /// <summary>
        /// 调用远程实例对象的方法
        /// <para>服务端有响应时, 返回 true, 此时输出参数 out returnResult 不为 null</para>
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="returnResult"></param>
        /// <returns>服务端有响应时, 返回 true, 否则返回 false</returns>
        public bool TryCallMethod(string objectName, string methodName, out MethodInvokeResult returnResult) 
            => TryCallMethod(objectName, methodName, null, true, out returnResult);
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <returns>服务端有响应时, 异步操作返回对象 <see cref="MethodInvokeResult"/> 不为 null </returns>
        public Task<MethodInvokeResult> TryCallMethodAsync(string objectName, string methodName) => Task.Run(() =>
        {
            return TryCallMethod(objectName, methodName, null, true, out MethodInvokeResult invokeResult) ? invokeResult : null;
        });

        /// <summary>
        /// 调用远程实例对象的方法
        /// <para>服务端有响应时, 返回 true, 此时输出参数 out returnResult 不为 null</para>
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="returnResult"></param>
        /// <returns>服务端有响应时, 返回 true, 否则返回 false</returns>
        public bool TryCallMethod(string objectName, string methodName, object[] parameters, out MethodInvokeResult returnResult) 
            => TryCallMethod(objectName, methodName, parameters, true, out returnResult);
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns>服务端有响应时, 异步操作返回对象 <see cref="MethodInvokeResult"/> 不为 null </returns>
        public Task<MethodInvokeResult> TryCallMethodAsync(string objectName, string methodName, object[] parameters) => Task.Run(() =>
        {
            return TryCallMethod(objectName, methodName, parameters, true, out MethodInvokeResult invokeResult) ? invokeResult : null;
        });

        /// <summary>
        /// 调用远程实例对象的方法
        /// <para>服务端有响应时, 返回 true, 此时输出参数 out returnResult 不为 null</para>
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="synchronous"></param>
        /// <param name="returnResult"></param>
        /// <returns>服务端有响应时, 返回 true, 否则返回 false</returns>
        public bool TryCallMethod(string objectName, string methodName, object[] parameters, bool synchronous, out MethodInvokeResult returnResult)
            => TryCallMethod(MethodInvokeMessage.Create(objectName, methodName, parameters, synchronous), out returnResult);
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="synchronous"></param>
        /// <returns>服务端有响应时, 异步操作返回对象 <see cref="MethodInvokeResult"/> 不为 null </returns>
        public Task<MethodInvokeResult> TryCallMethodAsync(string objectName, string methodName, object[] parameters, bool synchronous) => Task.Run(() =>
        {
             return TryCallMethod(objectName, methodName, parameters, synchronous, out MethodInvokeResult invokeResult) ? invokeResult : null;
        });
        
    }
}
