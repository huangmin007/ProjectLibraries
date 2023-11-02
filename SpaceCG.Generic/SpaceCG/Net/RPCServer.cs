using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using SpaceCG.Extensions;
using SpaceCG.Generic;

namespace SpaceCG.Net
{
    /// <summary>
    /// RPC (Remote Procedure Call) or (Reflection Program Control) Server
    /// <para>支持实例对象的公共方法，和实例类型的扩展方法</para>
    /// </summary>
    public class RPCServer : IDisposable
    {
        static readonly LoggerTrace Logger = new LoggerTrace(nameof(RPCServer));

        /// <summary>
        /// Buffer Size
        /// </summary>
        internal const int BufferSize = 1024 * 2;

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
        private Dictionary<string, MethodInfo> historyMethodInfos;

        /// <summary>
        /// RPC (Remote Procedure Call) or (Reflection Program Control) Server
        /// <para>支持实例对象的公共方法，和实例类型的扩展方法</para>
        /// </summary>
        public RPCServer()
        {
            this.syncContext = SynchronizationContext.Current;
            this.historyMethodInfos = new Dictionary<string, MethodInfo>(32);
            if (syncContext == null)
            {
                Logger.Warn($"当前线程的同步上下文为空，重新创建 SynchronizationContext");
                syncContext = new SynchronizationContext();
            }
        }
        /// <summary>
        /// RPC (Remote Procedure Call) or (Reflection Program Control) Server
        /// </summary>
        /// <param name="localPort"></param>
        public RPCServer(ushort localPort) : this()
        {
            this.localPort = localPort;
        }

        /// <summary>
        /// 启动 RPC(Remote Procedure Call)  服务
        /// </summary>
        public void Start()
        {
            if (localPort > 1024 && localPort < 65535)
            {
                Stop();
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

            try
            {
                listener?.Stop();
            }
            catch { }
            finally
            {
                listener = null;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Stop();

            localPort = 0;
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
                ClientObjects.Add(client, new byte[BufferSize]);
                Logger.Info($"RPC Server Accept Remote Client {client.Client?.RemoteEndPoint}, Clients Count:{ClientObjects.Count}");
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex)
            {
                Logger.Error($"RPC Server Accept Client Exception: {ex.Message}");
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
            catch (Exception ex)
            {
                if (ClientObjects.ContainsKey(client)) ClientObjects.Remove(client);
                client?.Dispose();
                Logger.Warn($"RPC Client {remoteEndPoint} GetStream Exception: {ex}");
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
                    Logger.Info($"RPC Server Remote Client {remoteEndPoint} Disconnection, Clients Count:{ClientObjects.Count}");
                    return;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, count);
                Logger.Debug($"RPC Server Remote Client {remoteEndPoint} Read Message({count}bytes): {message}");

                _ = Task.Run(() => TryParseControlMessage(client, message, out _));
            }
            catch (ObjectDisposedException)
            {
                networkStream?.Dispose();
                if (ClientObjects.ContainsKey(client)) ClientObjects.Remove(client);
                return;
            }
            catch (Exception ex)
            {
                if (ClientObjects.ContainsKey(client)) ClientObjects.Remove(client);
                client?.Dispose();

                Logger.Warn($"RPC Client {remoteEndPoint} NetworkStream Exception: {ex}");
                return;
            }

            ReadNetworkStream(client);
        }

        /// <summary>
        /// 向客户端发送返回结果对象
        /// </summary>
        /// <param name="client"></param>
        /// <param name="returnResult"></param>
        private void SendReturnObject(ref TcpClient client, MethodInvokeResult returnResult)
        {
            if (client == null || returnResult == null) return;

            try
            {
                if (client.Connected)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes($"{returnResult.ToMessage()}");
                    client.GetStream().WriteAsync(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        /// <summary>
        /// 解析客户端控制消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <param name="returnValue"></param>
        /// <returns></returns>
        private bool TryParseControlMessage(TcpClient client, string message, out object returnValue)
        {
            returnValue = null;
            if (string.IsNullOrEmpty(message)) return false;

            XElement action = null;
            try
            {
                action = XElement.Parse(message);
            }
            catch (Exception ex)
            {
                MethodInvokeResult result = new MethodInvokeResult(InvokeStatus.RPCServerException, $"数据解析异常: {ex.Message}, 或不支持数据格式");
                Logger.Warn($"Client {client?.Client?.RemoteEndPoint} {result.ExceptionMessage}");

                SendReturnObject(ref client, result);
                return false;
            }

            return TryParseControlMessage(client, action, out returnValue);
        }
        /// <summary>
        /// 解析控制消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="action"></param>
        /// <param name="returnValue"></param>
        private bool TryParseControlMessage(TcpClient client, XElement action, out object returnValue)
        {
            returnValue = null;
            if (!MethodInvokeMessage.CheckFormat(action))
            {
                SendReturnObject(ref client, new MethodInvokeResult(InvokeStatus.RPCServerException, $"{nameof(RPCClient)} 数据格式错误"));
                return false;
            }

            bool result = CallInstanceMethod(new MethodInvokeMessage(action), out MethodInvokeResult invokeResult);
            if(result) returnValue = invokeResult.ReturnValue;
            SendReturnObject(ref client, invokeResult);

            return result && invokeResult.Status == InvokeStatus.Success;
        }

        /// <summary>
        /// 获取对象的方法
        /// </summary>
        /// <param name="instanceType"></param>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private IEnumerable<MethodInfo> GetMethods(Type instanceType, string objectName, string methodName, object[] parameters)
        {
            int paramsLength = parameters?.Length ?? 0;
            Type extensionType = typeof(ExtensionAttribute);
            string objectMethod = $"{objectName}.{methodName}.{paramsLength}";

            if (historyMethodInfos.ContainsKey(objectMethod))
                return new MethodInfo[] { historyMethodInfos[objectMethod] };

            //Get Instance MethodInfo
            IEnumerable<MethodInfo> methodInfos = from method in instanceType.GetMethods()
                                                  where method.Name == methodName && method.GetParameters().Length == paramsLength
                                                  select method;

            int methodCount = methodInfos?.Count() ?? 0;
            if (methodCount == 0)
            {
                //Get Extension Method
                methodInfos = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                              where !assembly.GlobalAssemblyCache
                              from type in assembly.GetExportedTypes()
                              where type.IsSealed && !type.IsGenericType && !type.IsNested && type.IsAbstract
                              from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                              where method.Name == methodName && method.IsDefined(extensionType, false)
                              let methodParams = method.GetParameters()
                              where methodParams?.Length == (paramsLength + 1) && methodParams[0].ParameterType == instanceType
                              select method;
                Logger.Debug($"Get Extension Method Count: {methodInfos?.Count()}");
            }

            methodCount = methodInfos?.Count() ?? 0;
            if (methodCount <= 0) return Enumerable.Empty<MethodInfo>();
            if (methodCount == 1)  //如果只有唯一一个的方法与参数长度，则记录，因为不存在歧义
            {
                historyMethodInfos.Add(objectMethod, methodInfos.First());
                return methodInfos;
            }

            Type stringType = typeof(string);
            List<MethodInfo> methods = methodInfos.ToList();
            for (int i = 0; i < methods.Count; i++)
            {
                ParameterInfo[] paramsInfo = methods[i].GetParameters();
                int offset = methods[i].IsDefined(extensionType, false) ? 1 : 0;

                for (int k = 0; k < paramsLength; k++)
                {
                    Type inputParamType = parameters[k].GetType();
                    Type methodParamType = paramsInfo[k + offset].ParameterType;

                    if ((inputParamType == methodParamType) ||
                        (methodParamType.IsArray && inputParamType.IsArray) ||
                        (methodParamType.IsValueType && inputParamType == stringType))
                    {
                        continue;
                    }

                    methods.RemoveAt(i--);
                    break;
                }
            }
            return methods;
        }

        /// <summary>
        /// 调用实例对象的方法
        /// <para>支持实例对象的公共方法，和实例类型的扩展方法</para>
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters">input parameters</param>
        /// <param name="synchronous"></param>
        /// <param name="returnResult"></param>
        /// <returns></returns>
        private bool CallInstanceMethod(string objectName, string methodName, object[] parameters, bool synchronous, out MethodInvokeResult returnResult)
        {
            returnResult = new MethodInvokeResult(InvokeStatus.RPCServerException);
            if (string.IsNullOrWhiteSpace(objectName) || string.IsNullOrWhiteSpace(methodName))
            {
                returnResult.ExceptionMessage = $"参数异常, 参数不能为空";
                Logger.Warn($"{returnResult.ExceptionMessage} {nameof(objectName)}={objectName}, {nameof(methodName)}={methodName}");
                return false;
            }
            if (!AccessObjects.TryGetValue(objectName, out object objectInstance) || objectInstance == null)
            {
                returnResult.ExceptionMessage = $"实例对象名 {objectName} 不存在";
                Logger.Warn($"{returnResult.ExceptionMessage}, 访问对象集合中未找到该实例对象的唯一名称");
                return false;
            }
            if (MethodFilters.IndexOf($"*.{methodName}") != -1 || MethodFilters.IndexOf($"{objectName}.{methodName}") != -1)
            {
                returnResult.ExceptionMessage = $"实例对象名 {objectName}({objectInstance.GetType().Name}) 的方法 {methodName} 被禁止访问";
                Logger.Warn($"{returnResult.ExceptionMessage}");
                return false;
            }

            //获取方法对象
            int paramsLength = parameters?.Length ?? 0;
            Type instanceType = objectInstance.GetType();
            IEnumerable<MethodInfo> methodInfos = GetMethods(instanceType, objectName, methodName, parameters);
            if(methodInfos.Count() != 1)
            {
                returnResult.ExceptionMessage = $"在实例对象 {objectName}({instanceType}) 中, 匹配的方法 {methodName}, 参数长度 {paramsLength}, 有 {methodInfos.Count()} 个";
                Logger.Warn($"{returnResult.ExceptionMessage}");
                return false;
            }

            //参数解析转换
            int offsetIndex = 0;
            object[] convertParameters = null;
            MethodInfo methodInfo = methodInfos.First();
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            bool isExtensionMethod = methodInfo.IsDefined(typeof(ExtensionAttribute), false); //是否是实例类型的扩展方法
            if (isExtensionMethod)
            {
                convertParameters = new object[paramsLength + 1];
                convertParameters[0] = objectInstance;
                offsetIndex = 1;
            }
            else
            {
                offsetIndex = 0;
                convertParameters = parameters == null ? null : new object[paramsLength];
            }
            for (int i = 0; i < paramsLength; i++)
            {
                Type destinationType = parameterInfos[i + offsetIndex].ParameterType;
                if (!TypeExtensions.ConvertFrom(parameters[i], destinationType, out object convertValue))
                {
                    returnResult.ExceptionMessage = $"实例对象 {objectName}({instanceType.Name}), 方法 {methodName} 的参数值 {parameters[i]}({parameters[i]?.GetType().Name}) 转换类型 {destinationType} 失败";
                    Logger.Warn($"{returnResult.ExceptionMessage}");
                    return false;
                }
                convertParameters[i + offsetIndex] = convertValue;
            }

            Action<SendOrPostCallback, object> dispatcher;
            if (synchronous) dispatcher = syncContext.Send;
            else dispatcher = syncContext.Post;

            try
            {
                returnResult.Status = InvokeStatus.Success;
                returnResult.ReturnType = methodInfo.ReturnType;

                dispatcher.Invoke((result) =>
                {
                    object value = methodInfo.Invoke(objectInstance, convertParameters);
                    if (result != null)
                    {
                        MethodInvokeResult rresult = result as MethodInvokeResult;
                        rresult.ReturnValue = value;
                    }
                },
                returnResult.ReturnType == typeof(void) ? null : returnResult);
            }
            catch (Exception ex)
            {
                returnResult.Status = InvokeStatus.RPCServerException;
                returnResult.ExceptionMessage = $"实例对象 {objectName}({instanceType.Name}), 方法 {methodInfo.Name} 调用异常: {ex.Message}";

                Logger.Warn($"{returnResult.ExceptionMessage}");
                Logger.Error($"RPC Server MethodBase.Invoke Exception: {ex}");
                return false;
            }

            return true;
        }
        /// <summary>
        /// 调用实例对象的方法
        /// <para>支持实例对象的公共方法，和实例类型的扩展方法</para>
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <param name="returnResult"></param>
        /// <returns></returns>
        private bool CallInstanceMethod(MethodInvokeMessage invokeMessage, out MethodInvokeResult returnResult)
            => CallInstanceMethod(invokeMessage.ObjectName, invokeMessage.MethodName, invokeMessage.Parameters, invokeMessage.Synchronous, out returnResult);

        /// <summary>
        /// 调用多个实例对象的方法
        /// </summary>
        /// <param name="actions"></param>
        public void CallMethods(IEnumerable<XElement> actions)
        {
            foreach(var action in actions)
            {
                TryParseControlMessage(null, action, out _);
            }
        }
        /// <summary>
        /// 调用实例对象的方法
        /// <para>方法调用成功则返回 true 时, 输出参数 out returnValue 有效（如果调用的方法有返回值）</para>
        /// </summary>
        /// <param name="action"></param>
        /// <param name="returnValue"></param>
        /// <returns></returns>
        public bool TryCallMethod(XElement action, out object returnValue) => TryParseControlMessage(null, action, out returnValue);
        /// <summary>
        /// 调用实例对象的方法
        /// <para>方法调用成功则返回 true 时, 输出参数 out returnValue 有效（如果调用的方法有返回值）</para>
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="returnValue"></param>
        /// <returns></returns>
        public bool TryCallMethod(string objectName, string methodName, out object returnValue)
        {
            returnValue = null;
            if (CallInstanceMethod(objectName, methodName, null, true, out MethodInvokeResult invokeResult))
            {
                returnValue = invokeResult.ReturnValue;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 调用实例对象的方法
        /// <para>方法调用成功则返回 true 时, 输出参数 out returnValue 有效（如果调用的方法有返回值）</para>
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="returnValue"></param>
        /// <returns></returns>
        public bool TryCallMethod(string objectName, string methodName, object[] parameters, out object returnValue)
        {
            returnValue = null;
            if (CallInstanceMethod(objectName, methodName, parameters, true, out MethodInvokeResult invokeResult))
            {
                returnValue = invokeResult.ReturnValue;
                return true;
            }
            return false;
        }
        /// <summary>
        /// 调用实例对象的方法
        /// <para>方法调用成功则返回 true 时, 输出参数 out returnValue 有效（如果调用的方法有返回值）</para>
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="synchronous">在同步上下文件中同步调用，默认为 true </param>
        /// <param name="returnValue"></param>
        /// <returns></returns>
        public bool TryCallMethod(string objectName, string methodName, object[] parameters, bool synchronous, out object returnValue)
        {
            returnValue = null;
            if (CallInstanceMethod(objectName, methodName, parameters, synchronous, out MethodInvokeResult invokeResult))
            {
                returnValue = invokeResult.ReturnValue;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 方法调用的控制信息
    /// </summary>
    internal class MethodInvokeMessage
    {
        internal const string XAction = "Action";
        internal const string XObject = "Object";
        internal const string XTarget = "Target";
        internal const string XMethod = "Method";
        internal const string XSync = "Sync";
        internal const string XType = "Type";
        internal const string XParam = "Param";
        internal const string XParams = "Params";

        public string ObjectName { get; }

        public string MethodName { get; }

        public object[] Parameters { get; }

        public bool Synchronous { get; } = true;

        internal MethodInvokeMessage(XElement action) 
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action), "参数不能为空");

            ObjectName = action.Attribute(XObject) != null ? action.Attribute(XObject).Value : action.Attribute(XTarget)?.Value;
            MethodName = action.Attribute(XMethod) != null ? action.Attribute(XMethod).Value : null;

            if (action.Attribute(XSync) != null)
            {
                string syncValue = action.Attribute(XSync).Value;
                if (!string.IsNullOrWhiteSpace(syncValue) && bool.TryParse(syncValue, out bool sync))
                    Synchronous = sync;
            }

            if (action.Attribute(XParams) != null)
            {
                Parameters = StringExtensions.SplitToObjectArray(action.Attribute(XParams).Value);
            }
            else
            {
                var paramElements = action.Elements(XParam);
                if (paramElements?.Count() <= 0) return;

                Parameters = new object[paramElements.Count()];
                for (int i = 0; i < Parameters.Length; i++)
                {
                    XElement paramElement = paramElements.ElementAt(i);
                    if (paramElement == null) continue;

                    string typeName = paramElement.Attribute(XType)?.Value;
                    if (!string.IsNullOrWhiteSpace(typeName))
                    {
                        object value = paramElement.Value;
                        Type type = Type.GetType(typeName, false, true);

                        if (type != null && type.IsArray) value = paramElement.Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        Parameters[i] = type != null && TypeExtensions.ConvertFrom(value, type, out object convertValue) ? convertValue : paramElement.Value;
                    }
                    else
                    {
                        Parameters[i] = paramElement.Value;
                    }
                }
            }
        }

        internal static XElement Create(string objectName, string methodName, object[] parameters, bool synchronous)
        {
            StringBuilder builder = new StringBuilder(1024);
            builder.AppendLine($"<{XAction} {XObject}=\"{objectName}\" {XMethod}=\"{methodName}\" {XSync}=\"{synchronous}\">");
            if (parameters?.Length > 0)
            {
                foreach (var param in parameters)
                {
                    builder.AppendLine($"<{XParam} {XType}=\"{param.GetType()}\">{param}</{XParam}>");
                }
            }
            builder.Append($"</{XAction}>");

            return XElement.Parse(builder.ToString());
        }

        public static bool CheckFormat(XElement action)
        {
            return action != null && action.Name.LocalName == XAction;
        }
    }

    /// <summary> 方法调用的状态 </summary>
    public enum InvokeStatus
    {
        /// <summary> 未知状态 </summary>
        Unknow = -1,
        /// <summary> 调用成功 </summary>
        Success,
        /// <summary> 调用失败， <see cref="RPCServer"/> 端异常  </summary>
        RPCServerException,
        /// <summary> 调用失败， <see cref="RPCClient"/> 端异常  </summary>
        RPCClientException
    }

    /// <summary>
    /// 方法调用返回的结果信息
    /// </summary>
    public class MethodInvokeResult
    {
        const string XInvokeResult = "InvokeResult";

        /// <summary> 方法的调用状态 </summary>
        public InvokeStatus Status { get; internal set; }

        /// <summary> 方法的返回类型 </summary>
        public Type ReturnType { get; internal set; }

        /// <summary> 方法的返回值 </summary>
        public object ReturnValue { get; internal set; }

        /// <summary> 方法执行失败 (<see cref="Status"/> != <see cref="InvokeStatus.Success"/>) 时的异常信息 </summary>
        public string ExceptionMessage { get; internal set; }

        /// <summary>
        /// 方法调用返回的结果对象
        /// </summary>
        internal MethodInvokeResult() { }

        internal MethodInvokeResult(XElement element)
        {
            if (element == null || element.Name.LocalName != XInvokeResult) return;

            if (element.Attribute(nameof(ReturnType)) != null)
                ReturnType = Type.GetType(element.Attribute(nameof(ReturnType)).Value, false, true);

            if (ReturnType != null && ReturnType != typeof(void) && element.Attribute(nameof(ReturnValue)) != null)
            {
                string value = element.Attribute(nameof(ReturnValue)).Value;
                ReturnValue = !string.IsNullOrWhiteSpace(value) && TypeExtensions.ConvertFrom(value, ReturnType, out object conversionValue) ? conversionValue : value;
            }

            if (element.Attribute(nameof(Status)) != null)
                Status = Enum.TryParse(element.Attribute(nameof(Status)).Value, out InvokeStatus status) ? status : InvokeStatus.Unknow;

            if (element.Attribute(nameof(ExceptionMessage)) != null)
                ExceptionMessage = element.Attribute(nameof(ExceptionMessage)).Value;
        }
        /// <summary>
        /// 方法调用返回的结果对象
        /// </summary>
        /// <param name="status"></param>
        internal MethodInvokeResult(InvokeStatus status)
        {
            this.Status = status;
        }
        /// <summary>
        /// 方法调用返回的结果对象
        /// </summary>
        /// <param name="status"></param>
        /// <param name="exceptionMessage"></param>
        internal MethodInvokeResult(InvokeStatus status, string exceptionMessage)
        {
            this.Status = status;
            this.ExceptionMessage = exceptionMessage;
        }

        internal string ToMessage()
        {
            //这里要在后面完善，value to string
            return $"<{XInvokeResult} {nameof(Status)}=\"{Status}\" {nameof(ReturnType)}=\"{ReturnType}\" {nameof(ReturnValue)}=\"{ReturnValue}\" {nameof(ExceptionMessage)}=\"{ExceptionMessage}\" />";
        }

#if false
        /// <inheritdoc/>
        public override string ToString()
        {
            return $"<{XInvokeResult} {nameof(Status)}=\"{Status}\" {nameof(ReturnType)}=\"{ReturnType}\" {nameof(ReturnValue)}=\"{ReturnValue}\" {nameof(ExceptionMessage)}=\"{ExceptionMessage}\" />";
        }
#endif
    }
}
