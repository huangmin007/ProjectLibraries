using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
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
    public class RPCClient:IDisposable
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(RPCClient));

        private string remoteHost;
        private ushort remotePort;

        private byte[] buffer;
        private TcpClient tcpClient;

        /// <summary>
        /// 远程方法调用异常时是否在本在抛出
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
        /// <param name="remoteAddress"></param>
        /// <param name="remotePort"></param>
        public RPCClient(string remoteAddress, ushort remotePort)
        {
            buffer = new byte[4096];
            this.remotePort = remotePort;
            this.remoteHost = remoteAddress;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (tcpClient != null)
            {
                tcpClient?.Dispose();
                tcpClient = null;
            }

            remotePort = 0;
            remoteHost = null;
            buffer = null;
        }

        /// <summary>
        /// 连接远程服务端
        /// </summary>
        public async void ConnectAsync()
        {
            if (!IsConnected)
            {
                tcpClient?.Dispose();
                tcpClient = null;
            }

            if (remotePort <= 0) return;

            try
            {
                tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(remoteHost, remotePort);
            }
            catch(Exception ex)
            {
                Logger.Error($"RPC Client Connect(Async) Exception: {ex}");
            }

            if(!IsConnected)
            {
                Logger.Debug($"RPC Client Reconnecting {remoteHost}:{remotePort}");
                await Task.Delay(1000);
                ConnectAsync();
                return;
            }
            Logger.Info($"RPC Client Connect(Async) Server {tcpClient.Client.RemoteEndPoint} Success");
        }

        /// <summary>
        /// 同步连接远程服务端
        /// </summary>
        protected void Connect()
        {
            if (!IsConnected)
            {
                tcpClient?.Dispose();
                tcpClient = null;
            }

            if (remotePort <= 0) return;

            try
            {
                tcpClient = new TcpClient();
                tcpClient.Connect(remoteHost, remotePort);
            }
            catch(Exception ex)
            {
                Logger.Error($"RPC Client Connect(Sync) Exception: {ex}");
                return;
            }

            if(IsConnected)
                Logger.Info($"RPC Client Connect(Sync) Server {tcpClient.Client.RemoteEndPoint} Success");
        }

        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public object CallMethod(XElement action)
        {
            if (action == null || action.Name.LocalName != "Action")
                throw new ArgumentException(nameof(action), "参数不能为空或参数格式错误");

            if(!IsConnected) Connect();
            if(!IsConnected)
            {
                Logger.Warn($"RPC Client Connect(Sync) Failed");
                return null;
            }

            NetworkStream networkStream = null;

            try
            {
                networkStream = tcpClient.GetStream();
                networkStream.ReadTimeout = Timeout;
                networkStream.WriteTimeout = Timeout;

                //clear read buffer
                if (networkStream.DataAvailable)
                {
                    int count = networkStream.Read(buffer, 0, buffer.Length);
                    Logger.Warn($"RPC Client Read/Clear Buffer {count}");
                }

                byte[] bytes = Encoding.UTF8.GetBytes(action.ToString());
                networkStream.Write(bytes, 0, bytes.Length);
            }
            catch(Exception ex)
            {
                Logger.Error($"RPC Client Write Exception: {ex}");
                return null;
            }

            string response = null;
            try
            {
                while (networkStream != null)
                {
                    int count = networkStream.Read(buffer, 0, buffer.Length);

                    if (count <= 0)
                    {
                        ConnectAsync();
                        return null;
                    }

                    response = Encoding.UTF8.GetString(buffer, 0, count);
                    Logger.Debug($"Read Count::{count} {response}");
                    break;
                }
            }
            catch(Exception ex)
            {
                Logger.Error($"RPC Client Read Exception: {ex}");
            }

            if (string.IsNullOrWhiteSpace(response)) return null;
            XElement element = null;
            try { element = XElement.Parse(response); }
            catch (Exception) { return null; }

            ReturnObject result = new ReturnObject(element);
            if (result.Code != 0 && this.IsThrowException)
                throw new Exception(result.Exception);

            return result.Value;
        }
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public Task<object> CallMethodAsync(XElement action) => Task.Run(() => CallMethod(action));

        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public object CallMethod(string objectName, string methodName) => CallMethod(objectName, methodName, null, true);
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public Task<object> CallMethodAsync(string objectName, string methodName) => Task.Run(() => CallMethod(objectName, methodName, null, true));

        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public object CallMethod(string objectName, string methodName, object[] parameters) => CallMethod(objectName, methodName, parameters, true);
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Task<object> CallMethodAsync(string objectName, string methodName, object[] parameters) => Task.Run(()=>CallMethod(objectName, methodName, parameters, true));

        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="synchronous"></param>
        /// <returns></returns>
        public object CallMethod(string objectName, string methodName, object[] parameters, bool synchronous)
        {
            StringBuilder builder = new StringBuilder(1024);            
            builder.AppendLine($"<Action Object=\"{objectName}\" Method=\"{methodName}\" Sync=\"{synchronous}\">");
            if (parameters?.Length > 0)
            {
                foreach (var param in parameters)
                {
                    builder.AppendLine($"<Param Type=\"{param.GetType()}\">{param}</Param>");
                }
            }
            builder.Append("</Action>");

            return CallMethod(XElement.Parse(builder.ToString()));
        }
        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="synchronous"></param>
        /// <returns></returns>
        public Task<object> CallMethodAsync(string objectName, string methodName, object[] parameters, bool synchronous)
        {
            return Task.Run(() => CallMethod(objectName, methodName, parameters, synchronous));
        }
    }
}
