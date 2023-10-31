using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using SpaceCG.Extensions;
using SpaceCG.Generic;

namespace SpaceCG.Net
{
    /// <summary>
    /// RPC (Remote Procedure Call) or (Reflection Program Control) Server
    /// </summary>
    public class RPCServer : IDisposable
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(RPCServer));

        const string NAction = "Action";
        const string NObject = "Object";
        const string NTarget = "Target";
        const string NMethod = "Method";
        const string NSync = "Sync";
        const string NType = "Type";
        const string NParam = "Param";
        const string NParams = "Params";

        const int ClientBufferSize = 1024 * 4;

        /// <summary>
        /// 连接的客户端集合
        /// </summary>
        public IReadOnlyCollection<TcpClient> Clients => ClientObjects.Keys;
        /// <summary>
        /// 客户端对象集合
        /// </summary>
        protected Dictionary<TcpClient, byte[]> ClientObjects { get; } = new Dictionary<TcpClient, byte[]>(16);

        /// <summary>
        /// 可控制、访问的对象集合，就是可以通过反射技术访问的对象集合
        /// <para>值键对 &lt;name, object&gt; name 表示唯一的对象名称</para>
        /// </summary>
        public Dictionary<string, object> AccessObjects { get; } = new Dictionary<string, object>(16);
        /// <summary>
        /// 可控制、访问的对象的方法过滤集合，指定对象的方法不在访问范围内；字符格式为：objectName.methodName, objectName 支持通配符 '*'
        /// <para>例如："*.Dispose" 禁止反射访问所有对象的 Dispose 方法，默认已添加</para>
        /// </summary>
        public List<string> MethodFilters { get; } = new List<string>(16) { "*.Dispose", "*.Close" };

        private SynchronizationContext syncContext = SynchronizationContext.Current;
        /// <summary>
        /// 当前 RPC 的同步上下文, 默认为 <see cref="SynchronizationContext.Current"/>
        /// <code>
        /// //示例：
        /// <see cref="RPCServer.SynchronizationContext"/> = new SynchronizationContext();
        /// <see cref="RPCServer.SynchronizationContext"/> = new DispatcherSynchronizationContext();
        /// <see cref="RPCServer.SynchronizationContext"/> = new ReflectionSynchronizationContext();
        /// </code>
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public SynchronizationContext SynchronizationContext
        {
            get { return syncContext; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(SynchronizationContext), "参数不能设置为空");
                syncContext = value;
            }
        }

        private ushort localPort;
        private TcpListener listener;
        /// <summary>
        /// 调用过的方法记录
        /// </summary>
        private Dictionary<string, MethodInfo> historyMethodInfos;

        /// <summary>
        /// RPC (Remote Procedure Call) or (Reflection Program Control) Server
        /// </summary>
        /// <param name="localPort"></param>
        public RPCServer(ushort localPort)
        {
            this.localPort = localPort;
            this.syncContext = SynchronizationContext.Current;
            this.historyMethodInfos = new Dictionary<string, MethodInfo>(32);
            if (syncContext == null)
            {
                Logger.Warn($"当前线程的同步上下文为空，重新创建 SynchronizationContext");
                syncContext = new SynchronizationContext();
            }
        }

        /// <summary>
        /// 启动 RPC(Remote Procedure Call)  服务
        /// </summary>
        public void Start()
        {
            if (localPort > 1024 && localPort < 65535)
            {
                listener = new TcpListener(IPAddress.Any, localPort);

                try { listener.AllowNatTraversal(true); }
                catch (Exception) { }

                try
                {
                    listener.Start(16);
                }
                catch (Exception ex)
                {
                    listener?.Stop();
                    listener = null;
                    Logger.Error($"RPC Server Start (Port:{localPort}) Excepiton: {ex}");
                    return;
                }
                Logger.Info($"RPC Server Start Success: {listener.LocalEndpoint}");

                AcceptTcpClient();
            }
        }

        /// <summary>
        /// 启动 RPC(Remote Procedure Call)  服务
        /// </summary>
        /// <param name="localPort"></param>
        public void Start(ushort localPort)
        {
            this.localPort = localPort;
            Start();
        }

        /// <summary>
        /// 停止 RPC(Remote Procedure Call)  服务
        /// </summary>
        public void Stop()
        {
            if (listener != null)
            {
                listener?.Stop();
                listener = null;
            }
            foreach (var clientObject in ClientObjects)
            {
                TcpClient client = clientObject.Key;
                try
                {
                    if (client != null && client.Connected) client.Dispose();
                }
                catch (Exception) { }
            }

            ClientObjects.Clear();
            Logger.Info($"RPC Server Stop Success");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            localPort = 0;

            Stop();
            AccessObjects.Clear();
            historyMethodInfos.Clear();
        }

        /// <summary>
        /// 接受 <see cref="TcpClient"/> 的连接
        /// </summary>
        private async void AcceptTcpClient()
        {
            if (localPort <= 1024 || listener == null) return;

            TcpClient client;
            try
            {
                client = await listener.AcceptTcpClientAsync();
                ClientObjects.Add(client, new byte[ClientBufferSize]);
                Logger.Debug($"Server Accept Remote Client {client?.Client?.RemoteEndPoint} Count:{ClientObjects.Count}");
            }
            catch(ObjectDisposedException)
            {
                return;
            }
            catch(Exception ex) 
            {
                Logger.Error($"Server Exception: {ex.Message}");
                return;
            }

            ReadNetworkStream(client);
            AcceptTcpClient();
        }
        /// <summary>
        /// 读取 <see cref="TcpClient"/> 的网络流数据
        /// </summary>
        /// <param name="client"></param>
        private async void ReadNetworkStream(TcpClient client)
        {
            if (localPort <= 1024 || listener == null) return;
            if (client == null || !client.Connected || !ClientObjects.ContainsKey(client))
            {
                if (ClientObjects.ContainsKey(client)) ClientObjects.Remove(client);
                client.Dispose();
                return;
            }

            IPEndPoint remoteEndPoint = null;
            NetworkStream networkStream = null;

            try
            {
                networkStream = client.GetStream();
                remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            }
            catch(Exception ex) 
            {
                if (ClientObjects.ContainsKey(client)) ClientObjects.Remove(client);
                client?.Dispose();
                Logger.Warn($"Client {remoteEndPoint} GetStream Exception: {ex}");                
                return;
            }

            try
            {
                byte[] buffer = ClientObjects[client];
                int count = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                
                if (count <= 0)
                {
                    if (ClientObjects.ContainsKey(client)) ClientObjects.Remove(client);
                    client?.Dispose();
                    Logger.Debug($"Remote Client {remoteEndPoint} Disconnection, Count:{ClientObjects.Count}");
                    return;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, count);
                Logger.Debug($"Remote Client {remoteEndPoint} Read Message({count}bytes): {message}");

                _ = Task.Run(() => ParseClientMessage(client, message));
            }
            catch (ObjectDisposedException)
            {
                networkStream?.Dispose();
                if(ClientObjects.ContainsKey(client)) ClientObjects.Remove(client);
                return;
            }
            catch(Exception ex)
            {
                if (ClientObjects.ContainsKey(client)) ClientObjects.Remove(client);
                client?.Dispose();

                Logger.Warn($"Client {remoteEndPoint} NetworkStream Exception: {ex}");
                return;
            }

            ReadNetworkStream(client);
        }

        /// <summary>
        /// 向客户端发送返回结果对象
        /// </summary>
        /// <param name="client"></param>
        /// <param name="result"></param>
        private void SendReturnObject(ref TcpClient client, ReturnObject result)
        {
            if (client == null) return;

            try
            {
                if (client.Connected)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes($"{result}");
                    client.GetStream()?.WriteAsync(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        /// <summary>
        /// 解析客户端消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private object ParseClientMessage(TcpClient client, string message)
        {
            if(string.IsNullOrEmpty(message)) return null;

            XElement action = null;
            try
            {
                action = XElement.Parse(message);
            }
            catch(Exception ex)
            {
                ReturnObject result = new ReturnObject(-1);
                result.Exception = $"数据解析异常: {ex.Message}";
                Logger.Warn($"Client {client?.Client?.RemoteEndPoint} {result.Exception}");

                SendReturnObject(ref client, result);
                return null;
            }

            return ParseClientMessage(client, action);
        }

        /// <summary>
        /// 解析客户端消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="action"></param>
        private object ParseClientMessage(TcpClient client, XElement action)
        {
            if (action == null || action.Name.LocalName != NAction)
            {
                SendReturnObject(ref client, new ReturnObject(-1, "数据格式错误"));
                return null;
            }

            string objectName = action.Attribute(NObject) != null ? action.Attribute(NObject).Value : action.Attribute(NTarget)?.Value;
            string methodName = action.Attribute(NMethod) != null ? action.Attribute(NMethod).Value : null;

            bool synchronous = true;
            object[] parameters = null;

            if (action.Attribute(NSync) != null)
            {
                string syncValue = action.Attribute(NSync).Value;
                if (!string.IsNullOrWhiteSpace(syncValue) && bool.TryParse(syncValue, out bool sync))
                    synchronous = sync;
            }
            if (action.Attribute(NParams) != null)
            {
                parameters = StringExtensions.SplitToObjectArray(action.Attribute(NParams).Value);
            }
            else
            {
                var paramElements = action.Elements(NParam);
                if (paramElements.Count() > 0)
                {
                    parameters = new object[paramElements.Count()];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        XElement paramElement = paramElements.ElementAt(i);
                        if (paramElement == null) continue;

                        object value = paramElement.Value;
                        Type type = Type.GetType(paramElement.Attribute(NType).Value, false, true);
                        if(type != null && type.IsArray) value = paramElement.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        parameters[i] = type != null && TypeExtensions.ConvertFrom(value, type, out object convertValue) ? convertValue : paramElement.Value;
                    }
                }
            }

            CallInstanceMethod(objectName, methodName, parameters, synchronous, out ReturnObject result);
            SendReturnObject(ref client, result);
            
            return result.Value;
        }

        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters">input parameters</param>
        /// <param name="synchronous"></param>
        /// <param name="returnResult"></param>
        /// <returns></returns>
        private bool CallInstanceMethod(string objectName, string methodName, object[] parameters, bool synchronous, out ReturnObject returnResult)
        {
            returnResult = new ReturnObject(-1);
            if(string.IsNullOrWhiteSpace(objectName) || string.IsNullOrWhiteSpace(methodName))
            {
                returnResult.Exception = $"参数异常, 参数不能为空";
                Logger.Warn($"{returnResult.Exception} {nameof(objectName)}={objectName}, {nameof(methodName)}={methodName}");
                return false;
            }
            if (!AccessObjects.TryGetValue(objectName, out object instanceObj) || instanceObj == null)
            {
                returnResult.Exception = $"实例对象名 {objectName} 不存在";
                Logger.Warn($"{returnResult.Exception}, 访问对象集合中未找到该实例对象的唯一名称");
                return false;
            }
            if (MethodFilters.IndexOf($"*.{methodName}") != -1 || MethodFilters.IndexOf($"{objectName}.{methodName}") != -1)
            {
                returnResult.Exception = $"实例对象名 {objectName}({instanceObj.GetType().Name}) 的方法 {methodName} 被禁止访问";
                Logger.Warn($"{returnResult.Exception}");
                return false;
            }

            MethodInfo methodInfo = null;
            Type instanceType = instanceObj.GetType();
            int paramsLength = parameters == null ? 0 : parameters.Length;
            string objectMethod = $"{objectName}.{methodName}.{paramsLength}";

            //Find MethodInfo
            if (!historyMethodInfos.ContainsKey(objectMethod))
            {
                var methodInfos = from method in instanceType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
                                  where method.Name == methodName && method.GetParameters().Length == paramsLength
                                  select method;

                int methodCount = methodInfos?.Count() ?? 0;
                if (methodCount <= 0)
                {
                    returnResult.Exception = $"在实例对象 {objectName}({instanceType.Name}) 中, 匹配的方法 {methodName}, 参数长度 {paramsLength}, 有 0 个";
                    Logger.Warn($"{returnResult.Exception}");
                    return false;
                }
                else if (methodCount == 1)  //如果只有唯一一个的方法与参数长度，则记录，因为不存在分歧
                {
                    methodInfo = methodInfos.First();
                    historyMethodInfos.Add(objectMethod, methodInfos.First());
                }
                else if (methodCount > 1)  //存在多个方法
                {
                    Type stringType = typeof(string);
                    List<MethodInfo> list = methodInfos.ToList();
                    for (int i = 0; i < list.Count; i++)
                    {
                        ParameterInfo[] paramsInfo = list[i].GetParameters();
                        for (int k = 0; k < paramsInfo.Length; k++)
                        {
                            Type inputParamType = parameters[k].GetType();
                            Type methodParamType = paramsInfo[k].ParameterType;
                            Logger.Debug($"input type:{inputParamType}  param type:{methodParamType}");

                            if ((inputParamType == methodParamType) ||
                                (methodParamType.IsArray && inputParamType.IsArray) ||
                                (methodParamType.IsValueType && inputParamType == stringType))
                            {
                                continue;
                            }

                            list.Remove(list[i]);
                            break;
                        }
                    }

                    if (list.Count != 1)
                    {
                        returnResult.Exception = $"在实例对象 {objectName}({instanceType.Name}) 中, 匹配的方法 {methodName}, 参数长度 {paramsLength}, 有 {list.Count} 个";
                        Logger.Warn($"{returnResult.Exception}");
                        return false;
                    }

                    methodInfo = list[0];
                }
            }
            else
            {
                methodInfo = historyMethodInfos[objectMethod];
            }

            //参数解析转换
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            object[] arguments = parameters == null ? null : new object[paramsLength];
            for (int i = 0; i < paramsLength; i++)
            {
                if (!TypeExtensions.ConvertFrom(parameters[i], parameterInfos[i].ParameterType, out object convertValue))
                {
                    returnResult.Exception = $"实例对象 {objectName}({instanceType.Name}), 方法 {methodName} 的参数值 {parameters[i]} 转换类型 {parameterInfos[i].ParameterType} 失败";
                    Logger.Warn($"{returnResult.Exception}");
                    return false;
                }
                arguments[i] = convertValue;
            }

            Action<SendOrPostCallback, object> dispatcher;
            if (synchronous) dispatcher = syncContext.Send;
            else dispatcher = syncContext.Post;

            try
            {
                returnResult.Code = 0; 
                returnResult.Type = methodInfo.ReturnType;
                
                dispatcher.Invoke((result) =>
                {
                    object value = methodInfo.Invoke(instanceObj, arguments);                    
                    if (result != null)
                    {
                        ReturnObject rresult = result as ReturnObject;
                        rresult.Value = value;
                    }
                }, 
                returnResult.Type == typeof(void) ? null : returnResult);
            }
            catch (Exception ex)
            {
                returnResult.Code = -1;
                returnResult.Exception = $"实例对象 {objectName}({instanceType.Name}), 方法 {methodInfo.Name} 调用异常: {ex.Message}";
                Logger.Warn($"{returnResult.Exception}");
                Logger.Error($"MethodBase.Invoke Exception: {ex}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public object CallMethod(XElement action) => ParseClientMessage(null, action);
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public object CallMethod(string objectName, string methodName)
        {
            if(CallInstanceMethod(objectName, methodName, null, true, out ReturnObject result))
            {
                return result.Value;
            }
            return null;
        }
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public object CallMethod(string objectName, string methodName, object[] parameters)
        {
            if (CallInstanceMethod(objectName, methodName, parameters, true, out ReturnObject result))
            {
                return result.Value;
            }
            return null;
        }
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="synchronous">在同步上下文件中同步调用，默认为 true </param>
        /// <returns></returns>
        public object CallMethod(string objectName, string methodName, object[] parameters, bool synchronous)
        {
            if (CallInstanceMethod(objectName, methodName, parameters, synchronous, out ReturnObject result))
            {
                return result.Value;
            }
            return null;
        }
    }

    internal class ReturnObject
    {
        public Type Type { get; internal set; }

        public object Value { get; internal set; }

        public int Code { get; internal set; }

        public string Exception { get; internal set; }

        public ReturnObject()
        {
        }
        public ReturnObject(XElement element)
        {
            if (element == null || element.Name.LocalName != "Return") return;

            if (element.Attribute(nameof(Type)) != null)
                Type = Type.GetType(element.Attribute(nameof(Type)).Value, false, true);

            if (Type != null && Type != typeof(void) && element.Attribute(nameof(Value)) != null)
            {
                string value = element.Attribute(nameof(Value)).Value;
                Value = !string.IsNullOrWhiteSpace(value) && TypeExtensions.ConvertFrom(value, Type, out object conversionValue) ? conversionValue : value;
            }

            if (element.Attribute(nameof(Code)) != null)
                Code = int.Parse(element.Attribute(nameof(Code)).Value);

            if (element.Attribute(nameof(Exception)) != null)
                Exception = element.Attribute(nameof(Exception)).Value;
        }
        public ReturnObject(int code)
        {
            this.Code = code;
        }
        public ReturnObject(int code, string exception)
        {
            this.Code = code;
            this.Exception = exception;
        }

        public override string ToString()
        {
            return $"<Return {nameof(Type)}=\"{Type}\" {nameof(Value)}=\"{Value}\" {nameof(Code)}=\"{Code}\" {nameof(Exception)}=\"{Exception}\" />";
        }
    }

}
