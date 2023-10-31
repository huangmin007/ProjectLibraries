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
    public class RPCClient:IDisposable
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
            this.buffer = new byte[2048];
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
            Close();
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

            if(IsConnected)
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
            Close();
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
                Logger.Info($"RPC Client {tcpClient.Client.LocalEndPoint} Connect(Sync) Server {tcpClient.Client.RemoteEndPoint} Success");
        }

        /// <summary>
        /// 调用远程实例对象的方法
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public object CallMethod(XElement action)
        {
            if (tcpClient == null)
                throw new ObjectDisposedException(nameof(TcpClient), $"{nameof(TcpClient)} 已释放的对象执行操作异常");

            if (action == null || action.Name.LocalName != "Action")
                throw new ArgumentException(nameof(action), "参数不能为空或参数格式错误");

            if (!IsConnected) Connect();
            if (!IsConnected)
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
                return null;
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
                        Logger.Warn($"RPC Server is Closed");
                        return null;
                    }

                    responseMessage = Encoding.UTF8.GetString(buffer, 0, count);
                    Logger.Debug($"RPC Client Read Buffer Size: {count}  Message: {responseMessage}");
                    break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"RPC Client Read Exception: {ex}");
            }

            if (string.IsNullOrWhiteSpace(responseMessage)) return null;
            XElement element = null;
            try 
            {
                element = XElement.Parse(responseMessage); 
            }
            catch (Exception ex)
            {
                Logger.Warn($"RPC Client Respose Message: {responseMessage}");
                Logger.Error($"RPC Client Respose Message Exception: {ex}");
                return null; 
            }

            ReturnObject result = new ReturnObject(element);
            if (result.Code != 0 && IsThrowException) throw new Exception(result.Exception);

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
