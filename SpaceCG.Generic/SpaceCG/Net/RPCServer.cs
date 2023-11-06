using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        internal static readonly LoggerTrace Logger = new LoggerTrace(nameof(RPCServer));

        /// <summary>
        /// Buffer Size
        /// </summary>
        internal const int BUFFER_SIZE = 1024;

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
        /// <see cref="RPCServer.SynchronizationContext"/> = new RPCSynchronizationContext();
        /// <see cref="RPCServer.SynchronizationContext"/> = new DispatcherSynchronizationContext();
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
                ClientObjects.Add(client, new byte[BUFFER_SIZE]);
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
                    Logger.Info($"Remote Client {remoteEndPoint} Disconnection, Clients Count:{ClientObjects.Count}");
                    return;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, count);
                Logger.Debug($"Read Remote Client {remoteEndPoint} Message({count}bytes): {message}");

                _ = Task.Run(() => TryParseClientMessage(client, message));
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
        /// 解析客户端控制消息
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        protected bool TryParseClientMessage(TcpClient client, string message)
        {
            message = message.Trim();
            char fist = message[0];
            char last = message[message.Length - 1];

            InvokeResult invokeResult = new InvokeResult(StatusCodes.Failed, $"", $"不支持协议数据，解析失败。");

            if (fist == '<' && last == '>')
            {
                XElement element = null;
                try
                {
                    element = XElement.Parse(message);
                }
                catch (Exception ex)
                {
                    invokeResult.ExceptionMessage = $"数据解析异常: {ex.Message}, 或不支持数据格式";
                    Logger.Warn($"Client {client.Client?.RemoteEndPoint} {invokeResult.ExceptionMessage}");
                    SendInvokeResult(ref client, invokeResult);
                    return false;
                }

                if (element.Name.LocalName == nameof(InvokeMessage))
                {
                    bool result = TryCallMethod(element, out invokeResult);
                    SendInvokeResult(ref client, invokeResult);
                    return result;
                }
                else if (element.Name.LocalName == $"{nameof(InvokeMessage)}s")
                {
                    var invokeResults = TryCallMethods(element);
                    SendInvokeResult(ref client, invokeResults);
                    return true;
                }
            }
            else if (fist == '{' && last == '}')
            {

            }

            Logger.Warn($"RPC Client {client.Client?.RemoteEndPoint} Message Exception: {invokeResult.ExceptionMessage}");
            SendInvokeResult(ref client, invokeResult);
            return false;
        }

        /// <summary>
        /// 向客户端发送返回结果对象
        /// </summary>
        /// <param name="client"></param>
        /// <param name="invokeResult"></param>
        protected void SendInvokeResult(ref TcpClient client, InvokeResult invokeResult)
        {
            if (client == null || invokeResult == null) return;

            try
            {
                if (client.Connected)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes($"{invokeResult.ToXElementString()}");
                    client.GetStream().WriteAsync(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }
        /// <summary>
        /// 向客户端发送返回结果对象
        /// </summary>
        /// <param name="client"></param>
        /// <param name="invokeResults"></param>
        protected void SendInvokeResult(ref TcpClient client, IEnumerable<InvokeResult> invokeResults)
        {
            if (client == null || invokeResults == null) return;

            try
            {
                if (client.Connected)
                {
                    string InvokeMessage = $"{nameof(InvokeResult)}s";
                    StringBuilder builer = new StringBuilder(BUFFER_SIZE);

                    builer.AppendLine($"<{InvokeMessage}>");
                    for (int i = 0; i < invokeResults.Count(); i++)
                    {
                        builer.AppendLine(invokeResults.ElementAt(i).ToXElementString());
                    }
                    builer.AppendLine($"</{InvokeMessage}>");

                    byte[] bytes = Encoding.UTF8.GetBytes($"{builer.ToString()}");
                    client.GetStream().WriteAsync(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        /// <summary>
        /// 获取对象的方法
        /// </summary>
        /// <param name="instanceType"></param>
        /// <param name="invokeMessage"></param>
        /// <returns></returns>
        protected IEnumerable<MethodInfo> GetMethods(Type instanceType, InvokeMessage invokeMessage)
        {
            Type extensionType = typeof(ExtensionAttribute);
            int paramsLength = invokeMessage.Parameters?.Length ?? 0;
            string objectMethod = $"{invokeMessage.ObjectName}.{paramsLength}";

            if (historyMethodInfos.ContainsKey(objectMethod))
                return new MethodInfo[] { historyMethodInfos[objectMethod] };

            //Get Instance MethodInfo
            IEnumerable<MethodInfo> methodInfos = from method in instanceType.GetMethods()
                                                  where method.Name == invokeMessage.MethodName && method.GetParameters().Length == paramsLength
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
                              where method.Name == invokeMessage.MethodName && method.IsDefined(extensionType, false)
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
                    Type inputParamType = invokeMessage.Parameters[k].GetType();
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
        /// 调用多个实例对象的方法
        /// </summary>
        /// <param name="invokeMessages"></param>
        public IEnumerable<InvokeResult> TryCallMethods(XElement invokeMessages)
        {
            if (invokeMessages == null || invokeMessages.Name.LocalName != $"{nameof(InvokeMessage)}s")
                return Enumerable.Empty<InvokeResult>();

            int IntervalDelay = 0;
            if (invokeMessages.Attribute(nameof(IntervalDelay)) != null)
            {
                IntervalDelay = int.TryParse(invokeMessages.Attribute(nameof(IntervalDelay)).Value, out int millisecondsDelay) ? millisecondsDelay : 0;
            }

            var messages = invokeMessages.Elements(nameof(InvokeMessage));
            InvokeResult[] invokeResults = new InvokeResult[messages.Count()];

            for (int i = 0; i < messages.Count(); i++)
            {
                invokeResults[i] = new InvokeResult(StatusCodes.Unknow);
                bool success = TryCallMethod(messages.ElementAt(i), out invokeResults[i]);

                if (success && IntervalDelay > 0) Task.Delay(IntervalDelay);
            }

            return invokeResults;
        }
        /// <summary>
        /// 调用多个实例对象的方法
        /// </summary>
        /// <param name="invokeMessages"></param>
        /// <returns></returns>
        public IEnumerable<InvokeResult> TryCallMethods(IEnumerable<InvokeMessage> invokeMessages)
        {
            if(invokeMessages?.Count() <= 0) return Enumerable.Empty<InvokeResult>();

            InvokeResult[] invokeResults = new InvokeResult[invokeMessages.Count()];
            for (int i = 0; i < invokeMessages.Count(); i++)
            {
                invokeResults[i] = new InvokeResult(StatusCodes.Unknow);
                TryCallMethod(invokeMessages.ElementAt(i), out invokeResults[i]);
            }

            return invokeResults;
        }
        /// <summary>
        /// 调用多个实例对象的方法
        /// </summary>
        /// <param name="invokeMessages"></param>
        /// <returns></returns>
        public Task<IEnumerable<InvokeResult>> TryCallMethodsAsync(XElement invokeMessages) => Task.Run(() => TryCallMethods(invokeMessages));
        /// <summary>
        /// 调用多个实例对象的方法
        /// </summary>
        /// <param name="invokeMessages"></param>
        /// <returns></returns>
        public Task<IEnumerable<InvokeResult>> TryCallMethodsAsync(IEnumerable<InvokeMessage> invokeMessages) => Task.Run(() => TryCallMethods(invokeMessages));

        /// <summary>
        /// 调用实例对象的方法
        /// <para>支持实例对象的公共方法，和实例类型的扩展方法</para>
        /// <para>方法调用成功返回 true 时, 输出参数 <see cref="InvokeResult" /> 有效（如果调用的方法有返回值）</para>
        /// </summary>
        /// <param name="invokeMessage"></param>
        /// <param name="invokeResult"></param>
        /// <returns>方法调用成功时，返回 true, 否则返回 false </returns>
        public bool TryCallMethod(InvokeMessage invokeMessage, out InvokeResult invokeResult)
        {
            invokeResult = new InvokeResult(StatusCodes.Failed, invokeMessage?.ObjectMethod, $"参数异常，对象或参数是无效值");
            if (invokeMessage?.IsValid() == false)
            {
                Logger.Error(invokeResult.ExceptionMessage);
                return false;
            }
            if (!AccessObjects.TryGetValue(invokeMessage.ObjectName, out object objectInstance) || objectInstance == null)
            {
                invokeResult.ExceptionMessage = $"实例对象 {invokeMessage.ObjectName} 不存在，或为 null 对象";
                Logger.Warn($"{invokeResult.ExceptionMessage}");
                return false;
            }
            if (MethodFilters.IndexOf($"*.{invokeMessage.MethodName}") != -1 || MethodFilters.IndexOf($"{invokeMessage.ObjectName}.{invokeMessage.MethodName}") != -1)
            {
                invokeResult.ExceptionMessage = $"实例对象名名称 {invokeMessage.ObjectName}({objectInstance.GetType().Name}) 的方法 {invokeMessage.MethodName} 被禁止访问";
                Logger.Warn($"{invokeResult.ExceptionMessage}");
                return false;
            }

            //获取方法对象
            Type instanceType = objectInstance.GetType();
            int paramsLength = invokeMessage.Parameters?.Length ?? 0;
            IEnumerable<MethodInfo> methodInfos = GetMethods(instanceType, invokeMessage);
            if (methodInfos.Count() != 1)
            {
                invokeResult.ExceptionMessage = $"在实例对象 {invokeMessage.ObjectName}({instanceType}) 中, 匹配的方法 {invokeMessage.MethodName}, 参数长度 {paramsLength}, 有 {methodInfos.Count()} 个";
                Logger.Warn($"{invokeResult.ExceptionMessage}");
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
                offsetIndex = 1;
                convertParameters = new object[paramsLength + 1];
                convertParameters[0] = objectInstance;
            }
            else
            {
                offsetIndex = 0;
                convertParameters = invokeMessage.Parameters == null ? null : new object[paramsLength];
            }
            for (int i = 0; i < paramsLength; i++)
            {
                Type destinationType = parameterInfos[i + offsetIndex].ParameterType;
                if (!TypeExtensions.ConvertFrom(invokeMessage.Parameters[i], destinationType, out object convertValue))
                {
                    invokeResult.ExceptionMessage = $"实例对象 {invokeMessage.ObjectName}({instanceType.Name}), 方法 {invokeMessage.MethodName} 的参数值 {invokeMessage.Parameters[i]}({invokeMessage.Parameters[i]?.GetType().Name}) 转换类型 {destinationType} 失败";
                    Logger.Warn($"{invokeResult.ExceptionMessage}");
                    return false;
                }
                convertParameters[i + offsetIndex] = convertValue;
            }

            Action<SendOrPostCallback, object> dispatcher;
            if (invokeMessage.Synchronous) dispatcher = syncContext.Send;
            else dispatcher = syncContext.Post;

            try
            {
                Type voidType = typeof(void);
                invokeResult.ReturnType = methodInfo.ReturnType;
                invokeResult.StatusCode = invokeResult.ReturnType == voidType ? StatusCodes.Success : StatusCodes.SuccessAndReturn;

                dispatcher.Invoke((result) =>
                {
                    object value = methodInfo.Invoke(objectInstance, convertParameters);
                    if (result != null)
                    {
                        InvokeResult rresult = result as InvokeResult;
                        rresult.ReturnValue = value;
                    }
                },
                invokeResult.ReturnType == voidType ? null : invokeResult);
            }
            catch (Exception ex)
            {
                invokeResult.StatusCode = StatusCodes.Failed;
                invokeResult.ExceptionMessage = $"实例对象 {invokeMessage.ObjectName}({instanceType.Name}), 方法 {methodInfo.Name} 调用异常: {ex.Message}";

                Logger.Warn($"{invokeResult.ExceptionMessage}");
                Logger.Error($"RPC Server MethodBase.Invoke Exception: {ex}");
                return false;
            }

            return true;
        }
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="message"></param>
        /// <param name="invokeResult"></param>
        /// <returns>方法调用成功时，返回 true, 否则返回 false </returns>
        public bool TryCallMethod(XElement message, out InvokeResult invokeResult) => TryCallMethod(new InvokeMessage(message), out invokeResult);
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="invokeResult"></param>
        /// <returns>方法调用成功时，返回 true, 否则返回 false </returns>
        public bool TryCallMethod(string objectName, string methodName, out InvokeResult invokeResult) => TryCallMethod(new InvokeMessage(objectName, methodName), out invokeResult);
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="invokeResult"></param>
        /// <returns>方法调用成功时，返回 true, 否则返回 false </returns>
        public bool TryCallMethod(string objectName, string methodName, object[] parameters, out InvokeResult invokeResult) => TryCallMethod(new InvokeMessage(objectName, methodName, parameters, true, null), out invokeResult);
        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="synchronous"></param>
        /// <param name="invokeResult"></param>
        /// <returns>方法调用成功时，返回 true, 否则返回 false </returns>
        private bool TryCallMethod(string objectName, string methodName, object[] parameters, bool synchronous, out InvokeResult invokeResult) => TryCallMethod(new InvokeMessage(objectName, methodName, parameters, synchronous, null), out invokeResult);

        /// <summary>
        /// 调用实例对象的方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        public T TryCallMethod<T>(string objectName, string methodName) where T : class
        {
            if (TryCallMethod(new InvokeMessage(objectName, methodName), out InvokeResult invokeResult) && invokeResult.StatusCode == StatusCodes.SuccessAndReturn)
                return invokeResult.ReturnValue as T;

            return default;
        }
    }

    internal enum FormatType
    {
        Code = 0,
        XML = 1,
        JSON = 2,
    }

    /// <summary>
    /// 调用远程方法或函数的的消息对象
    /// </summary>
    public class InvokeMessage
    {
        internal const string XType = "Type";
        internal const string XParameter = "Parameter";
        internal const string XInvokeMessage = nameof(InvokeMessage);
        internal const string XObjectName = nameof(ObjectName);
        internal const string XMethodName = nameof(MethodName);
        internal const string XParameters = nameof(Parameters);
        internal const string XSynchronous = nameof(Synchronous);
        internal const string XComment = nameof(Comment);

        /// <summary>
        /// 消息的格式类型
        /// </summary>
        internal FormatType FormatType { get; private set; } = FormatType.Code;

        /// <summary>
        /// 调用的对象或实例名称
        /// </summary>
        public string ObjectName { get; set; }
        /// <summary>
        /// 调用的方法或函数名称
        /// </summary>
        public string MethodName { get; set; }
        /// <summary>
        /// 对象方法或函数名称
        /// </summary>
        internal string ObjectMethod => $"{ObjectName}.{MethodName}";
        /// <summary>
        /// 调用的方法或函数的参数
        /// </summary>
        public object[] Parameters { get; set; }
        /// <summary>
        /// Comment
        /// </summary>
        public string Comment { get; set; }
        /// <summary>
        /// 同步或异步调用方法或函数
        /// </summary>
        public bool Synchronous { get; set; } = true;
        /// <summary>
        /// 消息对象是否有效
        /// </summary>
        /// <returns>对象有效时返回 true </returns>
        public bool IsValid() => !string.IsNullOrWhiteSpace(ObjectName) && !string.IsNullOrWhiteSpace(MethodName);

        /// <summary>
        /// 调用远程方法或函数的的消息对象
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public InvokeMessage(string objectName, string methodName)
        {
            if (string.IsNullOrWhiteSpace(objectName)) throw new ArgumentNullException(nameof(objectName), "参数不能为空");
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(objectName), "参数不能为空");

            this.ObjectName = objectName;
            this.MethodName = methodName;
        }
        /// <summary>
        /// 调用远程方法或函数的的消息对象
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="methodName"></param>
        /// <param name="parameters"></param>
        /// <param name="synchronous"></param>
        /// <param name="comment"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public InvokeMessage(string objectName, string methodName, object[] parameters, bool synchronous = true, string comment = null)
        {
            if (string.IsNullOrWhiteSpace(objectName)) throw new ArgumentNullException(nameof(objectName), "参数不能为空");
            if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentNullException(nameof(objectName), "参数不能为空");

            this.ObjectName = objectName;
            this.MethodName = methodName;
            this.Parameters = parameters;
            this.Synchronous = synchronous;
            this.Comment = comment;
        }

        internal InvokeMessage(XElement message)
        {
            if (!IsValid(message))
                throw new ArgumentNullException(nameof(message), $"{nameof(XElement)} 数据格式错误，缺少必要属性或值");

            Comment = message.Attribute(XComment)?.Value;
            ObjectName = message.Attribute(XObjectName)?.Value;
            MethodName = message.Attribute(XMethodName)?.Value;

            if (message.Attribute(XSynchronous) != null)
            {
                string syncValue = message.Attribute(XSynchronous).Value;
                if (!string.IsNullOrWhiteSpace(syncValue) && bool.TryParse(syncValue, out bool sync))
                    Synchronous = sync;
            }

            var paramElements = message.Elements(XParameter);
            if (paramElements.Count() > 0)
            {
                Parameters = new object[paramElements.Count()];
                for (int i = 0; i < Parameters.Length; i++)
                {
                    XElement paramElement = paramElements.ElementAt(i);
                    if (paramElement == null) continue;
                    if (string.IsNullOrWhiteSpace(paramElement?.Value))
                    {
                        Parameters[i] = null;
                        continue;
                    }

                    try
                    {
                        string typeString = paramElement.Attribute(XType)?.Value;
                        Type paramType = string.IsNullOrWhiteSpace(typeString) ? null : Type.GetType(typeString, false, true);
                        if (paramType == null)
                        {
                            Parameters[i] = paramElement.Value;
                            continue;
                        }

                        object value;
                        if (paramType.IsArray) value = paramElement.Value.Split(new char[] { ',' }); //, StringSplitOptions.RemoveEmptyEntries);
                        else value = paramElement.Value;

                        Parameters[i] = TypeExtensions.ConvertFrom(value, paramType, out object convertValue) ? convertValue : paramElement?.Value;
                    }
                    catch (Exception)
                    {
                        Parameters[i] = paramElement.Value;
                    }
                }
            }
            else
            {
                string parameters = message.Attribute(XParameters)?.Value;
                if (!string.IsNullOrWhiteSpace(parameters)) Parameters = StringExtensions.SplitToObjectArray(parameters);
            }
        }
        /// <summary>
        /// 返回表示当前对象 <see cref="XElement"/> 格式的字符串
        /// </summary>
        /// <returns></returns>
        public string ToXElementString()
        {
            StringBuilder builder = new StringBuilder(RPCServer.BUFFER_SIZE);
            string xsynchronous = !Synchronous ? $"{XSynchronous}={Synchronous}" : "";
            string xcomment = !string.IsNullOrWhiteSpace(Comment) ? $"{XComment}={Comment}" : "";
            builder.AppendLine($"<{XInvokeMessage} {XObjectName}=\"{ObjectName}\" {XMethodName}=\"{MethodName}\" {xsynchronous} {xcomment} >");

            if (Parameters?.Length > 0)
            {
                Type stringType = typeof(string);
                foreach (var param in Parameters)
                {
                    if (param == null)
                    {
                        builder.AppendLine($"<{XParameter} />");
                        continue;
                    }

                    Type paramType = param.GetType();
                    if (paramType == stringType)
                    {
                        builder.AppendLine($"<{XParameter} {XType}=\"{paramType}\">{param}</{XParameter}>");
                        continue;
                    }

                    string paramString = param.ToString();
                    try
                    {
                        TypeConverter converter = TypeDescriptor.GetConverter(paramType);
                        if (converter.CanConvertTo(paramType)) paramString = converter.ConvertToString(param);
                    }
                    catch (Exception)
                    {
                        paramString = param.ToString();
                    }
                    builder.AppendLine($"<{XParameter} {XType}=\"{paramType}\">{paramString}</{XParameter}>");
                }
            }
            builder.Append($"</{nameof(InvokeMessage)}>");

            return builder.ToString();
        }

        /// <summary>
        /// 检查格式，是否有效
        /// </summary>
        /// <param name="message"></param>
        /// <returns>符合协议要求，返回 true</returns>
        public static bool IsValid(XElement message)
        {
            if (message == null) return false;

            if (message.Name.LocalName != XInvokeMessage) return false;
            if (string.IsNullOrWhiteSpace(message.Attribute(XObjectName)?.Value)) return false;
            if (string.IsNullOrWhiteSpace(message.Attribute(XMethodName)?.Value)) return false;

            return true;
        }

#if false
        internal InvokeMessage(JsonElement message)
        {

        }
        /// <summary>
        /// 返回表示当前对象  <see cref="JsonElement"/>  格式的字符串
        /// </summary>
        /// <returns></returns>
        public string ToJsonElementString()
        {
            StringBuilder builder = new StringBuilder(RPCServer.BUFFER_SIZE);
            return builder.ToString();
        }
#endif

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{nameof(InvokeMessage)}] {nameof(ObjectName)}=\"{ObjectName}\", {nameof(MethodName)}=\"{MethodName}\"";
        }

    }

    /// <summary> 方法调用的状态 </summary>
    public enum StatusCodes
    {
        /// <summary> 未知状态 </summary>
        Unknow = -2,
        /// <summary> 调用失败 </summary>
        Failed = -1,
        /// <summary> 调用成功，方法或函数返回参数 </summary>
        Success = 0,
        /// <summary> 调用成功，方法或函数有返回参数  </summary>
        SuccessAndReturn = 1,
    }

    /// <summary>
    /// 方法调用返回的结果对象
    /// </summary>
    public class InvokeResult
    {
        internal const string XInvokeResult = nameof(InvokeResult);
        internal const string XStatusCode = nameof(StatusCode);
        internal const string XObjectMethod = nameof(ObjectMethod);
        internal const string XReturnType = nameof(ReturnType);
        internal const string XReturnValue = nameof(ReturnValue);
        internal const string XExceptionMessage = nameof(ExceptionMessage);

        /// <summary> 方法的调用状态 </summary>
        public StatusCodes StatusCode { get; internal set; } = StatusCodes.Unknow;

        /// <summary> 对象或实例的方法或函数的完整名称 </summary>
        public string ObjectMethod { get; internal set; }

        /// <summary> 方法的返回类型 </summary>
        public Type ReturnType { get; internal set; }

        /// <summary> 方法的返回值 </summary>
        public object ReturnValue { get; internal set; }

        /// <summary> 方法执行失败 <see cref="StatusCode"/> 小于 0 时的异常信息 </summary>
        public string ExceptionMessage { get; internal set; }

        /// <summary>
        /// 方法调用返回的结果对象
        /// </summary>
        /// <param name="status"></param>
        internal InvokeResult(StatusCodes status)
        {
            this.StatusCode = status;
        }
        /// <summary>
        /// 方法调用返回的结果对象
        /// </summary>
        internal InvokeResult(StatusCodes statusCode, string objectMethod, string exceptionMessage)
        {
            this.StatusCode = statusCode;
            this.ObjectMethod = objectMethod;
            this.ExceptionMessage = exceptionMessage;
        }

        internal InvokeResult(XElement result)
        {
            if (!IsValid(result))
                throw new ArgumentException(nameof(result), $"{nameof(XElement)} 数据格式错误");

            ObjectMethod = result.Attribute(XObjectMethod)?.Value;
            ExceptionMessage = result.Attribute(XExceptionMessage)?.Value;
            StatusCode = Enum.TryParse(result.Attribute(XStatusCode)?.Value, out StatusCodes status) ? status : StatusCodes.Unknow;

            try
            {
                if (result.Attribute(XReturnType) != null)
                    ReturnType = Type.GetType(result.Attribute(XReturnType).Value, false, true);
            }
            catch (Exception)
            {

            }

            if (ReturnType != null && ReturnType != typeof(void) && result.Attribute(XReturnValue) != null)
            {
                string value = result.Attribute(XReturnValue).Value;
                ReturnValue = !string.IsNullOrWhiteSpace(value) && TypeExtensions.ConvertFrom(value, ReturnType, out object conversionValue) ? conversionValue : value;
            }
        }
        /// <summary>
        /// 返回表示当前对象 <see cref="XElement"/> 格式的字符串
        /// </summary>
        /// <returns></returns>
        public string ToXElementString()
        {
            string returnTypeString = "";
            string returnValueString = "";
            string exceptionMessage = StatusCode < StatusCodes.Success ? $"{XExceptionMessage}=\"{ExceptionMessage}\"" : "";
            string objectMethodString = !string.IsNullOrWhiteSpace(ObjectMethod) ? $"{nameof(XObjectMethod)}=\"{ObjectMethod}\"" : "";

            if (ReturnType == null || ReturnType == typeof(void))
            {
                returnTypeString = "";
                returnValueString = "";
            }
            else if (ReturnType == typeof(string))
            {
                returnTypeString = $"{XReturnType}=\"{ReturnType.ToString()}\"";
                returnValueString = $"{XReturnValue}=\"{ReturnValue?.ToString() ?? ""}\"";
            }
            else
            {
                returnTypeString = $"{XReturnType}=\"{ReturnType.ToString()}\"";
                returnValueString = $"{XReturnValue}=\"{ReturnValue?.ToString() ?? ""}\"";

                if (ReturnValue != null)
                {
                    try
                    {
                        TypeConverter converter = TypeDescriptor.GetConverter(ReturnType);
                        if (converter.CanConvertTo(ReturnType))
                            returnValueString = $"{XReturnValue}=\"{converter.ConvertToString(ReturnValue)}\"";
                    }
                    catch (Exception ex)
                    {
                        returnValueString = $"{XReturnValue}=\"{ReturnValue}\" ";
                        RPCServer.Logger.Warn($"MethodInvokeResult TypeConverter。ConvertToString Exception: {ex}");
                    }
                }
            }

            return $"<{XInvokeResult} {XStatusCode}=\"{(int)StatusCode}\" {objectMethodString} {returnTypeString} {returnValueString} {exceptionMessage} />";
        }

#if false
        internal InvokeResult(JsonElement result)
        {

        }
        public string ToJsonElementString()
        {
            return "";
        }
#endif

        /// <inheritdoc />
        public override string ToString()
        {
            if (StatusCode == StatusCodes.SuccessAndReturn)
                return $"[{nameof(InvokeResult)}] {XStatusCode}=\"{StatusCode}\", {XObjectMethod}=\"{ObjectMethod}\", {XReturnType}=\"{ReturnType}\", {XReturnValue}=\"{ReturnValue}\"";

            return $"[{nameof(InvokeResult)}] {XStatusCode}={StatusCode}, {XObjectMethod}={ObjectMethod}";
        }

        /// <summary>
        /// 检查格式，是否有效
        /// </summary>
        /// <param name="result"></param>
        /// <returns>符合协议要求，返回 true</returns>
        public static bool IsValid(XElement result)
        {
            if (result == null) return false;
            if (result.Name.LocalName != nameof(InvokeResult)) return false;
            if (string.IsNullOrWhiteSpace(result.Attribute(XStatusCode)?.Value)) return false;

            return true;
        }

    }
}
